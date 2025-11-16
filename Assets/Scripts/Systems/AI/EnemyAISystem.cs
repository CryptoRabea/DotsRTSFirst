using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using DotsRTS.Components.Units;
using DotsRTS.Components.Combat;
using DotsRTS.Components.Movement;
using DotsRTS.Components.Buildings;
using DotsRTS.Bootstrap;
using DotsRTS.Utilities;

namespace DotsRTS.Systems.AI
{
    /// <summary>
    /// Enemy AI system - handles target acquisition and decision making
    /// Optimized for massive enemy counts (100k-1M)
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(AISystemGroup))]
    public partial struct EnemyAISystem : ISystem
    {
        private const float TARGET_SEARCH_RADIUS = 50f;
        private const int CELL_SIZE = 20;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameTime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.GetSingleton<GameTime>().DeltaTime;
            var currentTime = SystemAPI.GetSingleton<GameTime>().ElapsedTime;

            // Build spatial hash of potential targets (player units and buildings)
            var targetQuery = SystemAPI.QueryBuilder()
                .WithAll<LocalTransform, Health>()
                .WithAny<UnitTag, BuildingTag>()
                .WithNone<EnemyTag>()
                .Build();

            var targetCount = targetQuery.CalculateEntityCount();
            if (targetCount == 0)
            {
                // No targets - enemies just wander or move toward center
                new NoTargetBehaviorJob().ScheduleParallel();
                return;
            }

            var targetHash = new NativeMultiHashMap<int, TargetInfo>(
                targetCount,
                Allocator.TempJob
            );

            // Build target spatial hash for units
            new BuildUnitTargetHashJob
            {
                TargetHash = targetHash.AsParallelWriter(),
                CellSize = CELL_SIZE
            }.ScheduleParallel();

            // Build target spatial hash for buildings
            new BuildBuildingTargetHashJob
            {
                TargetHash = targetHash.AsParallelWriter(),
                CellSize = CELL_SIZE
            }.ScheduleParallel();

            state.Dependency.Complete();

            // Update enemy AI
            new EnemyAIJob
            {
                DeltaTime = deltaTime,
                CurrentTime = currentTime,
                TargetHash = targetHash,
                CellSize = CELL_SIZE,
                SearchRadius = TARGET_SEARCH_RADIUS
            }.ScheduleParallel();

            state.Dependency.Complete();

            targetHash.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }

    /// <summary>
    /// Target information for enemy AI
    /// </summary>
    public struct TargetInfo
    {
        public Entity Entity;
        public float3 Position;
        public float Health;
        public float ThreatLevel;
        public bool IsBuilding;
    }

    /// <summary>
    /// Build spatial hash of unit targets
    /// </summary>
    [BurstCompile]
    public partial struct BuildUnitTargetHashJob : IJobEntity
    {
        public NativeMultiHashMap<int, TargetInfo>.ParallelWriter TargetHash;
        public int CellSize;

        [BurstCompile]
        private void Execute(
            Entity entity,
            in LocalTransform transform,
            in Health health,
            in UnitOwnership ownership)
        {
            // Only add player-owned entities as targets
            if (ownership.PlayerID < 0) return;
            if (health.IsDead) return;

            int cellX = (int)math.floor(transform.Position.x / CellSize);
            int cellZ = (int)math.floor(transform.Position.z / CellSize);
            int hash = TargetHash.GetHashCode(new int2(cellX, cellZ));

            TargetHash.Add(hash, new TargetInfo
            {
                Entity = entity,
                Position = transform.Position,
                Health = health.Current,
                ThreatLevel = 1f,
                IsBuilding = false
            });
        }
    }

    /// <summary>
    /// Build spatial hash of building targets
    /// </summary>
    [BurstCompile]
    public partial struct BuildBuildingTargetHashJob : IJobEntity
    {
        public NativeMultiHashMap<int, TargetInfo>.ParallelWriter TargetHash;
        public int CellSize;

        [BurstCompile]
        private void Execute(
            Entity entity,
            in LocalTransform transform,
            in Health health,
            in BuildingData buildingData)
        {
            // Only add player buildings
            if (buildingData.PlayerID < 0) return;
            if (health.IsDead) return;
            if (!buildingData.IsConstructed) return;

            int cellX = (int)math.floor(transform.Position.x / CellSize);
            int cellZ = (int)math.floor(transform.Position.z / CellSize);
            int hash = TargetHash.GetHashCode(new int2(cellX, cellZ));

            // Buildings have higher threat for siege units
            float threatLevel = buildingData.Type == BuildingType.Headquarters ? 10f : 1f;

            TargetHash.Add(hash, new TargetInfo
            {
                Entity = entity,
                Position = transform.Position,
                Health = health.Current,
                ThreatLevel = threatLevel,
                IsBuilding = true
            });
        }
    }

    /// <summary>
    /// Enemy AI decision making
    /// </summary>
    [BurstCompile]
    public partial struct EnemyAIJob : IJobEntity
    {
        public float DeltaTime;
        public float CurrentTime;
        [ReadOnly] public NativeMultiHashMap<int, TargetInfo> TargetHash;
        public int CellSize;
        public float SearchRadius;

        [BurstCompile]
        private void Execute(
            Entity entity,
            in LocalTransform transform,
            ref EnemyAI ai,
            ref MoveTarget moveTarget,
            ref Health health,
            in EnemyData enemyData)
        {
            if (health.IsDead)
            {
                ai.State = EnemyAIState.Dead;
                return;
            }

            // Update retarget timer
            ai.RetargetTimer += DeltaTime;

            // State machine
            switch (ai.State)
            {
                case EnemyAIState.Spawning:
                    ai.State = EnemyAIState.SeekingTarget;
                    break;

                case EnemyAIState.SeekingTarget:
                    if (ai.RetargetTimer >= ai.RetargetInterval)
                    {
                        ai.RetargetTimer = 0f;
                        FindNearestTarget(transform.Position, ref ai, enemyData);
                    }

                    if (ai.CurrentTarget != Entity.Null)
                    {
                        ai.State = EnemyAIState.MovingToTarget;
                        moveTarget.Destination = ai.TargetPosition;
                        moveTarget.HasDestination = true;
                        moveTarget.StoppingDistance = enemyData.AttackRange * 0.9f;
                    }
                    break;

                case EnemyAIState.MovingToTarget:
                    // Check if target is still valid
                    if (ai.CurrentTarget == Entity.Null || ai.RetargetTimer >= ai.RetargetInterval)
                    {
                        ai.State = EnemyAIState.SeekingTarget;
                        break;
                    }

                    // Check if in attack range
                    float distSq = math.distancesq(transform.Position, ai.TargetPosition);
                    if (distSq <= enemyData.AttackRange * enemyData.AttackRange)
                    {
                        ai.State = EnemyAIState.Attacking;
                        moveTarget.HasDestination = false;
                    }
                    break;

                case EnemyAIState.Attacking:
                    // Check if target escaped range
                    float attackDistSq = math.distancesq(transform.Position, ai.TargetPosition);
                    if (attackDistSq > (enemyData.AttackRange * 1.2f) * (enemyData.AttackRange * 1.2f))
                    {
                        ai.State = EnemyAIState.MovingToTarget;
                        moveTarget.Destination = ai.TargetPosition;
                        moveTarget.HasDestination = true;
                    }
                    break;
            }
        }

        [BurstCompile]
        private void FindNearestTarget(float3 position, ref EnemyAI ai, in EnemyData enemyData)
        {
            Entity closestTarget = Entity.Null;
            float closestDistSq = float.MaxValue;
            float3 closestPosition = float3.zero;

            int cellX = (int)math.floor(position.x / CellSize);
            int cellZ = (int)math.floor(position.z / CellSize);

            // Search in expanding radius
            int searchCells = (int)math.ceil(SearchRadius / CellSize);

            for (int x = -searchCells; x <= searchCells; x++)
            {
                for (int z = -searchCells; z <= searchCells; z++)
                {
                    int hash = TargetHash.GetHashCode(new int2(cellX + x, cellZ + z));

                    if (TargetHash.TryGetFirstValue(hash, out var target, out var iterator))
                    {
                        do
                        {
                            float distSq = math.distancesq(position, target.Position);

                            // Prioritize based on threat level and distance
                            float priority = distSq / math.max(0.1f, target.ThreatLevel);

                            if (priority < closestDistSq)
                            {
                                closestDistSq = priority;
                                closestTarget = target.Entity;
                                closestPosition = target.Position;
                            }
                        }
                        while (TargetHash.TryGetNextValue(out target, ref iterator));
                    }
                }
            }

            ai.CurrentTarget = closestTarget;
            ai.TargetPosition = closestPosition;
        }
    }

    /// <summary>
    /// Behavior when no targets exist
    /// </summary>
    [BurstCompile]
    public partial struct NoTargetBehaviorJob : IJobEntity
    {
        [BurstCompile]
        private void Execute(ref EnemyAI ai, ref MoveTarget moveTarget)
        {
            // Move toward map center (0,0,0)
            ai.CurrentTarget = Entity.Null;
            ai.TargetPosition = float3.zero;
            ai.State = EnemyAIState.SeekingTarget;

            moveTarget.Destination = float3.zero;
            moveTarget.HasDestination = true;
            moveTarget.StoppingDistance = 5f;
        }
    }
}

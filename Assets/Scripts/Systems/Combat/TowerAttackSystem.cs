using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using DotsRTS.Components.Combat;
using DotsRTS.Components.Buildings;
using DotsRTS.Components.Units;
using DotsRTS.Bootstrap;
using DotsRTS.Utilities;

namespace DotsRTS.Systems.Combat
{
    /// <summary>
    /// Tower attack system - handles automated defensive tower attacks
    /// Towers automatically target and attack nearby enemies
    /// </summary>
    [UpdateInGroup(typeof(CombatSystemGroup))]
    public partial struct TowerAttackSystem : ISystem
    {
        private const float TARGET_SEARCH_INTERVAL = 0.5f;
        private const int CELL_SIZE = 20;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameTime>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var currentTime = SystemAPI.GetSingleton<GameTime>().ElapsedTime;
            var deltaTime = SystemAPI.GetSingleton<GameTime>().DeltaTime;
            var config = SystemAPI.GetSingleton<Config.GameConfig>();

            // Build spatial hash of enemies
            var enemyQuery = SystemAPI.QueryBuilder()
                .WithAll<EnemyTag, LocalTransform, Health>()
                .Build();

            var enemyCount = enemyQuery.CalculateEntityCount();
            if (enemyCount == 0) return;

            var enemyHash = new NativeParallelMultiHashMap<int, EnemyInfo>(
                enemyCount,
                Allocator.TempJob
            );

            // Build enemy hash
            new BuildEnemyHashJob
            {
                EnemyHash = enemyHash.AsParallelWriter(),
                CellSize = CELL_SIZE
            }.ScheduleParallel();

            state.Dependency.Complete();

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // Process towers
            foreach (var (transform, towerData, buildingData, entity) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRW<TowerData>, RefRO<BuildingData>>()
                    .WithEntityAccess())
            {
                // Only attack if fully constructed
                if (!buildingData.ValueRO.IsConstructed) continue;

                // Find target if don't have one
                if (towerData.ValueRO.CurrentTarget == Entity.Null)
                {
                    FindNearestEnemy(
                        ref state,
                        ref towerData.ValueRW,
                        transform.ValueRO.Position,
                        enemyHash,
                        CELL_SIZE
                    );
                }

                // Validate current target
                if (towerData.ValueRO.CurrentTarget != Entity.Null)
                {
                    if (!state.EntityManager.Exists(towerData.ValueRO.CurrentTarget))
                    {
                        towerData.ValueRW.CurrentTarget = Entity.Null;
                        continue;
                    }

                    if (SystemAPI.HasComponent<Health>(towerData.ValueRO.CurrentTarget))
                    {
                        var targetHealth = SystemAPI.GetComponent<Health>(towerData.ValueRO.CurrentTarget);
                        if (targetHealth.IsDead)
                        {
                            towerData.ValueRW.CurrentTarget = Entity.Null;
                            continue;
                        }
                    }

                    // Get target position
                    if (!SystemAPI.HasComponent<LocalTransform>(towerData.ValueRO.CurrentTarget))
                    {
                        towerData.ValueRW.CurrentTarget = Entity.Null;
                        continue;
                    }

                    var targetTransform = SystemAPI.GetComponent<LocalTransform>(towerData.ValueRO.CurrentTarget);

                    // Check if still in range
                    float distSq = math.distancesq(transform.ValueRO.Position, targetTransform.Position);

                    if (distSq > towerData.ValueRO.AttackRange * towerData.ValueRO.AttackRange)
                    {
                        // Target out of range
                        towerData.ValueRW.CurrentTarget = Entity.Null;
                        continue;
                    }

                    // Check attack cooldown
                    float timeSinceAttack = currentTime - towerData.ValueRO.LastAttackTime;

                    if (timeSinceAttack >= towerData.ValueRO.AttackCooldown)
                    {
                        // Fire projectile
                        SpawnTowerProjectile(
                            ref state,
                            ref ecb,
                            entity,
                            transform.ValueRO.Position,
                            towerData.ValueRO.CurrentTarget,
                            targetTransform.Position,
                            towerData.ValueRO.AttackDamage,
                            config.ProjectileSpeed,
                            currentTime
                        );

                        towerData.ValueRW.LastAttackTime = currentTime;
                    }
                }
            }

            ecb.Playback(state.EntityManager);

            enemyHash.Dispose();
            ecb.Dispose();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        /// <summary>
        /// Find nearest enemy within tower range
        /// </summary>
        private void FindNearestEnemy(
            ref SystemState state,
            ref TowerData towerData,
            float3 towerPosition,
            NativeParallelMultiHashMap<int, EnemyInfo> enemyHash,
            int cellSize)
        {
            Entity closestEnemy = Entity.Null;
            float closestDistSq = towerData.AttackRange * towerData.AttackRange;

            int cellX = (int)math.floor(towerPosition.x / cellSize);
            int cellZ = (int)math.floor(towerPosition.z / cellSize);
            int searchCells = (int)math.ceil(towerData.AttackRange / cellSize);

            for (int x = -searchCells; x <= searchCells; x++)
            {
                for (int z = -searchCells; z <= searchCells; z++)
                {
                    int hash = (int)math.hash(new int2(cellX + x, cellZ + z));

                    if (enemyHash.TryGetFirstValue(hash, out var enemy, out var iterator))
                    {
                        do
                        {
                            float distSq = math.distancesq(towerPosition, enemy.Position);

                            if (distSq < closestDistSq)
                            {
                                closestDistSq = distSq;
                                closestEnemy = enemy.Entity;
                            }
                        }
                        while (enemyHash.TryGetNextValue(out enemy, ref iterator));
                    }
                }
            }

            towerData.CurrentTarget = closestEnemy;
        }

        /// <summary>
        /// Spawn tower projectile
        /// </summary>
        private void SpawnTowerProjectile(
            ref SystemState state,
            ref EntityCommandBuffer ecb,
            Entity source,
            float3 startPosition,
            Entity target,
            float3 targetPosition,
            float damage,
            float speed,
            float currentTime)
        {
            var projectile = ecb.CreateEntity();

            // Spawn from top of tower
            ecb.AddComponent(projectile, LocalTransform.FromPosition(startPosition + new float3(0, 2f, 0)));

            ecb.AddComponent<ProjectileTag>(projectile);

            ecb.AddComponent(projectile, new ProjectileData
            {
                Source = source,
                Target = target,
                TargetPosition = targetPosition,
                Speed = speed,
                Damage = damage,
                DamageType = DamageType.Physical,
                MaxLifetime = 5f,
                SpawnTime = currentTime,
                IsHoming = true,
                HasHit = false
            });
        }
    }

    /// <summary>
    /// Enemy information for spatial hash
    /// </summary>
    public struct EnemyInfo
    {
        public Entity Entity;
        public float3 Position;
        public float Health;
    }

    /// <summary>
    /// Build spatial hash of enemies for tower targeting
    /// </summary>
    [BurstCompile]
    public partial struct BuildEnemyHashJob : IJobEntity
    {
        public NativeParallelMultiHashMap<int, EnemyInfo>.ParallelWriter EnemyHash;
        public int CellSize;

        [BurstCompile]
        private void Execute(
            Entity entity,
            in LocalTransform transform,
            in Health health)
        {
            if (health.IsDead) return;

            int cellX = (int)math.floor(transform.Position.x / CellSize);
            int cellZ = (int)math.floor(transform.Position.z / CellSize);
            int hash = (int)math.hash(new int2(cellX, cellZ));

            EnemyHash.Add(hash, new EnemyInfo
            {
                Entity = entity,
                Position = transform.Position,
                Health = health.Current
            });
        }
    }
}

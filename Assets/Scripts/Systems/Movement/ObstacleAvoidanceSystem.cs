using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using DotsRTS.Components.Movement;
using DotsRTS.Bootstrap;
using MovementComponent = DotsRTS.Components.Movement.Movement;

namespace DotsRTS.Systems.Movement
{
    /// <summary>
    /// Obstacle avoidance system - prevents entities from moving through buildings and walls
    /// Uses spatial partitioning for efficient obstacle queries
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(MovementSystemGroup))]
    [UpdateAfter(typeof(FlowFieldSystem))]
    [UpdateBefore(typeof(TargetSeekingSystem))]
    public partial struct ObstacleAvoidanceSystem : ISystem
    {
        private const int CELL_SIZE = 10;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameTime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.GetSingleton<GameTime>().DeltaTime;

            // Build spatial hash of obstacles
            var obstacleQuery = SystemAPI.QueryBuilder()
                .WithAll<ObstacleTag, LocalTransform, ObstacleData>()
                .Build();

            var obstacleCount = obstacleQuery.CalculateEntityCount();
            if (obstacleCount == 0) return;

            var obstacleHash = new NativeParallelMultiHashMap<int, ObstacleInfo>(
                obstacleCount,
                Allocator.TempJob
            );

            // Populate obstacle spatial hash
            new BuildObstacleHashJob
            {
                ObstacleHash = obstacleHash.AsParallelWriter(),
                CellSize = CELL_SIZE
            }.ScheduleParallel();

            state.Dependency.Complete();

            // Apply avoidance forces
            new AvoidObstaclesJob
            {
                DeltaTime = deltaTime,
                ObstacleHash = obstacleHash,
                CellSize = CELL_SIZE
            }.ScheduleParallel();

            state.Dependency.Complete();

            obstacleHash.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }

    /// <summary>
    /// Obstacle information for spatial hash
    /// </summary>
    public struct ObstacleInfo
    {
        public float3 Position;
        public float Radius;
        public bool BlocksMovement;
    }

    /// <summary>
    /// Build spatial hash map of obstacles
    /// </summary>
    [BurstCompile]
    public partial struct BuildObstacleHashJob : IJobEntity
    {
        public NativeParallelMultiHashMap<int, ObstacleInfo>.ParallelWriter ObstacleHash;
        public int CellSize;

        [BurstCompile]
        private void Execute(in LocalTransform transform, in ObstacleData obstacleData)
        {
            if (!obstacleData.BlocksMovement) return;

            // Calculate cell
            int cellX = (int)math.floor(transform.Position.x / CellSize);
            int cellZ = (int)math.floor(transform.Position.z / CellSize);
            int hash = ObstacleHash.GetHashCode(new int2(cellX, cellZ));

            // Add obstacle to hash
            ObstacleHash.Add(hash, new ObstacleInfo
            {
                Position = transform.Position,
                Radius = obstacleData.Radius,
                BlocksMovement = obstacleData.BlocksMovement
            });
        }
    }

    /// <summary>
    /// Apply avoidance forces to entities
    /// </summary>
    [BurstCompile]
    public partial struct AvoidObstaclesJob : IJobEntity
    {
        public float DeltaTime;
        [ReadOnly] public NativeParallelMultiHashMap<int, ObstacleInfo> ObstacleHash;
        public int CellSize;

        [BurstCompile]
        private void Execute(
            in LocalTransform transform,
            ref MovementComponent movement,
            in Avoidance avoidance)
        {
            if (!avoidance.AvoidBuildings) return;

            float3 position = transform.Position;
            float3 avoidanceForce = float3.zero;

            // Check surrounding cells for obstacles
            int cellX = (int)math.floor(position.x / CellSize);
            int cellZ = (int)math.floor(position.z / CellSize);

            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    int hash = ObstacleHash.GetHashCode(new int2(cellX + x, cellZ + z));

                    if (ObstacleHash.TryGetFirstValue(hash, out var obstacle, out var iterator))
                    {
                        do
                        {
                            float3 toObstacle = obstacle.Position - position;
                            float distSq = math.lengthsq(toObstacle);
                            float avoidRadius = avoidance.AvoidanceRadius + obstacle.Radius;
                            float avoidRadiusSq = avoidRadius * avoidRadius;

                            // If too close to obstacle, push away
                            if (distSq < avoidRadiusSq && distSq > 0.001f)
                            {
                                float dist = math.sqrt(distSq);
                                float3 pushDirection = -math.normalize(toObstacle);

                                // Stronger push when closer
                                float pushStrength = (1f - (dist / avoidRadius)) * avoidance.AvoidanceForce;
                                avoidanceForce += pushDirection * pushStrength;
                            }
                        }
                        while (ObstacleHash.TryGetNextValue(out obstacle, ref iterator));
                    }
                }
            }

            // Apply avoidance force to velocity
            if (math.lengthsq(avoidanceForce) > 0.001f)
            {
                movement.Velocity += avoidanceForce * DeltaTime;

                // Limit velocity
                if (math.lengthsq(movement.Velocity) > movement.MoveSpeed * movement.MoveSpeed)
                {
                    movement.Velocity = math.normalize(movement.Velocity) * movement.MoveSpeed;
                }
            }
        }
    }
}

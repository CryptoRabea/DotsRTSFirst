using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using DotsRTS.Components.Movement;
using DotsRTS.Bootstrap;
using DotsRTS.Utilities;

namespace DotsRTS.Systems.Movement
{
    /// <summary>
    /// Advanced steering behaviors for flocking, separation, and cohesion
    /// Enables massive crowds without entity overlap
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(MovementSystemGroup))]
    [UpdateAfter(typeof(TargetSeekingSystem))]
    [UpdateBefore(typeof(MovementSystem))]
    public partial struct SteeringSystem : ISystem
    {
        private const float NEIGHBOR_RADIUS = 5f;
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

            // Build spatial hash map for efficient neighbor queries
            var entityCount = SystemAPI.QueryBuilder()
                .WithAll<LocalTransform, Steering>()
                .Build()
                .CalculateEntityCount();

            if (entityCount == 0) return;

            // Create spatial hash for this frame
            var spatialHash = new NativeMultiHashMap<int, EntityPositionData>(
                entityCount,
                Allocator.TempJob
            );

            // Populate spatial hash
            new BuildSpatialHashJob
            {
                SpatialHash = spatialHash.AsParallelWriter(),
                CellSize = CELL_SIZE
            }.ScheduleParallel();

            state.Dependency.Complete();

            // Apply steering behaviors using spatial hash
            new ApplySteeringJob
            {
                DeltaTime = deltaTime,
                SpatialHash = spatialHash,
                CellSize = CELL_SIZE,
                NeighborRadius = NEIGHBOR_RADIUS
            }.ScheduleParallel();

            state.Dependency.Complete();

            spatialHash.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }

    /// <summary>
    /// Entity position data for spatial partitioning
    /// </summary>
    public struct EntityPositionData
    {
        public Entity Entity;
        public float3 Position;
        public float3 Velocity;
    }

    /// <summary>
    /// Build spatial hash map for efficient neighbor queries
    /// </summary>
    [BurstCompile]
    public partial struct BuildSpatialHashJob : IJobEntity
    {
        public NativeMultiHashMap<int, EntityPositionData>.ParallelWriter SpatialHash;
        public int CellSize;

        [BurstCompile]
        private void Execute(Entity entity, in LocalTransform transform, in Movement movement)
        {
            // Calculate cell hash
            int cellX = (int)math.floor(transform.Position.x / CellSize);
            int cellZ = (int)math.floor(transform.Position.z / CellSize);
            int hash = SpatialHash.GetHashCode(new int2(cellX, cellZ));

            // Add to spatial hash
            SpatialHash.Add(hash, new EntityPositionData
            {
                Entity = entity,
                Position = transform.Position,
                Velocity = movement.Velocity
            });
        }
    }

    /// <summary>
    /// Apply steering forces (separation, alignment, cohesion)
    /// </summary>
    [BurstCompile]
    public partial struct ApplySteeringJob : IJobEntity
    {
        public float DeltaTime;
        [ReadOnly] public NativeMultiHashMap<int, EntityPositionData> SpatialHash;
        public int CellSize;
        public float NeighborRadius;

        [BurstCompile]
        private void Execute(
            Entity entity,
            in LocalTransform transform,
            ref Movement movement,
            in Steering steering)
        {
            float3 position = transform.Position;

            // Find neighbors in surrounding cells
            float3 separationForce = float3.zero;
            float3 alignmentSum = float3.zero;
            float3 cohesionSum = float3.zero;
            int neighborCount = 0;

            // Check 9 cells (3x3 grid around entity)
            int cellX = (int)math.floor(position.x / CellSize);
            int cellZ = (int)math.floor(position.z / CellSize);

            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    int hash = SpatialHash.GetHashCode(new int2(cellX + x, cellZ + z));

                    if (SpatialHash.TryGetFirstValue(hash, out var neighbor, out var iterator))
                    {
                        do
                        {
                            // Skip self
                            if (neighbor.Entity == entity) continue;

                            float3 offset = position - neighbor.Position;
                            float distSq = math.lengthsq(offset);

                            // Only process neighbors within radius
                            if (distSq < NeighborRadius * NeighborRadius && distSq > 0.001f)
                            {
                                float dist = math.sqrt(distSq);

                                // Separation: steer away from neighbors
                                separationForce += (offset / dist) / dist;

                                // Alignment: match neighbor velocities
                                alignmentSum += neighbor.Velocity;

                                // Cohesion: move toward center of neighbors
                                cohesionSum += neighbor.Position;

                                neighborCount++;
                            }
                        }
                        while (SpatialHash.TryGetNextValue(out neighbor, ref iterator));
                    }
                }
            }

            // Apply steering forces
            if (neighborCount > 0)
            {
                // Average alignment and cohesion
                float3 alignment = (alignmentSum / neighborCount) - movement.Velocity;
                float3 cohesion = ((cohesionSum / neighborCount) - position);
                cohesion = math.normalizesafe(cohesion) * steering.MaxSpeed - movement.Velocity;

                // Combine forces with weights
                float3 steeringForce =
                    separationForce * steering.SeparationWeight +
                    alignment * steering.AlignmentWeight +
                    cohesion * steering.CohesionWeight;

                // Limit steering force
                if (math.lengthsq(steeringForce) > steering.MaxForce * steering.MaxForce)
                {
                    steeringForce = math.normalize(steeringForce) * steering.MaxForce;
                }

                // Apply to velocity
                movement.Velocity += steeringForce * DeltaTime;

                // Limit velocity
                if (math.lengthsq(movement.Velocity) > steering.MaxSpeed * steering.MaxSpeed)
                {
                    movement.Velocity = math.normalize(movement.Velocity) * steering.MaxSpeed;
                }
            }
        }
    }
}

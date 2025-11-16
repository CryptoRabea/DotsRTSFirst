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
    /// Makes entities move toward their target destination
    /// Updates velocity based on desired direction
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(MovementSystemGroup))]
    [UpdateBefore(typeof(MovementSystem))]
    public partial struct TargetSeekingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameTime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.GetSingleton<GameTime>().DeltaTime;

            new SeekTargetJob
            {
                DeltaTime = deltaTime
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }

    /// <summary>
    /// Job that calculates desired velocity toward target
    /// </summary>
    [BurstCompile]
    public partial struct SeekTargetJob : IJobEntity
    {
        public float DeltaTime;

        [BurstCompile]
        private void Execute(
            in LocalTransform transform,
            ref Movement movement,
            ref MoveTarget target)
        {
            if (!target.HasDestination)
            {
                // No destination - slow down to stop
                movement.Velocity = math.lerp(movement.Velocity, float3.zero, 5f * DeltaTime);
                return;
            }

            // Calculate direction to target
            float3 direction = target.Destination - transform.Position;
            float distanceSq = math.lengthsq(direction);
            float stoppingDistSq = target.StoppingDistance * target.StoppingDistance;

            // Check if reached destination
            if (distanceSq <= stoppingDistSq)
            {
                target.ReachedDestination = true;
                movement.Velocity = math.lerp(movement.Velocity, float3.zero, 10f * DeltaTime);
                return;
            }

            target.ReachedDestination = false;

            // Normalize direction
            direction = math.normalize(direction);

            // Calculate desired velocity
            float3 desiredVelocity = direction * movement.MoveSpeed;

            // Smoothly accelerate toward desired velocity
            movement.Velocity = math.lerp(
                movement.Velocity,
                desiredVelocity,
                movement.Acceleration * DeltaTime
            );

            // Update current direction
            movement.CurrentDirection = math.normalizesafe(movement.Velocity);
        }
    }
}

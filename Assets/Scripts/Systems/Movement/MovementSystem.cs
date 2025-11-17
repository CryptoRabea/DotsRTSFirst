using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using DotsRTS.Components.Movement;
using DotsRTS.Bootstrap;
using DotsRTS.Utilities;
using MovementComponent = DotsRTS.Components.Movement.Movement;

namespace DotsRTS.Systems.Movement
{
    /// <summary>
    /// High-performance movement system - applies velocity to position
    /// Processes hundreds of thousands of entities per frame using Burst compilation
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(MovementSystemGroup))]
    public partial struct MovementSystem : ISystem
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

            // Process all moving entities in parallel using IJobEntity
            new MovementJob
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
    /// Burst-compiled job for moving entities
    /// Runs in parallel across all chunks
    /// </summary>
    [BurstCompile]
    public partial struct MovementJob : IJobEntity
    {
        public float DeltaTime;

        [BurstCompile]
        private void Execute(ref LocalTransform transform, in MovementComponent movement)
        {
            // Apply velocity to position
            if (math.lengthsq(movement.Velocity) > 0.001f)
            {
                transform.Position += movement.Velocity * DeltaTime;

                // Update rotation to face movement direction
                float3 forward = math.normalizesafe(movement.Velocity);
                if (math.lengthsq(forward) > 0.001f)
                {
                    quaternion targetRotation = quaternion.LookRotationSafe(forward, math.up());
                    transform.Rotation = math.slerp(transform.Rotation, targetRotation,
                        movement.RotationSpeed * DeltaTime);
                }
            }
        }
    }
}

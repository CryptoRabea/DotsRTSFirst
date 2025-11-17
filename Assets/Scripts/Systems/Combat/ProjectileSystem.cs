using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using DotsRTS.Components.Combat;
using DotsRTS.Bootstrap;
using DotsRTS.Utilities;

namespace DotsRTS.Systems.Combat
{
    /// <summary>
    /// Projectile movement system - moves projectiles toward targets
    /// Handles homing projectiles that track moving targets
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    public partial struct ProjectileMovementSystem : ISystem
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

            new MoveProjectilesJob
            {
                DeltaTime = deltaTime,
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true)
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }

    /// <summary>
    /// Job to move projectiles
    /// </summary>
    [BurstCompile]
    public partial struct MoveProjectilesJob : IJobEntity
    {
        public float DeltaTime;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

        [BurstCompile]
        private void Execute(
            ref LocalTransform transform,
            ref ProjectileData projectileData)
        {
            if (projectileData.HasHit) return;

            // Update target position for homing projectiles
            if (projectileData.IsHoming && projectileData.Target != Entity.Null)
            {
                if (TransformLookup.HasComponent(projectileData.Target))
                {
                    var targetTransform = TransformLookup[projectileData.Target];
                    projectileData.TargetPosition = targetTransform.Position;
                }
            }

            // Calculate direction to target
            float3 direction = math.normalizesafe(projectileData.TargetPosition - transform.Position);

            // Move toward target
            transform.Position += direction * projectileData.Speed * DeltaTime;

            // Rotate to face movement direction
            if (math.lengthsq(direction) > 0.001f)
            {
                transform.Rotation = quaternion.LookRotationSafe(direction, math.up());
            }
        }
    }

    /// <summary>
    /// Projectile lifetime system - destroys old projectiles
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(ProjectileMovementSystem))]
    public partial struct ProjectileLifetimeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameTime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var currentTime = SystemAPI.GetSingleton<GameTime>().ElapsedTime;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // Check projectile lifetimes
            foreach (var (projectileData, entity) in
                SystemAPI.Query<RefRO<ProjectileData>>().WithEntityAccess())
            {
                float lifetime = currentTime - projectileData.ValueRO.SpawnTime;

                if (lifetime > projectileData.ValueRO.MaxLifetime)
                {
                    ecb.DestroyEntity(entity);
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }

    /// <summary>
    /// Projectile collision system - detects when projectiles hit targets
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(ProjectileMovementSystem))]
    public partial struct ProjectileCollisionSystem : ISystem
    {
        private const float HIT_RADIUS = 0.5f;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameTime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var currentTime = SystemAPI.GetSingleton<GameTime>().ElapsedTime;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // Check for hits
            foreach (var (projectileData, transform, entity) in
                SystemAPI.Query<RefRW<ProjectileData>, RefRO<LocalTransform>>()
                    .WithEntityAccess())
            {
                if (projectileData.ValueRO.HasHit) continue;

                // Check distance to target
                float distSq = math.distancesq(transform.ValueRO.Position, projectileData.ValueRO.TargetPosition);

                if (distSq <= HIT_RADIUS * HIT_RADIUS)
                {
                    // Hit detected
                    projectileData.ValueRW.HasHit = true;

                    // Apply damage to target
                    if (projectileData.ValueRO.Target != Entity.Null)
                    {
                        ApplyDamage(
                            ref state,
                            ref ecb,
                            projectileData.ValueRO.Target,
                            projectileData.ValueRO.Damage,
                            projectileData.ValueRO.DamageType,
                            projectileData.ValueRO.Source,
                            transform.ValueRO.Position,
                            currentTime
                        );
                    }

                    // Check for AOE damage
                    if (SystemAPI.HasComponent<AOEDamage>(entity))
                    {
                        ApplyAOEDamage(
                            ref state,
                            ref ecb,
                            entity,
                            transform.ValueRO.Position,
                            currentTime
                        );
                    }

                    // Destroy projectile
                    ecb.DestroyEntity(entity);
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        /// <summary>
        /// Apply damage to a target
        /// </summary>
        private void ApplyDamage(
            ref SystemState state,
            ref EntityCommandBuffer ecb,
            Entity target,
            float damage,
            DamageType damageType,
            Entity source,
            float3 hitPosition,
            float currentTime)
        {
            if (!state.EntityManager.Exists(target)) return;

            // Add damage event component
            ecb.AddComponent(target, new DamageEvent
            {
                Amount = damage,
                Source = source,
                Type = damageType,
                HitPosition = hitPosition
            });
        }

        /// <summary>
        /// Apply area of effect damage
        /// </summary>
        private void ApplyAOEDamage(
            ref SystemState state,
            ref EntityCommandBuffer ecb,
            Entity projectile,
            float3 explosionPosition,
            float currentTime)
        {
            var aoe = SystemAPI.GetComponent<AOEDamage>(projectile);

            // Find all entities within AOE radius
            foreach (var (health, transform, entity) in
                SystemAPI.Query<RefRO<Health>, RefRO<LocalTransform>>()
                    .WithEntityAccess())
            {
                if (health.ValueRO.IsDead) continue;

                float distSq = math.distancesq(transform.ValueRO.Position, explosionPosition);

                if (distSq <= aoe.Radius * aoe.Radius)
                {
                    // Calculate falloff (linear)
                    float dist = math.sqrt(distSq);
                    float damageMult = 1f - (dist / aoe.Radius);
                    float actualDamage = aoe.Damage * damageMult;

                    // Apply damage
                    ecb.AddComponent(entity, new DamageEvent
                    {
                        Amount = actualDamage,
                        Source = aoe.Source,
                        Type = aoe.DamageType,
                        HitPosition = explosionPosition
                    });
                }
            }
        }
    }
}

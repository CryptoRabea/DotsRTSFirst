using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using DotsRTS.Components.Combat;
using DotsRTS.Components.Units;
using DotsRTS.Bootstrap;

namespace DotsRTS.Systems.Combat
{
    /// <summary>
    /// Melee attack system - handles close-range combat
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    public partial struct MeleeAttackSystem : ISystem
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

            // Process melee units
            foreach (var (transform, canAttack, attackTarget, health) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRW<CanAttack>,
                    RefRW<AttackTarget>, RefRO<Health>>()
                    .WithAll<MeleeUnitTag>())
            {
                if (health.ValueRO.IsDead) continue;
                if (attackTarget.ValueRO.Target == Entity.Null) continue;

                // Check if target still exists and is alive
                if (!state.EntityManager.Exists(attackTarget.ValueRO.Target))
                {
                    attackTarget.ValueRW.Target = Entity.Null;
                    continue;
                }

                if (SystemAPI.HasComponent<Health>(attackTarget.ValueRO.Target))
                {
                    var targetHealth = SystemAPI.GetComponent<Health>(attackTarget.ValueRO.Target);
                    if (targetHealth.IsDead)
                    {
                        attackTarget.ValueRW.Target = Entity.Null;
                        continue;
                    }
                }

                // Get target position
                if (!SystemAPI.HasComponent<LocalTransform>(attackTarget.ValueRO.Target))
                    continue;

                var targetTransform = SystemAPI.GetComponent<LocalTransform>(attackTarget.ValueRO.Target);
                attackTarget.ValueRW.LastKnownPosition = targetTransform.Position;

                // Check if in range
                float distSq = math.distancesq(transform.ValueRO.Position, targetTransform.Position);

                if (distSq <= canAttack.ValueRO.AttackRange * canAttack.ValueRO.AttackRange)
                {
                    // Check attack cooldown
                    float timeSinceAttack = currentTime - canAttack.ValueRO.LastAttackTime;

                    if (timeSinceAttack >= canAttack.ValueRO.AttackCooldown)
                    {
                        // Perform melee attack
                        ecb.AddComponent(attackTarget.ValueRO.Target, new DamageEvent
                        {
                            Amount = canAttack.ValueRO.AttackDamage,
                            Source = Entity.Null, // Would need entity reference
                            Type = canAttack.ValueRO.DamageType,
                            HitPosition = targetTransform.Position
                        });

                        canAttack.ValueRW.LastAttackTime = currentTime;
                    }
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
    /// Ranged attack system - handles projectile spawning for archers
    /// </summary>
    [UpdateInGroup(typeof(CombatSystemGroup))]
    public partial struct RangedAttackSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameTime>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var currentTime = SystemAPI.GetSingleton<GameTime>().ElapsedTime;
            var config = SystemAPI.GetSingleton<Config.GameConfig>();
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // Process ranged units
            foreach (var (transform, canAttack, attackTarget, health, entity) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRW<CanAttack>,
                    RefRW<AttackTarget>, RefRO<Health>>()
                    .WithAll<RangedUnitTag>()
                    .WithEntityAccess())
            {
                if (health.ValueRO.IsDead) continue;
                if (attackTarget.ValueRO.Target == Entity.Null) continue;

                // Check if target still exists and is alive
                if (!state.EntityManager.Exists(attackTarget.ValueRO.Target))
                {
                    attackTarget.ValueRW.Target = Entity.Null;
                    continue;
                }

                if (SystemAPI.HasComponent<Health>(attackTarget.ValueRO.Target))
                {
                    var targetHealth = SystemAPI.GetComponent<Health>(attackTarget.ValueRO.Target);
                    if (targetHealth.IsDead)
                    {
                        attackTarget.ValueRW.Target = Entity.Null;
                        continue;
                    }
                }

                // Get target position
                if (!SystemAPI.HasComponent<LocalTransform>(attackTarget.ValueRO.Target))
                    continue;

                var targetTransform = SystemAPI.GetComponent<LocalTransform>(attackTarget.ValueRO.Target);
                attackTarget.ValueRW.LastKnownPosition = targetTransform.Position;

                // Check if in range
                float distSq = math.distancesq(transform.ValueRO.Position, targetTransform.Position);

                if (distSq <= canAttack.ValueRO.AttackRange * canAttack.ValueRO.AttackRange)
                {
                    // Check attack cooldown
                    float timeSinceAttack = currentTime - canAttack.ValueRO.LastAttackTime;

                    if (timeSinceAttack >= canAttack.ValueRO.AttackCooldown)
                    {
                        // Spawn projectile
                        SpawnProjectile(
                            ref state,
                            ref ecb,
                            entity,
                            transform.ValueRO.Position,
                            attackTarget.ValueRO.Target,
                            targetTransform.Position,
                            canAttack.ValueRO.AttackDamage,
                            config.ProjectileSpeed,
                            currentTime
                        );

                        canAttack.ValueRW.LastAttackTime = currentTime;
                    }
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        /// <summary>
        /// Spawn a projectile entity
        /// </summary>
        private void SpawnProjectile(
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
            // Create projectile entity
            var projectile = ecb.CreateEntity();

            // Add transform (slightly above ground)
            ecb.AddComponent(projectile, LocalTransform.FromPosition(startPosition + new float3(0, 0.5f, 0)));

            // Add projectile tag
            ecb.AddComponent<ProjectileTag>(projectile);

            // Add projectile data
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
}

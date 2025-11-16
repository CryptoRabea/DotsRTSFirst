using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using DotsRTS.Components.Combat;
using DotsRTS.Bootstrap;

namespace DotsRTS.Systems.Combat
{
    /// <summary>
    /// Damage processing system - applies damage events to entities
    /// Handles armor, damage reduction, and death
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(ProjectileCollisionSystem))]
    public partial struct DamageSystem : ISystem
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

            // Process damage events
            foreach (var (damageEvent, health, entity) in
                SystemAPI.Query<RefRO<DamageEvent>, RefRW<Health>>()
                    .WithEntityAccess())
            {
                if (health.ValueRO.IsDead)
                {
                    ecb.RemoveComponent<DamageEvent>(entity);
                    continue;
                }

                // Calculate actual damage (accounting for armor if present)
                float actualDamage = damageEvent.ValueRO.Amount;

                if (SystemAPI.HasComponent<Armor>(entity))
                {
                    actualDamage = CalculateDamageWithArmor(
                        damageEvent.ValueRO.Amount,
                        damageEvent.ValueRO.Type,
                        SystemAPI.GetComponent<Armor>(entity)
                    );
                }

                // Apply damage
                health.ValueRW.Current -= actualDamage;
                health.ValueRW.LastDamageTime = currentTime;

                // Clamp health
                health.ValueRW.Current = math.max(0, health.ValueRO.Current);

                // Check for death
                if (health.ValueRO.Current <= 0)
                {
                    health.ValueRW.IsDead = true;

                    // Add death event if not already present
                    if (!SystemAPI.HasComponent<DeathEvent>(entity))
                    {
                        ecb.AddComponent(entity, new DeathEvent
                        {
                            DeathTime = currentTime,
                            Killer = damageEvent.ValueRO.Source,
                            HasBeenProcessed = false
                        });
                    }
                }

                // Remove damage event after processing
                ecb.RemoveComponent<DamageEvent>(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        /// <summary>
        /// Calculate damage after armor reduction
        /// </summary>
        [BurstCompile]
        private float CalculateDamageWithArmor(float damage, DamageType damageType, Armor armor)
        {
            float resistance = damageType switch
            {
                DamageType.Physical => armor.PhysicalArmor,
                DamageType.Fire => armor.FireResistance,
                DamageType.Poison => armor.PoisonResistance,
                DamageType.Siege => armor.SiegeResistance,
                _ => 0f
            };

            // Armor formula: damage * (1 - resistance)
            // Resistance is clamped 0-1 (0% to 100%)
            resistance = math.clamp(resistance, 0f, 0.99f);
            return damage * (1f - resistance);
        }
    }

    /// <summary>
    /// Death processing system - handles entity cleanup after death
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CleanupSystemGroup))]
    public partial struct DeathSystem : ISystem
    {
        private const float DEATH_DELAY = 0.5f; // Delay before removal for visual effects

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

            // Process death events
            foreach (var (deathEvent, health, entity) in
                SystemAPI.Query<RefRW<DeathEvent>, RefRO<Health>>()
                    .WithNone<Components.Buildings.BuildingTag>() // Buildings handled separately
                    .WithEntityAccess())
            {
                if (!deathEvent.ValueRO.HasBeenProcessed)
                {
                    deathEvent.ValueRW.HasBeenProcessed = true;

                    // TODO: Trigger death effects, animations, etc.
                }

                // Check if enough time has passed
                float timeSinceDeath = currentTime - deathEvent.ValueRO.DeathTime;

                if (timeSinceDeath >= DEATH_DELAY)
                {
                    // Destroy entity
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
    /// Health regeneration system
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateBefore(typeof(DamageSystem))]
    public partial struct RegenerationSystem : ISystem
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
            var currentTime = SystemAPI.GetSingleton<GameTime>().ElapsedTime;

            // Process regeneration
            foreach (var (regen, health) in
                SystemAPI.Query<RefRO<Regeneration>, RefRW<Health>>())
            {
                if (health.ValueRO.IsDead) continue;
                if (health.ValueRO.Current >= health.ValueRO.Maximum) continue;

                // Check if enough time has passed since last damage
                float timeSinceDamage = currentTime - health.ValueRO.LastDamageTime;

                if (timeSinceDamage >= regen.ValueRO.RegenDelay)
                {
                    // Apply regeneration
                    float regenAmount = regen.ValueRO.RegenRate * deltaTime;
                    health.ValueRW.Current = math.min(
                        health.ValueRO.Current + regenAmount,
                        health.ValueRO.Maximum
                    );
                }
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}

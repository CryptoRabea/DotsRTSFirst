using Unity.Entities;
using Unity.Mathematics;

namespace DotsRTS.Components.Combat
{
    /// <summary>
    /// Health component for all damageable entities
    /// </summary>
    public struct Health : IComponentData
    {
        public float Current;
        public float Maximum;
        public bool IsDead;
        public float LastDamageTime;
    }

    /// <summary>
    /// Damage event component - added when entity takes damage
    /// Processed and removed by damage system
    /// </summary>
    public struct DamageEvent : IComponentData
    {
        public float Amount;
        public Entity Source;
        public DamageType Type;
        public float3 HitPosition;
    }

    public enum DamageType : byte
    {
        Physical,
        Fire,
        Poison,
        Siege
    }

    /// <summary>
    /// Tag for entities that can deal damage
    /// </summary>
    public struct CanAttack : IComponentData
    {
        public float AttackRange;
        public float AttackDamage;
        public float AttackCooldown;
        public float LastAttackTime;
        public DamageType DamageType;
        public bool RequiresLineOfSight;
    }

    /// <summary>
    /// Current attack target
    /// </summary>
    public struct AttackTarget : IComponentData
    {
        public Entity Target;
        public float3 LastKnownPosition;
        public float AcquisitionTime;
    }

    /// <summary>
    /// Projectile tag
    /// </summary>
    public struct ProjectileTag : IComponentData
    {
    }

    /// <summary>
    /// Projectile data
    /// </summary>
    public struct ProjectileData : IComponentData
    {
        public Entity Source;
        public Entity Target;
        public float3 TargetPosition;       // Last known target position
        public float Speed;
        public float Damage;
        public DamageType DamageType;
        public float MaxLifetime;
        public float SpawnTime;
        public bool IsHoming;               // Tracks moving targets
        public bool HasHit;
    }

    /// <summary>
    /// Area of effect damage (for explosions, etc.)
    /// </summary>
    public struct AOEDamage : IComponentData
    {
        public float Radius;
        public float Damage;
        public DamageType DamageType;
        public Entity Source;
    }

    /// <summary>
    /// Armor component - reduces incoming damage
    /// </summary>
    public struct Armor : IComponentData
    {
        public float PhysicalArmor;
        public float FireResistance;
        public float PoisonResistance;
        public float SiegeResistance;
    }

    /// <summary>
    /// Death event - marked for cleanup
    /// </summary>
    public struct DeathEvent : IComponentData
    {
        public float DeathTime;
        public Entity Killer;
        public bool HasBeenProcessed;
    }

    /// <summary>
    /// Regeneration component
    /// </summary>
    public struct Regeneration : IComponentData
    {
        public float RegenRate;             // HP per second
        public float RegenDelay;            // Delay after taking damage
        public float LastDamageTime;
    }
}

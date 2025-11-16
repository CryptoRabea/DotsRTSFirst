using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using DotsRTS.Components.Combat;

namespace DotsRTS.Aspects
{
    /// <summary>
    /// Projectile aspect
    /// </summary>
    public readonly partial struct ProjectileAspect : IAspect
    {
        public readonly Entity Entity;

        private readonly RefRW<LocalTransform> m_Transform;
        private readonly RefRW<ProjectileData> m_Data;

        public float3 Position
        {
            get => m_Transform.ValueRO.Position;
            set => m_Transform.ValueRW.Position = value;
        }

        public quaternion Rotation
        {
            get => m_Transform.ValueRO.Rotation;
            set => m_Transform.ValueRW.Rotation = value;
        }

        public Entity Target => m_Data.ValueRO.Target;
        public float3 TargetPosition => m_Data.ValueRO.TargetPosition;
        public float Speed => m_Data.ValueRO.Speed;
        public float Damage => m_Data.ValueRO.Damage;
        public bool IsHoming => m_Data.ValueRO.IsHoming;
        public bool HasHit
        {
            get => m_Data.ValueRO.HasHit;
            set => m_Data.ValueRW.HasHit = value;
        }

        public void UpdateTargetPosition(float3 newPosition)
        {
            m_Data.ValueRW.TargetPosition = newPosition;
        }

        public void MoveTowardsTarget(float deltaTime)
        {
            float3 direction = math.normalizesafe(TargetPosition - Position);
            Position += direction * Speed * deltaTime;

            // Update rotation to face movement direction
            if (math.lengthsq(direction) > 0.001f)
            {
                Rotation = quaternion.LookRotationSafe(direction, math.up());
            }
        }
    }

    /// <summary>
    /// Combatant aspect - for entities that can attack
    /// </summary>
    public readonly partial struct CombatantAspect : IAspect
    {
        public readonly Entity Entity;

        private readonly RefRO<LocalTransform> m_Transform;
        private readonly RefRW<CanAttack> m_CanAttack;
        private readonly RefRW<AttackTarget> m_AttackTarget;
        private readonly RefRO<Health> m_Health;

        public float3 Position => m_Transform.ValueRO.Position;

        public float AttackRange => m_CanAttack.ValueRO.AttackRange;
        public float AttackDamage => m_CanAttack.ValueRO.AttackDamage;
        public float AttackCooldown => m_CanAttack.ValueRO.AttackCooldown;
        public float LastAttackTime => m_CanAttack.ValueRO.LastAttackTime;

        public Entity Target
        {
            get => m_AttackTarget.ValueRO.Target;
            set => m_AttackTarget.ValueRW.Target = value;
        }

        public float3 LastKnownTargetPosition
        {
            get => m_AttackTarget.ValueRO.LastKnownPosition;
            set => m_AttackTarget.ValueRW.LastKnownPosition = value;
        }

        public bool IsAlive => !m_Health.ValueRO.IsDead;

        public bool CanAttackNow(float currentTime)
        {
            return (currentTime - LastAttackTime) >= AttackCooldown;
        }

        public void RecordAttack(float currentTime)
        {
            m_CanAttack.ValueRW.LastAttackTime = currentTime;
        }

        public bool IsInRange(float3 targetPosition)
        {
            float distSq = math.distancesq(Position, targetPosition);
            return distSq <= (AttackRange * AttackRange);
        }
    }

    /// <summary>
    /// Damageable aspect - for entities that can take damage
    /// </summary>
    public readonly partial struct DamageableAspect : IAspect
    {
        public readonly Entity Entity;

        private readonly RefRW<Health> m_Health;

        public float CurrentHealth => m_Health.ValueRO.Current;
        public float MaxHealth => m_Health.ValueRO.Maximum;
        public bool IsDead => m_Health.ValueRO.IsDead;
        public float HealthPercent => m_Health.ValueRO.Current / math.max(0.001f, m_Health.ValueRO.Maximum);

        public void TakeDamage(float amount, float currentTime)
        {
            m_Health.ValueRW.Current = math.max(0, m_Health.ValueRO.Current - amount);
            m_Health.ValueRW.IsDead = m_Health.ValueRO.Current <= 0;
            m_Health.ValueRW.LastDamageTime = currentTime;
        }

        public void Heal(float amount)
        {
            m_Health.ValueRW.Current = math.min(m_Health.ValueRO.Maximum, m_Health.ValueRO.Current + amount);
            m_Health.ValueRW.IsDead = false;
        }

        public void SetMaxHealth(float maxHealth)
        {
            m_Health.ValueRW.Maximum = maxHealth;
            m_Health.ValueRW.Current = math.min(m_Health.ValueRO.Current, maxHealth);
        }
    }
}

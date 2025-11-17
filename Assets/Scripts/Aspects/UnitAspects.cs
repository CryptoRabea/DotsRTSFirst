using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using DotsRTS.Components.Units;
using DotsRTS.Components.Combat;
using DotsRTS.Components.Movement;

namespace DotsRTS.Aspects
{
    /// <summary>
    /// Basic unit aspect - common data for all units
    /// </summary>
    public readonly partial struct UnitAspect : IAspect
    {
        public readonly Entity Entity;

        private readonly RefRO<LocalTransform> m_Transform;
        private readonly RefRW<Movement> m_Movement;
        private readonly RefRW<Health> m_Health;
        private readonly RefRO<UnitStats> m_Stats;

        public float3 Position => m_Transform.ValueRO.Position;
        public quaternion Rotation => m_Transform.ValueRO.Rotation;
        public float3 Velocity => m_Movement.ValueRO.Velocity;
        public float MoveSpeed => m_Movement.ValueRO.MoveSpeed;

        public float CurrentHealth => m_Health.ValueRO.Current;
        public float MaxHealth => m_Health.ValueRO.Maximum;
        public bool IsDead => m_Health.ValueRO.IsDead;

        public float AttackRange => m_Stats.ValueRO.AttackRange;
        public float AttackDamage => m_Stats.ValueRO.AttackDamage;

        public void SetVelocity(float3 velocity)
        {
            m_Movement.ValueRW.Velocity = velocity;
        }

        public void TakeDamage(float amount, float currentTime)
        {
            m_Health.ValueRW.Current = math.max(0, m_Health.ValueRO.Current - amount);
            m_Health.ValueRW.IsDead = m_Health.ValueRO.Current <= 0;
            m_Health.ValueRW.LastDamageTime = currentTime;
        }

        public void Heal(float amount)
        {
            m_Health.ValueRW.Current = math.min(m_Health.ValueRO.Maximum, m_Health.ValueRO.Current + amount);
        }
    }

    /// <summary>
    /// Worker unit aspect
    /// </summary>
    public readonly partial struct WorkerAspect : IAspect
    {
        public readonly Entity Entity;

        private readonly RefRO<LocalTransform> m_Transform;
        private readonly RefRW<WorkerData> m_WorkerData;
        private readonly RefRW<Movement> m_Movement;
        private readonly RefRW<UnitStateComponent> m_State;

        public float3 Position => m_Transform.ValueRO.Position;

        public Entity TargetResourceNode
        {
            get => m_WorkerData.ValueRO.TargetResourceNode;
            set => m_WorkerData.ValueRW.TargetResourceNode = value;
        }

        public Entity TargetDepositBuilding
        {
            get => m_WorkerData.ValueRO.TargetDepositBuilding;
            set => m_WorkerData.ValueRW.TargetDepositBuilding = value;
        }

        public ResourceType CarriedResourceType
        {
            get => m_WorkerData.ValueRO.CarriedResourceType;
            set => m_WorkerData.ValueRW.CarriedResourceType = value;
        }

        public int CarriedAmount
        {
            get => m_WorkerData.ValueRO.CarriedResourceAmount;
            set => m_WorkerData.ValueRW.CarriedResourceAmount = value;
        }

        public bool IsCarryingResources => m_WorkerData.ValueRO.CarriedResourceAmount > 0;

        public UnitState State
        {
            get => m_State.ValueRO.State;
            set => m_State.ValueRW.State = value;
        }

        public void SetState(UnitState newState, float currentTime)
        {
            m_State.ValueRW.State = newState;
            m_State.ValueRW.StateStartTime = currentTime;
        }
    }

    /// <summary>
    /// Enemy unit aspect
    /// </summary>
    public readonly partial struct EnemyAspect : IAspect
    {
        public readonly Entity Entity;

        private readonly RefRO<LocalTransform> m_Transform;
        private readonly RefRW<EnemyAI> m_AI;
        private readonly RefRW<Movement> m_Movement;
        private readonly RefRW<Health> m_Health;
        private readonly RefRO<EnemyData> m_Data;

        public float3 Position => m_Transform.ValueRO.Position;
        public quaternion Rotation => m_Transform.ValueRO.Rotation;

        public Entity CurrentTarget
        {
            get => m_AI.ValueRO.CurrentTarget;
            set => m_AI.ValueRW.CurrentTarget = value;
        }

        public float3 TargetPosition
        {
            get => m_AI.ValueRO.TargetPosition;
            set => m_AI.ValueRW.TargetPosition = value;
        }

        public EnemyAIState AIState
        {
            get => m_AI.ValueRO.State;
            set => m_AI.ValueRW.State = value;
        }

        public float MoveSpeed => m_Data.ValueRO.MoveSpeed;
        public float AttackRange => m_Data.ValueRO.AttackRange;
        public float AttackDamage => m_Data.ValueRO.AttackDamage;

        public float CurrentHealth => m_Health.ValueRO.Current;
        public bool IsDead => m_Health.ValueRO.IsDead;

        public void SetVelocity(float3 velocity)
        {
            m_Movement.ValueRW.Velocity = velocity;
        }

        public void TakeDamage(float amount, float currentTime)
        {
            m_Health.ValueRW.Current = math.max(0, m_Health.ValueRO.Current - amount);
            m_Health.ValueRW.IsDead = m_Health.ValueRO.Current <= 0;
            m_Health.ValueRW.LastDamageTime = currentTime;
        }
    }
}

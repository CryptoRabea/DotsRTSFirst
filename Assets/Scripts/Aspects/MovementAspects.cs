using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using DotsRTS.Components.Movement;

namespace DotsRTS.Aspects
{
    /// <summary>
    /// Movement aspect - for moving entities
    /// </summary>
    public readonly partial struct MovementAspect : IAspect
    {
        public readonly Entity Entity;

        private readonly RefRW<LocalTransform> m_Transform;
        private readonly RefRW<Movement> m_Movement;
        private readonly RefRW<MoveTarget> m_Target;

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

        public float3 Velocity
        {
            get => m_Movement.ValueRO.Velocity;
            set => m_Movement.ValueRW.Velocity = value;
        }

        public float MoveSpeed => m_Movement.ValueRO.MoveSpeed;
        public float RotationSpeed => m_Movement.ValueRO.RotationSpeed;

        public float3 Destination
        {
            get => m_Target.ValueRO.Destination;
            set => m_Target.ValueRW.Destination = value;
        }

        public bool HasDestination
        {
            get => m_Target.ValueRO.HasDestination;
            set => m_Target.ValueRW.HasDestination = value;
        }

        public bool ReachedDestination => m_Target.ValueRO.ReachedDestination;

        public void SetDestination(float3 destination, float stoppingDistance = 0.5f)
        {
            m_Target.ValueRW.Destination = destination;
            m_Target.ValueRW.HasDestination = true;
            m_Target.ValueRW.StoppingDistance = stoppingDistance;
            m_Target.ValueRW.ReachedDestination = false;
        }

        public void ClearDestination()
        {
            m_Target.ValueRW.HasDestination = false;
            m_Target.ValueRW.ReachedDestination = false;
        }

        public void Move(float deltaTime)
        {
            if (!HasDestination) return;

            float3 direction = Destination - Position;
            float distanceSq = math.lengthsq(direction);
            float stoppingDistSq = m_Target.ValueRO.StoppingDistance * m_Target.ValueRO.StoppingDistance;

            if (distanceSq <= stoppingDistSq)
            {
                m_Target.ValueRW.ReachedDestination = true;
                Velocity = float3.zero;
                return;
            }

            direction = math.normalize(direction);
            Velocity = direction * MoveSpeed;
            Position += Velocity * deltaTime;

            // Rotate to face movement direction
            if (math.lengthsq(Velocity) > 0.001f)
            {
                quaternion targetRotation = quaternion.LookRotationSafe(direction, math.up());
                Rotation = math.slerp(Rotation, targetRotation, RotationSpeed * deltaTime);
            }
        }

        public float DistanceToDestination()
        {
            return math.distance(Position, Destination);
        }

        public float DistanceToDestinationSquared()
        {
            return math.distancesq(Position, Destination);
        }
    }

    /// <summary>
    /// Steering aspect - for advanced movement behaviors
    /// </summary>
    public readonly partial struct SteeringAspect : IAspect
    {
        public readonly Entity Entity;

        private readonly RefRW<LocalTransform> m_Transform;
        private readonly RefRW<Movement> m_Movement;
        private readonly RefRW<Steering> m_Steering;

        public float3 Position
        {
            get => m_Transform.ValueRO.Position;
            set => m_Transform.ValueRW.Position = value;
        }

        public float3 Velocity
        {
            get => m_Movement.ValueRO.Velocity;
            set => m_Movement.ValueRW.Velocity = value;
        }

        public float3 DesiredVelocity
        {
            get => m_Steering.ValueRO.DesiredVelocity;
            set => m_Steering.ValueRW.DesiredVelocity = value;
        }

        public float MaxSpeed => m_Steering.ValueRO.MaxSpeed;
        public float MaxForce => m_Steering.ValueRO.MaxForce;

        public void ApplySteeringForce(float3 force, float deltaTime)
        {
            // Limit force magnitude
            if (math.lengthsq(force) > MaxForce * MaxForce)
            {
                force = math.normalize(force) * MaxForce;
            }

            // Apply force to velocity
            float3 newVelocity = Velocity + force * deltaTime;

            // Limit velocity magnitude
            if (math.lengthsq(newVelocity) > MaxSpeed * MaxSpeed)
            {
                newVelocity = math.normalize(newVelocity) * MaxSpeed;
            }

            Velocity = newVelocity;
        }

        public float3 CalculateSeek(float3 targetPosition)
        {
            float3 desired = math.normalize(targetPosition - Position) * MaxSpeed;
            return desired - Velocity;
        }

        public float3 CalculateFlee(float3 targetPosition)
        {
            float3 desired = math.normalize(Position - targetPosition) * MaxSpeed;
            return desired - Velocity;
        }

        public float3 CalculateSeparation(float3 averagePosition, float separationRadius)
        {
            float3 diff = Position - averagePosition;
            float distSq = math.lengthsq(diff);

            if (distSq > 0.001f && distSq < separationRadius * separationRadius)
            {
                return math.normalize(diff) / math.sqrt(distSq);
            }

            return float3.zero;
        }
    }

    /// <summary>
    /// Flow field agent aspect
    /// </summary>
    public readonly partial struct FlowFieldAspect : IAspect
    {
        public readonly Entity Entity;

        private readonly RefRO<LocalTransform> m_Transform;
        private readonly RefRW<FlowFieldAgent> m_FlowField;
        private readonly RefRW<Movement> m_Movement;

        public float3 Position => m_Transform.ValueRO.Position;

        public int2 CurrentCell
        {
            get => m_FlowField.ValueRO.CurrentCell;
            set => m_FlowField.ValueRW.CurrentCell = value;
        }

        public int2 TargetCell
        {
            get => m_FlowField.ValueRO.TargetCell;
            set => m_FlowField.ValueRW.TargetCell = value;
        }

        public float3 FlowDirection
        {
            get => m_FlowField.ValueRO.FlowDirection;
            set => m_FlowField.ValueRW.FlowDirection = value;
        }

        public bool UseFlowField
        {
            get => m_FlowField.ValueRO.UseFlowField;
            set => m_FlowField.ValueRW.UseFlowField = value;
        }

        public void ApplyFlowDirection(float deltaTime)
        {
            if (!UseFlowField) return;

            float3 velocity = FlowDirection * m_Movement.ValueRO.MoveSpeed;
            m_Movement.ValueRW.Velocity = velocity;
        }
    }
}

using Unity.Entities;
using Unity.Mathematics;

namespace DotsRTS.Components.Movement
{
    /// <summary>
    /// Movement speed and velocity
    /// </summary>
    public struct Movement : IComponentData
    {
        public float3 Velocity;
        public float MoveSpeed;
        public float RotationSpeed;
        public float Acceleration;
        public float3 CurrentDirection;
    }

    /// <summary>
    /// Movement destination
    /// </summary>
    public struct MoveTarget : IComponentData
    {
        public float3 Destination;
        public bool HasDestination;
        public float StoppingDistance;
        public bool ReachedDestination;
    }

    /// <summary>
    /// Follow entity (for chase behavior)
    /// </summary>
    public struct FollowTarget : IComponentData
    {
        public Entity Target;
        public float FollowDistance;
        public float3 LastTargetPosition;
        public bool IsFollowing;
    }

    /// <summary>
    /// Steering behavior data
    /// </summary>
    public struct Steering : IComponentData
    {
        public float3 DesiredVelocity;
        public float3 SteeringForce;
        public float MaxForce;
        public float MaxSpeed;
        public float SeparationWeight;
        public float AlignmentWeight;
        public float CohesionWeight;
    }

    /// <summary>
    /// Avoidance - avoid obstacles and other units
    /// </summary>
    public struct Avoidance : IComponentData
    {
        public float AvoidanceRadius;
        public float AvoidanceForce;
        public bool AvoidUnits;
        public bool AvoidBuildings;
    }

    /// <summary>
    /// Flow field pathfinding data
    /// Stores which grid cell the entity is in and desired direction
    /// </summary>
    public struct FlowFieldAgent : IComponentData
    {
        public int2 CurrentCell;
        public int2 TargetCell;
        public float3 FlowDirection;
        public bool UseFlowField;
    }

    /// <summary>
    /// Formation data (for group movement)
    /// </summary>
    public struct FormationMember : IComponentData
    {
        public Entity FormationLeader;
        public int FormationIndex;
        public float3 FormationOffset;
        public bool InFormation;
    }

    /// <summary>
    /// Obstacle tag - for static objects that block movement
    /// </summary>
    public struct ObstacleTag : IComponentData
    {
    }

    /// <summary>
    /// Pathfinding obstacle data
    /// </summary>
    public struct ObstacleData : IComponentData
    {
        public float Radius;
        public bool BlocksMovement;
        public bool BlocksVision;
    }

    /// <summary>
    /// Wander behavior (random movement)
    /// </summary>
    public struct Wander : IComponentData
    {
        public float WanderRadius;
        public float WanderDistance;
        public float WanderAngle;
        public float WanderChangeRate;
        public float3 WanderTarget;
    }
}

using Unity.Entities;
using Unity.Mathematics;

namespace DotsRTS.Components.Units
{
    /// <summary>
    /// Base tag for all units (workers, soldiers, etc.)
    /// </summary>
    public struct UnitTag : IComponentData
    {
    }

    /// <summary>
    /// Worker unit - gathers resources
    /// </summary>
    public struct WorkerTag : IComponentData
    {
    }

    /// <summary>
    /// Melee combat unit
    /// </summary>
    public struct MeleeUnitTag : IComponentData
    {
    }

    /// <summary>
    /// Ranged combat unit (archer)
    /// </summary>
    public struct RangedUnitTag : IComponentData
    {
    }

    /// <summary>
    /// Unit stats and attributes
    /// </summary>
    public struct UnitStats : IComponentData
    {
        public float MoveSpeed;
        public float AttackDamage;
        public float AttackRange;
        public float AttackCooldown;
        public float LastAttackTime;
        public int PopulationCost;
    }

    /// <summary>
    /// Unit ownership and team
    /// </summary>
    public struct UnitOwnership : IComponentData
    {
        public int PlayerID;        // 0 = player, negative = enemy
        public int TeamID;          // For team-based gameplay
        public bool IsPlayerControlled;
    }

    /// <summary>
    /// Current unit state
    /// </summary>
    public enum UnitState : byte
    {
        Idle,
        Moving,
        Gathering,
        Attacking,
        Fleeing,
        Dead
    }

    /// <summary>
    /// Unit state component
    /// </summary>
    public struct UnitStateComponent : IComponentData
    {
        public UnitState State;
        public float StateStartTime;
    }

    /// <summary>
    /// Worker-specific data
    /// </summary>
    public struct WorkerData : IComponentData
    {
        public Entity TargetResourceNode;
        public Entity TargetDepositBuilding;
        public ResourceType CarriedResourceType;
        public int CarriedResourceAmount;
        public int MaxCarryCapacity;
        public float GatherSpeed;
    }

    /// <summary>
    /// Resource types that can be gathered
    /// </summary>
    public enum ResourceType : byte
    {
        None,
        Wood,
        Stone,
        Food,
        Gold
    }

    /// <summary>
    /// Selection state for player-controlled units
    /// </summary>
    public struct Selectable : IComponentData
    {
        public bool IsSelected;
        public bool IsHovered;
    }
}

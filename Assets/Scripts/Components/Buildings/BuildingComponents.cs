using Unity.Entities;
using Unity.Mathematics;

namespace DotsRTS.Components.Buildings
{
    /// <summary>
    /// Base tag for all building entities
    /// </summary>
    public struct BuildingTag : IComponentData
    {
    }

    /// <summary>
    /// Building types
    /// </summary>
    public enum BuildingType : byte
    {
        Headquarters,
        House,
        Barracks,
        ArcheryRange,
        Tower,
        Wall,
        Gate,
        Storehouse,
        Farm,
        LumberCamp,
        StoneMine
    }

    /// <summary>
    /// Building data
    /// </summary>
    public struct BuildingData : IComponentData
    {
        public BuildingType Type;
        public int PlayerID;
        public float ConstructionProgress;  // 0-1, 1 = complete
        public bool IsConstructed;
        public int2 GridPosition;           // Grid-based position
        public int2 GridSize;               // Size in grid cells (e.g., 2x2, 3x3)
    }

    /// <summary>
    /// House - increases population cap
    /// </summary>
    public struct HouseTag : IComponentData
    {
    }

    public struct HouseData : IComponentData
    {
        public int PopulationProvided;
    }

    /// <summary>
    /// Barracks - produces melee units
    /// </summary>
    public struct BarracksTag : IComponentData
    {
    }

    /// <summary>
    /// Archery Range - produces ranged units
    /// </summary>
    public struct ArcheryRangeTag : IComponentData
    {
    }

    /// <summary>
    /// Production building data
    /// </summary>
    public struct ProductionBuilding : IComponentData
    {
        public Entity CurrentProducingUnit;
        public float ProductionProgress;
        public float ProductionTime;
        public bool IsProducing;
        public int QueueCount;
    }

    /// <summary>
    /// Tower - defensive structure
    /// </summary>
    public struct TowerTag : IComponentData
    {
    }

    public struct TowerData : IComponentData
    {
        public float AttackRange;
        public float AttackDamage;
        public float AttackCooldown;
        public float LastAttackTime;
        public Entity CurrentTarget;
        public bool CanAttackGround;
        public bool CanAttackAir;
    }

    /// <summary>
    /// Wall segment
    /// </summary>
    public struct WallTag : IComponentData
    {
    }

    public struct WallData : IComponentData
    {
        public int2 GridPosition;
        public WallOrientation Orientation;
        public bool IsCorner;
        public bool IsGate;
        public Entity ConnectedWallNorth;
        public Entity ConnectedWallSouth;
        public Entity ConnectedWallEast;
        public Entity ConnectedWallWest;
    }

    public enum WallOrientation : byte
    {
        Horizontal,
        Vertical,
        Corner
    }

    /// <summary>
    /// Storehouse - resource deposit point
    /// </summary>
    public struct StorehouseTag : IComponentData
    {
    }

    public struct StorehouseData : IComponentData
    {
        public bool AcceptsWood;
        public bool AcceptsStone;
        public bool AcceptsFood;
        public bool AcceptsGold;
    }

    /// <summary>
    /// Building placement ghost (for preview before placement)
    /// </summary>
    public struct PlacementGhost : IComponentData
    {
        public bool IsValidPlacement;
        public BuildingType BuildingType;
        public int2 GridPosition;
        public int2 GridSize;
    }

    /// <summary>
    /// Building under construction
    /// </summary>
    public struct UnderConstruction : IComponentData
    {
        public float BuildProgress;         // 0-1
        public float BuildTime;
        public int WoodCost;
        public int StoneCost;
        public int GoldCost;
        public bool ResourcesPaid;
    }
}

using Unity.Entities;
using Unity.Mathematics;
using DotsRTS.Components.Units;

namespace DotsRTS.Components.Resources
{
    /// <summary>
    /// Global resource storage (singleton)
    /// </summary>
    public struct GlobalResources : IComponentData
    {
        public int Wood;
        public int Stone;
        public int Food;
        public int Gold;
        public int CurrentPopulation;
        public int MaxPopulation;
    }

    /// <summary>
    /// Resource node tag
    /// </summary>
    public struct ResourceNodeTag : IComponentData
    {
    }

    /// <summary>
    /// Resource node data
    /// </summary>
    public struct ResourceNode : IComponentData
    {
        public ResourceType Type;
        public int CurrentAmount;
        public int MaxAmount;
        public bool IsDepleted;
        public float RegenerationRate;      // Resources per second (for farms, etc.)
        public float LastRegenerationTime;
        public int GatherersCount;          // How many workers are gathering from this node
        public int MaxGatherers;            // Maximum simultaneous gatherers
    }

    /// <summary>
    /// Gatherer spots around a resource node
    /// </summary>
    public struct GatherSpot : IBufferElementData
    {
        public float3 Position;
        public bool IsOccupied;
        public Entity Occupant;
    }

    /// <summary>
    /// Resource deposit building (storehouse, etc.)
    /// </summary>
    public struct ResourceDepositTag : IComponentData
    {
    }

    /// <summary>
    /// Resource costs for buildings/units
    /// </summary>
    public struct ResourceCost : IComponentData
    {
        public int WoodCost;
        public int StoneCost;
        public int FoodCost;
        public int GoldCost;
        public int PopulationCost;
    }

    /// <summary>
    /// Resource income modifier (for passive income)
    /// </summary>
    public struct ResourceIncome : IComponentData
    {
        public float WoodPerSecond;
        public float StonePerSecond;
        public float FoodPerSecond;
        public float GoldPerSecond;
    }

    /// <summary>
    /// Resource transaction event
    /// </summary>
    public struct ResourceTransaction : IComponentData
    {
        public ResourceType Type;
        public int Amount;                  // Positive = gain, negative = spend
        public Entity Source;
        public bool IsProcessed;
    }
}

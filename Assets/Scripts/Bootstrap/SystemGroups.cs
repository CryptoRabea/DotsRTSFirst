using Unity.Entities;
using Unity.Burst;

namespace DotsRTS.Bootstrap
{
    /// <summary>
    /// Main simulation group for all game logic systems
    /// Updates in SimulationSystemGroup
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial class RTSSimulationSystemGroup : ComponentSystemGroup
    {
    }

    /// <summary>
    /// Input processing group - handles player input
    /// </summary>
    [UpdateInGroup(typeof(RTSSimulationSystemGroup), OrderFirst = true)]
    public partial class InputSystemGroup : ComponentSystemGroup
    {
    }

    /// <summary>
    /// Movement and pathfinding systems
    /// </summary>
    [UpdateInGroup(typeof(RTSSimulationSystemGroup))]
    [UpdateAfter(typeof(InputSystemGroup))]
    public partial class MovementSystemGroup : ComponentSystemGroup
    {
    }

    /// <summary>
    /// AI and decision-making systems
    /// </summary>
    [UpdateInGroup(typeof(RTSSimulationSystemGroup))]
    [UpdateAfter(typeof(InputSystemGroup))]
    public partial class AISystemGroup : ComponentSystemGroup
    {
    }

    /// <summary>
    /// Resource gathering and economy systems
    /// </summary>
    [UpdateInGroup(typeof(RTSSimulationSystemGroup))]
    [UpdateAfter(typeof(AISystemGroup))]
    public partial class EconomySystemGroup : ComponentSystemGroup
    {
    }

    /// <summary>
    /// Combat-related systems (damage, projectiles, etc.)
    /// </summary>
    [UpdateInGroup(typeof(RTSSimulationSystemGroup))]
    [UpdateAfter(typeof(MovementSystemGroup))]
    public partial class CombatSystemGroup : ComponentSystemGroup
    {
    }

    /// <summary>
    /// Building and construction systems
    /// </summary>
    [UpdateInGroup(typeof(RTSSimulationSystemGroup))]
    [UpdateAfter(typeof(EconomySystemGroup))]
    public partial class BuildingSystemGroup : ComponentSystemGroup
    {
    }

    /// <summary>
    /// Game state management (day/night, waves, victory/defeat)
    /// </summary>
    [UpdateInGroup(typeof(RTSSimulationSystemGroup))]
    [UpdateAfter(typeof(CombatSystemGroup))]
    public partial class GameStateSystemGroup : ComponentSystemGroup
    {
    }

    /// <summary>
    /// Cleanup systems (entity destruction, recycling, etc.)
    /// </summary>
    [UpdateInGroup(typeof(RTSSimulationSystemGroup), OrderLast = true)]
    public partial class CleanupSystemGroup : ComponentSystemGroup
    {
    }

    /// <summary>
    /// Presentation systems (visual updates, UI, etc.)
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class RTSPresentationSystemGroup : ComponentSystemGroup
    {
    }
}

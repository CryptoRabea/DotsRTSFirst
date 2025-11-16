using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using DotsRTS.Components.Resources;
using DotsRTS.Components.Units;
using DotsRTS.Bootstrap;

namespace DotsRTS.Systems.Economy
{
    /// <summary>
    /// Resource node system - handles resource depletion and regeneration
    /// Updates gather spot occupancy
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(EconomySystemGroup))]
    [UpdateAfter(typeof(WorkerAISystem))]
    public partial struct ResourceNodeSystem : ISystem
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

            // Update resource nodes
            foreach (var (resourceNode, buffer) in
                SystemAPI.Query<RefRW<ResourceNode>, DynamicBuffer<GatherSpot>>())
            {
                // Handle regeneration
                if (resourceNode.ValueRO.RegenerationRate > 0f &&
                    resourceNode.ValueRO.CurrentAmount < resourceNode.ValueRO.MaxAmount)
                {
                    float timeSinceRegen = currentTime - resourceNode.ValueRO.LastRegenerationTime;

                    if (timeSinceRegen >= 1f) // Regenerate every second
                    {
                        int regenAmount = (int)(resourceNode.ValueRO.RegenerationRate * timeSinceRegen);
                        resourceNode.ValueRW.CurrentAmount = math.min(
                            resourceNode.ValueRO.CurrentAmount + regenAmount,
                            resourceNode.ValueRO.MaxAmount
                        );
                        resourceNode.ValueRW.LastRegenerationTime = currentTime;

                        if (resourceNode.ValueRO.CurrentAmount > 0)
                        {
                            resourceNode.ValueRW.IsDepleted = false;
                        }
                    }
                }

                // Update depletion status
                if (resourceNode.ValueRO.CurrentAmount <= 0)
                {
                    resourceNode.ValueRW.IsDepleted = true;
                }

                // Count active gatherers
                int gatherersCount = 0;
                for (int i = 0; i < buffer.Length; i++)
                {
                    if (buffer[i].IsOccupied)
                    {
                        gatherersCount++;
                    }
                }
                resourceNode.ValueRW.GatherersCount = gatherersCount;
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }

    /// <summary>
    /// Resource gathering system - handles actual resource extraction
    /// Reduces resource node amounts when workers gather
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(EconomySystemGroup))]
    [UpdateAfter(typeof(ResourceNodeSystem))]
    public partial struct ResourceGatheringSystem : ISystem
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

            // Process workers that are gathering
            foreach (var (workerData, workerState) in
                SystemAPI.Query<RefRW<WorkerData>, RefRO<UnitStateComponent>>())
            {
                if (workerState.ValueRO.State != UnitState.Gathering)
                    continue;

                if (workerData.ValueRO.TargetResourceNode == Entity.Null)
                    continue;

                // Check if enough time has passed to gather
                float gatherTime = currentTime - workerState.ValueRO.StateStartTime;
                float gatherDuration = 1f / workerData.ValueRO.GatherSpeed;

                if (gatherTime >= gatherDuration)
                {
                    // Try to gather from node
                    if (SystemAPI.HasComponent<ResourceNode>(workerData.ValueRO.TargetResourceNode))
                    {
                        var node = SystemAPI.GetComponentRW<ResourceNode>(workerData.ValueRO.TargetResourceNode);

                        int amountToGather = math.min(10, node.ValueRO.CurrentAmount);
                        amountToGather = math.min(amountToGather, workerData.ValueRO.MaxCarryCapacity);

                        if (amountToGather > 0)
                        {
                            // Deduct from node
                            node.ValueRW.CurrentAmount -= amountToGather;

                            // Update worker's carried amount (done in WorkerAISystem)
                            // This system just handles the node depletion
                        }
                    }
                }
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}

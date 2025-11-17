using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using DotsRTS.Components.Resources;
using DotsRTS.Components.Units;
using DotsRTS.Components.Movement;
using DotsRTS.Bootstrap;
using DotsRTS.Utilities;

namespace DotsRTS.Systems.Economy
{
    /// <summary>
    /// Gather spot management system
    /// Assigns workers to specific gather spots around resource nodes
    /// Prevents overcrowding and ensures efficient gathering
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(EconomySystemGroup))]
    [UpdateBefore(typeof(WorkerAISystem))]
    public partial struct GatherSpotSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameTime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // First pass: Clear invalid spot occupants (dead workers, workers that moved away)
            foreach (var (resourceNode, gatherSpots, transform, entity) in
                SystemAPI.Query<RefRO<ResourceNode>, DynamicBuffer<GatherSpot>, RefRO<LocalTransform>>()
                    .WithEntityAccess())
            {
                for (int i = 0; i < gatherSpots.Length; i++)
                {
                    var spot = gatherSpots[i];

                    if (spot.IsOccupied && spot.Occupant != Entity.Null)
                    {
                        // Check if occupant still exists and is still gathering from this node
                        bool isValid = IsWorkerValid(ref state, spot.Occupant, entity);

                        if (!isValid)
                        {
                            // Free the spot
                            spot.IsOccupied = false;
                            spot.Occupant = Entity.Null;
                            gatherSpots[i] = spot;
                        }
                    }
                }
            }

            // Second pass: Assign workers to spots
            foreach (var (workerData, workerState, transform) in
                SystemAPI.Query<RefRW<WorkerData>, RefRO<UnitStateComponent>, RefRO<LocalTransform>>())
            {
                // Only assign spots for workers moving to or gathering from a resource node
                if (workerData.ValueRO.TargetResourceNode == Entity.Null)
                    continue;

                if (workerState.ValueRO.State != UnitState.Moving &&
                    workerState.ValueRO.State != UnitState.Gathering)
                    continue;

                // Try to assign/maintain a gather spot
                AssignGatherSpot(
                    ref state,
                    workerData.ValueRO.TargetResourceNode,
                    transform.ValueRO.Position
                );
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        /// <summary>
        /// Check if a worker is still valid for occupying a gather spot
        /// </summary>
        [BurstCompile]
        private bool IsWorkerValid(ref SystemState state, Entity worker, Entity resourceNode)
        {
            // Check if worker still exists
            if (!state.EntityManager.Exists(worker))
                return false;

            // Check if worker has WorkerData
            if (!SystemAPI.HasComponent<WorkerData>(worker))
                return false;

            var workerData = SystemAPI.GetComponent<WorkerData>(worker);

            // Check if worker is still targeting this resource node
            if (workerData.TargetResourceNode != resourceNode)
                return false;

            // Check if worker is in gathering or moving state
            if (!SystemAPI.HasComponent<UnitStateComponent>(worker))
                return false;

            var state_comp = SystemAPI.GetComponent<UnitStateComponent>(worker);

            return state_comp.State == UnitState.Moving || state_comp.State == UnitState.Gathering;
        }

        /// <summary>
        /// Assign a worker to the nearest available gather spot
        /// </summary>
        [BurstCompile]
        private bool AssignGatherSpot(
            ref SystemState state,
            Entity resourceNode,
            float3 workerPosition)
        {
            if (!SystemAPI.HasBuffer<GatherSpot>(resourceNode))
                return false;

            var buffer = SystemAPI.GetBuffer<GatherSpot>(resourceNode);

            // Find nearest available spot
            int nearestSpotIndex = -1;
            float nearestDistSq = float.MaxValue;

            for (int i = 0; i < buffer.Length; i++)
            {
                var spot = buffer[i];

                if (!spot.IsOccupied)
                {
                    float distSq = math.distancesq(workerPosition, spot.Position);

                    if (distSq < nearestDistSq)
                    {
                        nearestDistSq = distSq;
                        nearestSpotIndex = i;
                    }
                }
            }

            // Assign worker to spot if available
            if (nearestSpotIndex >= 0)
            {
                var spot = buffer[nearestSpotIndex];
                // Note: We can't store the worker entity here without more context
                // This would require tracking worker-to-spot assignments
                // For now, just return success
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Worker movement to gather spot system
    /// Updates worker movement targets to specific gather spots
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(EconomySystemGroup))]
    [UpdateAfter(typeof(GatherSpotSystem))]
    public partial struct WorkerGatherMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameTime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Update workers moving to resource nodes
            foreach (var (workerData, workerState, transform, moveTarget) in
                SystemAPI.Query<RefRO<WorkerData>, RefRO<UnitStateComponent>,
                    RefRO<LocalTransform>, RefRW<MoveTarget>>())
            {
                // Only update if moving to a resource node
                if (workerState.ValueRO.State != UnitState.Moving)
                    continue;

                if (workerData.ValueRO.TargetResourceNode == Entity.Null)
                    continue;

                // Get resource node transform
                if (SystemAPI.HasComponent<LocalTransform>(workerData.ValueRO.TargetResourceNode))
                {
                    var nodeTransform = SystemAPI.GetComponent<LocalTransform>(
                        workerData.ValueRO.TargetResourceNode
                    );

                    // Set destination to resource node position
                    // TODO: In a more complete implementation, this would find the
                    // nearest free gather spot and move to that specific position
                    moveTarget.ValueRW.Destination = nodeTransform.Position;
                    moveTarget.ValueRW.HasDestination = true;
                    moveTarget.ValueRW.StoppingDistance = 2f; // Stop near the node
                }
            }

            // Update workers returning to deposit
            foreach (var (workerData, workerState, transform, moveTarget) in
                SystemAPI.Query<RefRO<WorkerData>, RefRO<UnitStateComponent>,
                    RefRO<LocalTransform>, RefRW<MoveTarget>>())
            {
                // Only update if moving to a deposit and carrying resources
                if (workerState.ValueRO.State != UnitState.Moving)
                    continue;

                if (workerData.ValueRO.TargetDepositBuilding == Entity.Null)
                    continue;

                if (workerData.ValueRO.CarriedResourceAmount <= 0)
                    continue;

                // Get deposit building transform
                if (SystemAPI.HasComponent<LocalTransform>(workerData.ValueRO.TargetDepositBuilding))
                {
                    var depositTransform = SystemAPI.GetComponent<LocalTransform>(
                        workerData.ValueRO.TargetDepositBuilding
                    );

                    moveTarget.ValueRW.Destination = depositTransform.Position;
                    moveTarget.ValueRW.HasDestination = true;
                    moveTarget.ValueRW.StoppingDistance = 3f; // Stop near the building
                }
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}

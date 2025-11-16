using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using DotsRTS.Components.Units;
using DotsRTS.Components.Resources;
using DotsRTS.Components.Movement;
using DotsRTS.Components.Buildings;
using DotsRTS.Bootstrap;

namespace DotsRTS.Systems.Economy
{
    /// <summary>
    /// Worker AI system - handles resource gathering behavior
    /// Workers find resources, gather them, and deposit at storehouses
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(EconomySystemGroup))]
    public partial struct WorkerAISystem : ISystem
    {
        private const float RESOURCE_SEARCH_RADIUS = 100f;
        private const int CELL_SIZE = 20;

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

            // Build spatial hash of resource nodes
            var resourceQuery = SystemAPI.QueryBuilder()
                .WithAll<ResourceNodeTag, LocalTransform, ResourceNode>()
                .Build();

            var resourceCount = resourceQuery.CalculateEntityCount();

            // Build spatial hash of deposit buildings
            var depositQuery = SystemAPI.QueryBuilder()
                .WithAll<ResourceDepositTag, LocalTransform, BuildingData>()
                .Build();

            var depositCount = depositQuery.CalculateEntityCount();

            if (resourceCount == 0 || depositCount == 0)
            {
                // No resources or deposits - workers idle
                return;
            }

            var resourceHash = new NativeParallelMultiHashMap<int, ResourceNodeInfo>(
                resourceCount,
                Allocator.TempJob
            );

            var depositHash = new NativeParallelMultiHashMap<int, DepositInfo>(
                depositCount,
                Allocator.TempJob
            );

            // Build resource hash
            new BuildResourceHashJob
            {
                ResourceHash = resourceHash.AsParallelWriter(),
                CellSize = CELL_SIZE
            }.ScheduleParallel();

            // Build deposit hash
            new BuildDepositHashJob
            {
                DepositHash = depositHash.AsParallelWriter(),
                CellSize = CELL_SIZE
            }.ScheduleParallel();

            state.Dependency.Complete();

            // Update worker AI
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            new WorkerAIJob
            {
                DeltaTime = deltaTime,
                CurrentTime = currentTime,
                ResourceHash = resourceHash,
                DepositHash = depositHash,
                CellSize = CELL_SIZE,
                SearchRadius = RESOURCE_SEARCH_RADIUS,
                ECB = ecb.AsParallelWriter()
            }.ScheduleParallel();

            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);

            resourceHash.Dispose();
            depositHash.Dispose();
            ecb.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }

    /// <summary>
    /// Resource node information for spatial hash
    /// </summary>
    public struct ResourceNodeInfo
    {
        public Entity Entity;
        public float3 Position;
        public ResourceType Type;
        public int AvailableAmount;
        public bool IsFull; // All gather spots occupied
    }

    /// <summary>
    /// Deposit building information
    /// </summary>
    public struct DepositInfo
    {
        public Entity Entity;
        public float3 Position;
        public bool AcceptsWood;
        public bool AcceptsStone;
        public bool AcceptsFood;
        public bool AcceptsGold;
    }

    /// <summary>
    /// Build spatial hash of resource nodes
    /// </summary>
    [BurstCompile]
    public partial struct BuildResourceHashJob : IJobEntity
    {
        public NativeParallelMultiHashMap<int, ResourceNodeInfo>.ParallelWriter ResourceHash;
        public int CellSize;

        [BurstCompile]
        private void Execute(
            Entity entity,
            in LocalTransform transform,
            in ResourceNode resourceNode)
        {
            if (resourceNode.IsDepleted) return;

            int cellX = (int)math.floor(transform.Position.x / CellSize);
            int cellZ = (int)math.floor(transform.Position.z / CellSize);
            int hash = ResourceHash.GetHashCode(new int2(cellX, cellZ));

            bool isFull = resourceNode.GatherersCount >= resourceNode.MaxGatherers;

            ResourceHash.Add(hash, new ResourceNodeInfo
            {
                Entity = entity,
                Position = transform.Position,
                Type = resourceNode.Type,
                AvailableAmount = resourceNode.CurrentAmount,
                IsFull = isFull
            });
        }
    }

    /// <summary>
    /// Build spatial hash of deposit buildings
    /// </summary>
    [BurstCompile]
    public partial struct BuildDepositHashJob : IJobEntity
    {
        public NativeParallelMultiHashMap<int, DepositInfo>.ParallelWriter DepositHash;
        public int CellSize;

        [BurstCompile]
        private void Execute(
            Entity entity,
            in LocalTransform transform,
            in StorehouseData storehouse,
            in BuildingData buildingData)
        {
            if (!buildingData.IsConstructed) return;

            int cellX = (int)math.floor(transform.Position.x / CellSize);
            int cellZ = (int)math.floor(transform.Position.z / CellSize);
            int hash = DepositHash.GetHashCode(new int2(cellX, cellZ));

            DepositHash.Add(hash, new DepositInfo
            {
                Entity = entity,
                Position = transform.Position,
                AcceptsWood = storehouse.AcceptsWood,
                AcceptsStone = storehouse.AcceptsStone,
                AcceptsFood = storehouse.AcceptsFood,
                AcceptsGold = storehouse.AcceptsGold
            });
        }
    }

    /// <summary>
    /// Worker AI decision making
    /// </summary>
    [BurstCompile]
    public partial struct WorkerAIJob : IJobEntity
    {
        public float DeltaTime;
        public float CurrentTime;
        [ReadOnly] public NativeParallelMultiHashMap<int, ResourceNodeInfo> ResourceHash;
        [ReadOnly] public NativeParallelMultiHashMap<int, DepositInfo> DepositHash;
        public int CellSize;
        public float SearchRadius;
        public EntityCommandBuffer.ParallelWriter ECB;

        [BurstCompile]
        private void Execute(
            [ChunkIndexInQuery] int chunkIndex,
            Entity entity,
            in LocalTransform transform,
            ref WorkerData workerData,
            ref UnitStateComponent state,
            ref MoveTarget moveTarget)
        {
            // Worker state machine
            switch (state.State)
            {
                case UnitState.Idle:
                    // Find nearest resource node
                    if (FindNearestResource(transform.Position, ref workerData))
                    {
                        state.State = UnitState.Moving;
                        state.StateStartTime = CurrentTime;
                    }
                    break;

                case UnitState.Moving:
                    // Moving to resource node
                    if (workerData.TargetResourceNode != Entity.Null)
                    {
                        // Check if reached node
                        if (moveTarget.ReachedDestination)
                        {
                            state.State = UnitState.Gathering;
                            state.StateStartTime = CurrentTime;
                            moveTarget.HasDestination = false;
                        }
                    }
                    // Moving to deposit
                    else if (workerData.TargetDepositBuilding != Entity.Null &&
                             workerData.CarriedResourceAmount > 0)
                    {
                        if (moveTarget.ReachedDestination)
                        {
                            // Deposit resources
                            DepositResources(chunkIndex, entity, ref workerData);
                            state.State = UnitState.Idle;
                            moveTarget.HasDestination = false;
                        }
                    }
                    break;

                case UnitState.Gathering:
                    float gatherTime = CurrentTime - state.StateStartTime;
                    float gatherDuration = 1f / workerData.GatherSpeed;

                    if (gatherTime >= gatherDuration)
                    {
                        // Gather resources
                        int gathered = math.min(10, workerData.MaxCarryCapacity);
                        workerData.CarriedResourceAmount = gathered;

                        // Find deposit building
                        if (FindNearestDeposit(transform.Position, ref workerData))
                        {
                            state.State = UnitState.Moving;
                            state.StateStartTime = CurrentTime;
                        }
                        else
                        {
                            // No deposit found - go idle
                            state.State = UnitState.Idle;
                        }
                    }
                    break;
            }
        }

        [BurstCompile]
        private bool FindNearestResource(float3 position, ref WorkerData workerData)
        {
            Entity closestNode = Entity.Null;
            float closestDistSq = float.MaxValue;
            float3 closestPosition = float3.zero;
            ResourceType closestType = ResourceType.None;

            int cellX = (int)math.floor(position.x / CellSize);
            int cellZ = (int)math.floor(position.z / CellSize);
            int searchCells = (int)math.ceil(SearchRadius / CellSize);

            for (int x = -searchCells; x <= searchCells; x++)
            {
                for (int z = -searchCells; z <= searchCells; z++)
                {
                    int hash = ResourceHash.GetHashCode(new int2(cellX + x, cellZ + z));

                    if (ResourceHash.TryGetFirstValue(hash, out var resource, out var iterator))
                    {
                        do
                        {
                            if (resource.IsFull || resource.AvailableAmount <= 0)
                                continue;

                            float distSq = math.distancesq(position, resource.Position);

                            if (distSq < closestDistSq)
                            {
                                closestDistSq = distSq;
                                closestNode = resource.Entity;
                                closestPosition = resource.Position;
                                closestType = resource.Type;
                            }
                        }
                        while (ResourceHash.TryGetNextValue(out resource, ref iterator));
                    }
                }
            }

            if (closestNode != Entity.Null)
            {
                workerData.TargetResourceNode = closestNode;
                workerData.CarriedResourceType = closestType;
                return true;
            }

            return false;
        }

        [BurstCompile]
        private bool FindNearestDeposit(float3 position, ref WorkerData workerData)
        {
            Entity closestDeposit = Entity.Null;
            float closestDistSq = float.MaxValue;
            float3 closestPosition = float3.zero;

            int cellX = (int)math.floor(position.x / CellSize);
            int cellZ = (int)math.floor(position.z / CellSize);
            int searchCells = (int)math.ceil(SearchRadius / CellSize);

            for (int x = -searchCells; x <= searchCells; x++)
            {
                for (int z = -searchCells; z <= searchCells; z++)
                {
                    int hash = DepositHash.GetHashCode(new int2(cellX + x, cellZ + z));

                    if (DepositHash.TryGetFirstValue(hash, out var deposit, out var iterator))
                    {
                        do
                        {
                            // Check if deposit accepts this resource type
                            bool accepts = workerData.CarriedResourceType switch
                            {
                                ResourceType.Wood => deposit.AcceptsWood,
                                ResourceType.Stone => deposit.AcceptsStone,
                                ResourceType.Food => deposit.AcceptsFood,
                                ResourceType.Gold => deposit.AcceptsGold,
                                _ => false
                            };

                            if (!accepts) continue;

                            float distSq = math.distancesq(position, deposit.Position);

                            if (distSq < closestDistSq)
                            {
                                closestDistSq = distSq;
                                closestDeposit = deposit.Entity;
                                closestPosition = deposit.Position;
                            }
                        }
                        while (DepositHash.TryGetNextValue(out deposit, ref iterator));
                    }
                }
            }

            if (closestDeposit != Entity.Null)
            {
                workerData.TargetDepositBuilding = closestDeposit;
                return true;
            }

            return false;
        }

        [BurstCompile]
        private void DepositResources(int chunkIndex, Entity workerEntity, ref WorkerData workerData)
        {
            if (workerData.CarriedResourceAmount <= 0) return;

            // Create resource transaction
            var transactionEntity = ECB.CreateEntity(chunkIndex);
            ECB.AddComponent(chunkIndex, transactionEntity, new ResourceTransaction
            {
                Type = workerData.CarriedResourceType,
                Amount = workerData.CarriedResourceAmount,
                Source = workerEntity,
                IsProcessed = false
            });

            // Clear carried resources
            workerData.CarriedResourceAmount = 0;
            workerData.CarriedResourceType = ResourceType.None;
            workerData.TargetDepositBuilding = Entity.Null;
            workerData.TargetResourceNode = Entity.Null;
        }
    }
}

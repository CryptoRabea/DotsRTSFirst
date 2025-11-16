using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using DotsRTS.Components.Buildings;
using DotsRTS.Components.Combat;
using DotsRTS.Components.Resources;
using DotsRTS.Bootstrap;

namespace DotsRTS.Systems.Building
{
    /// <summary>
    /// Building destruction system - handles building death and cleanup
    /// Updates population caps, removes wall connections, etc.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CleanupSystemGroup))]
    public partial struct BuildingDestructionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameTime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var currentTime = SystemAPI.GetSingleton<GameTime>().ElapsedTime;

            // Process destroyed buildings
            foreach (var (buildingData, health, entity) in
                SystemAPI.Query<RefRO<BuildingData>, RefRW<Health>>()
                    .WithEntityAccess())
            {
                if (health.ValueRO.IsDead)
                {
                    // Handle type-specific destruction logic
                    OnBuildingDestroyed(ref state, ref ecb, entity, buildingData.ValueRO);

                    // Add death event if not already present
                    if (!SystemAPI.HasComponent<DeathEvent>(entity))
                    {
                        ecb.AddComponent(entity, new DeathEvent
                        {
                            DeathTime = currentTime,
                            Killer = Entity.Null,
                            HasBeenProcessed = false
                        });
                    }
                }
            }

            // Process death events
            foreach (var (deathEvent, entity) in
                SystemAPI.Query<RefRW<DeathEvent>>()
                    .WithAll<BuildingTag>()
                    .WithEntityAccess())
            {
                if (!deathEvent.ValueRO.HasBeenProcessed)
                {
                    // Mark as processed
                    deathEvent.ValueRW.HasBeenProcessed = true;

                    // Delay destruction slightly for visual effects
                    float timeSinceDeath = currentTime - deathEvent.ValueRO.DeathTime;
                    if (timeSinceDeath > 1f) // 1 second delay
                    {
                        ecb.DestroyEntity(entity);
                    }
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        /// <summary>
        /// Handle building-specific destruction logic
        /// </summary>
        private void OnBuildingDestroyed(
            ref SystemState state,
            ref EntityCommandBuffer ecb,
            Entity buildingEntity,
            BuildingData buildingData)
        {
            switch (buildingData.Type)
            {
                case BuildingType.House:
                    UpdatePopulationCapOnDestruction(ref state, buildingEntity);
                    break;

                case BuildingType.Wall:
                    UpdateWallConnectionsOnDestruction(ref state, buildingEntity);
                    break;

                case BuildingType.Headquarters:
                    // Game over if HQ destroyed
                    TriggerDefeat(ref state);
                    break;
            }
        }

        /// <summary>
        /// Update population cap when house is destroyed
        /// </summary>
        private void UpdatePopulationCapOnDestruction(ref SystemState state, Entity houseEntity)
        {
            if (!SystemAPI.HasComponent<HouseData>(houseEntity))
                return;

            var houseData = SystemAPI.GetComponent<HouseData>(houseEntity);

            if (SystemAPI.TryGetSingletonRW<GlobalResources>(out var resources))
            {
                resources.ValueRW.MaxPopulation -= houseData.PopulationProvided;
                resources.ValueRW.MaxPopulation = math.max(0, resources.ValueRO.MaxPopulation);
            }
        }

        /// <summary>
        /// Update wall connections when a wall segment is destroyed
        /// </summary>
        private void UpdateWallConnectionsOnDestruction(ref SystemState state, Entity wallEntity)
        {
            if (!SystemAPI.HasComponent<WallData>(wallEntity))
                return;

            var wallData = SystemAPI.GetComponent<WallData>(wallEntity);

            // Disconnect neighboring walls
            DisconnectWall(ref state, wallData.ConnectedWallNorth, wallEntity);
            DisconnectWall(ref state, wallData.ConnectedWallSouth, wallEntity);
            DisconnectWall(ref state, wallData.ConnectedWallEast, wallEntity);
            DisconnectWall(ref state, wallData.ConnectedWallWest, wallEntity);
        }

        /// <summary>
        /// Disconnect a wall from its neighbor
        /// </summary>
        private void DisconnectWall(ref SystemState state, Entity neighborWall, Entity destroyedWall)
        {
            if (neighborWall == Entity.Null) return;
            if (!SystemAPI.HasComponent<WallData>(neighborWall)) return;

            var neighborData = SystemAPI.GetComponent<WallData>(neighborWall);

            // Clear connection references
            if (neighborData.ConnectedWallNorth == destroyedWall)
                neighborData.ConnectedWallNorth = Entity.Null;
            if (neighborData.ConnectedWallSouth == destroyedWall)
                neighborData.ConnectedWallSouth = Entity.Null;
            if (neighborData.ConnectedWallEast == destroyedWall)
                neighborData.ConnectedWallEast = Entity.Null;
            if (neighborData.ConnectedWallWest == destroyedWall)
                neighborData.ConnectedWallWest = Entity.Null;

            SystemAPI.SetComponent(neighborWall, neighborData);
        }

        /// <summary>
        /// Trigger defeat condition
        /// </summary>
        private void TriggerDefeat(ref SystemState state)
        {
            // Update game state to defeat
            if (SystemAPI.TryGetSingletonRW<Components.GameState.GameState>(out var gameState))
            {
                gameState.ValueRW.Phase = Components.GameState.GamePhase.Defeat;
            }

            if (SystemAPI.TryGetSingletonRW<Components.GameState.VictoryConditions>(out var victory))
            {
                victory.ValueRW.HeadquartersDestroyed = true;
            }
        }
    }
}

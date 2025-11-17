using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using DotsRTS.Components.Buildings;
using DotsRTS.Components.Combat;
using DotsRTS.Components.Resources;
using DotsRTS.Bootstrap;
using DotsRTS.Utilities;

namespace DotsRTS.Systems.Building
{
    /// <summary>
    /// Building construction system - handles building progress and completion
    /// Buildings under construction gradually gain health and become functional when complete
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(BuildingSystemGroup))]
    public partial struct BuildingConstructionSystem : ISystem
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
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // Process buildings under construction
            foreach (var (construction, buildingData, health, entity) in
                SystemAPI.Query<RefRW<UnderConstruction>, RefRW<BuildingData>, RefRW<Health>>()
                    .WithEntityAccess())
            {
                // Increase build progress
                construction.ValueRW.BuildProgress += deltaTime / construction.ValueRO.BuildTime;
                construction.ValueRW.BuildProgress = math.clamp(construction.ValueRW.BuildProgress, 0f, 1f);

                // Update building data
                buildingData.ValueRW.ConstructionProgress = construction.ValueRO.BuildProgress;

                // Update health to match construction progress (10% minimum)
                health.ValueRW.Current = math.lerp(
                    health.ValueRO.Maximum * 0.1f,
                    health.ValueRO.Maximum,
                    construction.ValueRO.BuildProgress
                );

                // Check if construction is complete
                if (construction.ValueRO.BuildProgress >= 1f)
                {
                    buildingData.ValueRW.IsConstructed = true;
                    health.ValueRW.Current = health.ValueRO.Maximum;

                    // Remove construction component
                    ecb.RemoveComponent<UnderConstruction>(entity);

                    // Trigger completion events (e.g., update population cap for houses)
                    OnBuildingComplete(ref state, ref ecb, entity, buildingData.ValueRO);
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
        /// Called when a building completes construction
        /// </summary>
        private void OnBuildingComplete(
            ref SystemState state,
            ref EntityCommandBuffer ecb,
            Entity buildingEntity,
            BuildingData buildingData)
        {
            // Type-specific completion logic
            switch (buildingData.Type)
            {
                case BuildingType.House:
                    // Houses increase population cap
                    // This would update a global resources singleton
                    UpdatePopulationCap(ref state, buildingEntity);
                    break;

                case BuildingType.Tower:
                    // Towers can now attack
                    // Already functional via IsConstructed check
                    break;

                case BuildingType.Barracks:
                case BuildingType.ArcheryRange:
                    // Production buildings can now produce units
                    // Already functional via IsConstructed check
                    break;
            }
        }

        /// <summary>
        /// Update population cap when house is built
        /// </summary>
        private void UpdatePopulationCap(ref SystemState state, Entity houseEntity)
        {
            if (!SystemAPI.HasComponent<HouseData>(houseEntity))
                return;

            var houseData = SystemAPI.GetComponent<HouseData>(houseEntity);

            // Update global resources singleton
            if (SystemAPI.TryGetSingletonRW<GlobalResources>(out var resources))
            {
                resources.ValueRW.MaxPopulation += houseData.PopulationProvided;
            }
        }
    }

    /// <summary>
    /// Building placement confirmation system
    /// Converts placement ghosts into actual buildings
    /// </summary>
    [UpdateInGroup(typeof(BuildingSystemGroup))]
    [UpdateBefore(typeof(BuildingConstructionSystem))]
    public partial struct BuildingPlacementConfirmSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // Process placement requests
            foreach (var (request, entity) in
                SystemAPI.Query<RefRO<PlacementRequest>>().WithEntityAccess())
            {
                // Check if player has resources
                if (SystemAPI.TryGetSingletonRW<GlobalResources>(out var resources))
                {
                    // Get building cost (would need to be stored somewhere accessible)
                    // For now, use default costs
                    int woodCost = GetBuildingWoodCost(request.ValueRO.BuildingType);
                    int stoneCost = GetBuildingStoneCost(request.ValueRO.BuildingType);

                    if (resources.ValueRO.Wood >= woodCost &&
                        resources.ValueRO.Stone >= stoneCost)
                    {
                        // Deduct resources
                        resources.ValueRW.Wood -= woodCost;
                        resources.ValueRW.Stone -= stoneCost;

                        // Create building entity
                        CreateBuilding(
                            ref state,
                            ref ecb,
                            request.ValueRO.BuildingType,
                            request.ValueRO.Position,
                            request.ValueRO.PlayerID
                        );
                    }
                }

                // Remove request
                ecb.DestroyEntity(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        private void CreateBuilding(
            ref SystemState state,
            ref EntityCommandBuffer ecb,
            BuildingType buildingType,
            float3 position,
            int playerID)
        {
            // This would ideally instantiate from a prefab
            // For now, create a basic entity structure
            var building = ecb.CreateEntity();

            ecb.AddComponent(building, new BuildingTag());
            ecb.AddComponent(building, LocalTransform.FromPosition(position));
            // Add more components based on building type...
        }

        private int GetBuildingWoodCost(BuildingType type)
        {
            return type switch
            {
                BuildingType.House => 50,
                BuildingType.Barracks => 100,
                BuildingType.Tower => 75,
                BuildingType.Wall => 10,
                _ => 25
            };
        }

        private int GetBuildingStoneCost(BuildingType type)
        {
            return type switch
            {
                BuildingType.Tower => 50,
                BuildingType.Wall => 5,
                _ => 0
            };
        }
    }
}

using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using DotsRTS.Components.Buildings;
using DotsRTS.Components.Combat;
using DotsRTS.Components.Resources;
using DotsRTS.Components.Movement;

namespace DotsRTS.Authoring
{
    /// <summary>
    /// Authoring component for buildings
    /// </summary>
    public class BuildingAuthoring : MonoBehaviour
    {
        [Header("Building Type")]
        public BuildingType buildingType = BuildingType.House;

        [Header("Basic Settings")]
        public int playerID = 0;
        public int2 gridSize = new int2(2, 2);
        public float maxHealth = 500f;

        [Header("Construction")]
        public float constructionTime = 10f;
        public int woodCost = 50;
        public int stoneCost = 0;
        public int goldCost = 0;
        public bool startConstructed = false;

        [Header("House Settings")]
        [ConditionalField("buildingType")]
        public int populationProvided = 5;

        [Header("Tower Settings")]
        [ConditionalField("buildingType")]
        public float towerAttackRange = 25f;
        [ConditionalField("buildingType")]
        public float towerAttackDamage = 30f;
        [ConditionalField("buildingType")]
        public float towerAttackCooldown = 1f;

        [Header("Production Settings")]
        [ConditionalField("buildingType")]
        public float unitProductionTime = 5f;

        [Header("Storehouse Settings")]
        [ConditionalField("buildingType")]
        public bool acceptsWood = true;
        [ConditionalField("buildingType")]
        public bool acceptsStone = true;
        [ConditionalField("buildingType")]
        public bool acceptsFood = true;
        [ConditionalField("buildingType")]
        public bool acceptsGold = true;
    }

    /// <summary>
    /// Baker for Building entities
    /// </summary>
    public class BuildingBaker : Baker<BuildingAuthoring>
    {
        public override void Bake(BuildingAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // Add building tag
            AddComponent<BuildingTag>(entity);

            // Calculate grid position from world position
            var transform = GetComponent<Transform>();
            int2 gridPos = new int2(
                (int)math.round(transform.position.x),
                (int)math.round(transform.position.z)
            );

            // Add building data
            AddComponent(entity, new BuildingData
            {
                Type = authoring.buildingType,
                PlayerID = authoring.playerID,
                ConstructionProgress = authoring.startConstructed ? 1f : 0f,
                IsConstructed = authoring.startConstructed,
                GridPosition = gridPos,
                GridSize = authoring.gridSize
            });

            // Add health
            AddComponent(entity, new Health
            {
                Current = authoring.startConstructed ? authoring.maxHealth : authoring.maxHealth * 0.1f,
                Maximum = authoring.maxHealth,
                IsDead = false,
                LastDamageTime = 0f
            });

            // Add resource cost
            AddComponent(entity, new ResourceCost
            {
                WoodCost = authoring.woodCost,
                StoneCost = authoring.stoneCost,
                FoodCost = 0,
                GoldCost = authoring.goldCost,
                PopulationCost = 0
            });

            // Add construction component if not already constructed
            if (!authoring.startConstructed)
            {
                AddComponent(entity, new UnderConstruction
                {
                    BuildProgress = 0f,
                    BuildTime = authoring.constructionTime,
                    WoodCost = authoring.woodCost,
                    StoneCost = authoring.stoneCost,
                    GoldCost = authoring.goldCost,
                    ResourcesPaid = false
                });
            }

            // Add obstacle component (buildings block movement)
            AddComponent<ObstacleTag>(entity);
            AddComponent(entity, new ObstacleData
            {
                Radius = math.length(new float2(authoring.gridSize.x, authoring.gridSize.y)) * 0.5f,
                BlocksMovement = true,
                BlocksVision = authoring.buildingType != BuildingType.Wall
            });

            // Type-specific components
            switch (authoring.buildingType)
            {
                case BuildingType.Headquarters:
                    AddComponent<HeadquartersTag>(entity);
                    AddComponent<ResourceDepositTag>(entity);
                    AddComponent(entity, new StorehouseData
                    {
                        AcceptsWood = true,
                        AcceptsStone = true,
                        AcceptsFood = true,
                        AcceptsGold = true
                    });
                    break;

                case BuildingType.House:
                    AddComponent<HouseTag>(entity);
                    AddComponent(entity, new HouseData
                    {
                        PopulationProvided = authoring.populationProvided
                    });
                    break;

                case BuildingType.Barracks:
                    AddComponent<BarracksTag>(entity);
                    AddComponent(entity, new ProductionBuilding
                    {
                        CurrentProducingUnit = Entity.Null,
                        ProductionProgress = 0f,
                        ProductionTime = authoring.unitProductionTime,
                        IsProducing = false,
                        QueueCount = 0
                    });
                    break;

                case BuildingType.ArcheryRange:
                    AddComponent<ArcheryRangeTag>(entity);
                    AddComponent(entity, new ProductionBuilding
                    {
                        CurrentProducingUnit = Entity.Null,
                        ProductionProgress = 0f,
                        ProductionTime = authoring.unitProductionTime,
                        IsProducing = false,
                        QueueCount = 0
                    });
                    break;

                case BuildingType.Tower:
                    AddComponent<TowerTag>(entity);
                    AddComponent(entity, new TowerData
                    {
                        AttackRange = authoring.towerAttackRange,
                        AttackDamage = authoring.towerAttackDamage,
                        AttackCooldown = authoring.towerAttackCooldown,
                        LastAttackTime = 0f,
                        CurrentTarget = Entity.Null,
                        CanAttackGround = true,
                        CanAttackAir = false
                    });
                    break;

                case BuildingType.Storehouse:
                    AddComponent<StorehouseTag>(entity);
                    AddComponent<ResourceDepositTag>(entity);
                    AddComponent(entity, new StorehouseData
                    {
                        AcceptsWood = authoring.acceptsWood,
                        AcceptsStone = authoring.acceptsStone,
                        AcceptsFood = authoring.acceptsFood,
                        AcceptsGold = authoring.acceptsGold
                    });
                    break;

                case BuildingType.Wall:
                    AddComponent<WallTag>(entity);
                    AddComponent(entity, new WallData
                    {
                        GridPosition = gridPos,
                        Orientation = WallOrientation.Horizontal,
                        IsCorner = false,
                        IsGate = false,
                        ConnectedWallNorth = Entity.Null,
                        ConnectedWallSouth = Entity.Null,
                        ConnectedWallEast = Entity.Null,
                        ConnectedWallWest = Entity.Null
                    });
                    break;
            }
        }
    }
}

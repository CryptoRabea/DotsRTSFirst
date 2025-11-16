using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using DotsRTS.Components.Units;
using DotsRTS.Components.Combat;
using DotsRTS.Components.Movement;

namespace DotsRTS.Authoring
{
    /// <summary>
    /// Authoring component for basic units
    /// </summary>
    public class UnitAuthoring : MonoBehaviour
    {
        [Header("Unit Type")]
        public bool isWorker;
        public bool isMeleeUnit;
        public bool isRangedUnit;

        [Header("Stats")]
        public float moveSpeed = 5f;
        public float attackDamage = 10f;
        public float attackRange = 1.5f;
        public float attackCooldown = 1f;
        public int populationCost = 1;

        [Header("Health")]
        public float maxHealth = 100f;

        [Header("Ownership")]
        public int playerID = 0;
        public bool isPlayerControlled = true;

        [Header("Worker Settings")]
        [ConditionalField("isWorker")]
        public int maxCarryCapacity = 10;
        [ConditionalField("isWorker")]
        public float gatherSpeed = 1f;
    }

    /// <summary>
    /// Baker for Unit entities
    /// </summary>
    public class UnitBaker : Baker<UnitAuthoring>
    {
        public override void Bake(UnitAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // Add base unit tag
            AddComponent<UnitTag>(entity);

            // Add specific unit type tags
            if (authoring.isWorker)
            {
                AddComponent<WorkerTag>(entity);
                AddComponent(entity, new WorkerData
                {
                    TargetResourceNode = Entity.Null,
                    TargetDepositBuilding = Entity.Null,
                    CarriedResourceType = ResourceType.None,
                    CarriedResourceAmount = 0,
                    MaxCarryCapacity = authoring.maxCarryCapacity,
                    GatherSpeed = authoring.gatherSpeed
                });
            }

            if (authoring.isMeleeUnit)
            {
                AddComponent<MeleeUnitTag>(entity);
            }

            if (authoring.isRangedUnit)
            {
                AddComponent<RangedUnitTag>(entity);
            }

            // Add unit stats
            AddComponent(entity, new UnitStats
            {
                MoveSpeed = authoring.moveSpeed,
                AttackDamage = authoring.attackDamage,
                AttackRange = authoring.attackRange,
                AttackCooldown = authoring.attackCooldown,
                LastAttackTime = 0f,
                PopulationCost = authoring.populationCost
            });

            // Add ownership
            AddComponent(entity, new UnitOwnership
            {
                PlayerID = authoring.playerID,
                TeamID = authoring.playerID,
                IsPlayerControlled = authoring.isPlayerControlled
            });

            // Add state
            AddComponent(entity, new UnitStateComponent
            {
                State = UnitState.Idle,
                StateStartTime = 0f
            });

            // Add health
            AddComponent(entity, new Health
            {
                Current = authoring.maxHealth,
                Maximum = authoring.maxHealth,
                IsDead = false,
                LastDamageTime = 0f
            });

            // Add movement
            AddComponent(entity, new Movement
            {
                Velocity = float3.zero,
                MoveSpeed = authoring.moveSpeed,
                RotationSpeed = 5f,
                Acceleration = 10f,
                CurrentDirection = new float3(0, 0, 1)
            });

            AddComponent(entity, new MoveTarget
            {
                Destination = float3.zero,
                HasDestination = false,
                StoppingDistance = 0.5f,
                ReachedDestination = false
            });

            // Add avoidance
            AddComponent(entity, new Avoidance
            {
                AvoidanceRadius = 1f,
                AvoidanceForce = 2f,
                AvoidUnits = true,
                AvoidBuildings = true
            });

            // Add attack capability
            AddComponent(entity, new CanAttack
            {
                AttackRange = authoring.attackRange,
                AttackDamage = authoring.attackDamage,
                AttackCooldown = authoring.attackCooldown,
                LastAttackTime = 0f,
                DamageType = DamageType.Physical,
                RequiresLineOfSight = false
            });

            AddComponent(entity, new AttackTarget
            {
                Target = Entity.Null,
                LastKnownPosition = float3.zero,
                AcquisitionTime = 0f
            });

            // Add selection capability for player units
            if (authoring.isPlayerControlled)
            {
                AddComponent(entity, new Selectable
                {
                    IsSelected = false,
                    IsHovered = false
                });
            }
        }
    }

    /// <summary>
    /// Conditional field attribute for inspector (placeholder - implement in editor script)
    /// </summary>
    public class ConditionalFieldAttribute : PropertyAttribute
    {
        public string PropertyToCheck;

        public ConditionalFieldAttribute(string propertyToCheck)
        {
            PropertyToCheck = propertyToCheck;
        }
    }
}

using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using DotsRTS.Components.Units;
using DotsRTS.Components.Combat;
using DotsRTS.Components.Movement;

namespace DotsRTS.Authoring
{
    /// <summary>
    /// Authoring component for enemy entities
    /// </summary>
    public class EnemyAuthoring : MonoBehaviour
    {
        [Header("Enemy Type")]
        public EnemyType enemyType = EnemyType.Basic;

        [Header("Stats")]
        public float moveSpeed = 4f;
        public float attackDamage = 15f;
        public float attackRange = 1.5f;
        public float attackCooldown = 1.5f;
        public float maxHealth = 80f;

        [Header("AI")]
        public float retargetInterval = 2f;
        public float threatLevel = 1f;

        [Header("Wave")]
        public int waveNumber = 1;
        public int nightNumber = 1;
    }

    /// <summary>
    /// Baker for Enemy entities
    /// </summary>
    public class EnemyBaker : Baker<EnemyAuthoring>
    {
        public override void Bake(EnemyAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // Add enemy tag
            AddComponent<EnemyTag>(entity);

            // Add specific enemy type tags
            switch (authoring.enemyType)
            {
                case EnemyType.Basic:
                    AddComponent<BasicEnemyTag>(entity);
                    break;
                case EnemyType.Fast:
                    AddComponent<FastEnemyTag>(entity);
                    break;
                case EnemyType.Tank:
                    AddComponent<TankEnemyTag>(entity);
                    break;
                case EnemyType.Siege:
                    AddComponent<SiegeEnemyTag>(entity);
                    break;
            }

            // Add enemy data
            AddComponent(entity, new EnemyData
            {
                Type = authoring.enemyType,
                MoveSpeed = authoring.moveSpeed,
                AttackDamage = authoring.attackDamage,
                AttackRange = authoring.attackRange,
                AttackCooldown = authoring.attackCooldown,
                LastAttackTime = 0f,
                WaveNumber = authoring.waveNumber,
                ThreatLevel = authoring.threatLevel
            });

            // Add enemy AI
            AddComponent(entity, new EnemyAI
            {
                State = EnemyAIState.Spawning,
                CurrentTarget = Entity.Null,
                TargetPosition = float3.zero,
                RetargetTimer = 0f,
                RetargetInterval = authoring.retargetInterval
            });

            // Add wave data
            AddComponent(entity, new WaveData
            {
                WaveNumber = authoring.waveNumber,
                NightNumber = authoring.nightNumber,
                SpawnTime = 0f
            });

            // Add ownership (enemies are always negative player ID)
            AddComponent(entity, new UnitOwnership
            {
                PlayerID = -1,
                TeamID = -1,
                IsPlayerControlled = false
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
                RotationSpeed = 8f,
                Acceleration = 15f,
                CurrentDirection = new float3(0, 0, 1)
            });

            AddComponent(entity, new MoveTarget
            {
                Destination = float3.zero,
                HasDestination = false,
                StoppingDistance = authoring.attackRange * 0.9f,
                ReachedDestination = false
            });

            // Add steering for flock behavior
            AddComponent(entity, new Steering
            {
                DesiredVelocity = float3.zero,
                SteeringForce = float3.zero,
                MaxForce = 20f,
                MaxSpeed = authoring.moveSpeed,
                SeparationWeight = 1.5f,
                AlignmentWeight = 1f,
                CohesionWeight = 1f
            });

            // Add avoidance
            AddComponent(entity, new Avoidance
            {
                AvoidanceRadius = 1f,
                AvoidanceForce = 3f,
                AvoidUnits = true,
                AvoidBuildings = false
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

            // Add flow field for pathfinding
            AddComponent(entity, new FlowFieldAgent
            {
                CurrentCell = int2.zero,
                TargetCell = int2.zero,
                FlowDirection = float3.zero,
                UseFlowField = true
            });
        }
    }
}

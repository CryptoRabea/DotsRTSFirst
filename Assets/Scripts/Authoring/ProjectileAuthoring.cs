using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using DotsRTS.Components.Combat;

namespace DotsRTS.Authoring
{
    /// <summary>
    /// Authoring component for projectiles (used as prefab template)
    /// </summary>
    public class ProjectileAuthoring : MonoBehaviour
    {
        [Header("Projectile Settings")]
        public float speed = 20f;
        public float damage = 10f;
        public DamageType damageType = DamageType.Physical;
        public float maxLifetime = 5f;
        public bool isHoming = true;

        [Header("Area of Effect")]
        public bool hasAOE = false;
        public float aoeRadius = 0f;
    }

    /// <summary>
    /// Baker for Projectile entities
    /// </summary>
    public class ProjectileBaker : Baker<ProjectileAuthoring>
    {
        public override void Bake(ProjectileAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // Add projectile tag
            AddComponent<ProjectileTag>(entity);

            // Add projectile data
            AddComponent(entity, new ProjectileData
            {
                Source = Entity.Null,
                Target = Entity.Null,
                TargetPosition = float3.zero,
                Speed = authoring.speed,
                Damage = authoring.damage,
                DamageType = authoring.damageType,
                MaxLifetime = authoring.maxLifetime,
                SpawnTime = 0f,
                IsHoming = authoring.isHoming,
                HasHit = false
            });

            // Add AOE damage if enabled
            if (authoring.hasAOE)
            {
                AddComponent(entity, new AOEDamage
                {
                    Radius = authoring.aoeRadius,
                    Damage = authoring.damage,
                    DamageType = authoring.damageType,
                    Source = Entity.Null
                });
            }
        }
    }
}

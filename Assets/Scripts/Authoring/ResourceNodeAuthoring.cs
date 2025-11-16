using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using DotsRTS.Components.Resources;
using DotsRTS.Components.Units;
using DotsRTS.Components.Movement;

namespace DotsRTS.Authoring
{
    /// <summary>
    /// Authoring component for resource nodes
    /// </summary>
    public class ResourceNodeAuthoring : MonoBehaviour
    {
        [Header("Resource Type")]
        public ResourceType resourceType = ResourceType.Wood;

        [Header("Resources")]
        public int startingAmount = 1000;
        public int maxAmount = 1000;
        public float regenerationRate = 0f;

        [Header("Gathering")]
        public int maxGatherers = 3;
        public float gatherRadius = 2f;
    }

    /// <summary>
    /// Baker for Resource Node entities
    /// </summary>
    public class ResourceNodeBaker : Baker<ResourceNodeAuthoring>
    {
        public override void Bake(ResourceNodeAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // Add resource node tag
            AddComponent<ResourceNodeTag>(entity);

            // Add resource node data
            AddComponent(entity, new ResourceNode
            {
                Type = authoring.resourceType,
                CurrentAmount = authoring.startingAmount,
                MaxAmount = authoring.maxAmount,
                IsDepleted = authoring.startingAmount <= 0,
                RegenerationRate = authoring.regenerationRate,
                LastRegenerationTime = 0f,
                GatherersCount = 0,
                MaxGatherers = authoring.maxGatherers
            });

            // Add obstacle (can't move through resource nodes)
            AddComponent<ObstacleTag>(entity);
            AddComponent(entity, new ObstacleData
            {
                Radius = authoring.gatherRadius,
                BlocksMovement = true,
                BlocksVision = false
            });

            // Create gather spots around the resource node
            var transform = GetComponent<Transform>();
            var buffer = AddBuffer<GatherSpot>(entity);

            // Create gathering positions in a circle around the node
            int spotCount = authoring.maxGatherers;
            float angleStep = 360f / spotCount;

            for (int i = 0; i < spotCount; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                float3 offset = new float3(
                    math.cos(angle) * authoring.gatherRadius,
                    0f,
                    math.sin(angle) * authoring.gatherRadius
                );

                buffer.Add(new GatherSpot
                {
                    Position = transform.position + (Vector3)offset,
                    IsOccupied = false,
                    Occupant = Entity.Null
                });
            }
        }
    }
}

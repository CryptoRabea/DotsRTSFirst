using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using DotsRTS.Components.Buildings;
using DotsRTS.Components.Movement;
using DotsRTS.Components.Resources;
using DotsRTS.Bootstrap;
using DotsRTS.Config;
using DotsRTS.Utilities;

namespace DotsRTS.Systems.Building
{
    /// <summary>
    /// Building placement system - handles grid-based building placement
    /// Validates placement positions and manages ghost previews
    /// </summary>
    [UpdateInGroup(typeof(BuildingSystemGroup))]
    public partial struct BuildingPlacementSystem : ISystem
    {
        private const int GRID_CELL_SIZE = 1;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameConfig>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<GameConfig>();
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // Update placement ghosts
            foreach (var (ghost, transform, entity) in
                SystemAPI.Query<RefRW<PlacementGhost>, RefRW<LocalTransform>>()
                    .WithEntityAccess())
            {
                // Snap to grid
                float3 snappedPos = MathHelpers.SnapToGrid(
                    transform.ValueRO.Position,
                    config.GridCellSize
                );
                transform.ValueRW.Position = snappedPos;

                // Update grid position
                ghost.ValueRW.GridPosition = MathHelpers.WorldToGrid(
                    snappedPos,
                    config.GridCellSize
                );

                // Check if placement is valid
                bool isValid = CheckPlacementValid(
                    ref state,
                    ghost.ValueRO.GridPosition,
                    ghost.ValueRO.GridSize
                );

                ghost.ValueRW.IsValidPlacement = isValid;
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        /// <summary>
        /// Check if a building can be placed at the specified grid position
        /// </summary>
        private bool CheckPlacementValid(
            ref SystemState state,
            int2 gridPos,
            int2 gridSize)
        {
            // Check if any existing buildings overlap
            foreach (var (buildingData, buildingTransform) in
                SystemAPI.Query<RefRO<BuildingData>, RefRO<LocalTransform>>())
            {
                int2 otherPos = buildingData.ValueRO.GridPosition;
                int2 otherSize = buildingData.ValueRO.GridSize;

                // Check for AABB overlap
                if (GridOverlap(gridPos, gridSize, otherPos, otherSize))
                {
                    return false;
                }
            }

            // Check if any obstacles overlap
            foreach (var (obstacle, obstacleTransform) in
                SystemAPI.Query<RefRO<ObstacleData>, RefRO<LocalTransform>>()
                    .WithNone<BuildingTag>())
            {
                float3 buildingWorldPos = MathHelpers.GridToWorld(gridPos, GRID_CELL_SIZE);
                float distSq = math.distancesq(buildingWorldPos, obstacleTransform.ValueRO.Position);

                float combinedRadius = (math.length(new float2(gridSize.x, gridSize.y)) * 0.5f) +
                                       obstacle.ValueRO.Radius;

                if (distSq < combinedRadius * combinedRadius)
                {
                    return false;
                }
            }

            // TODO: Add terrain checks, max map bounds checks, etc.

            return true;
        }

        /// <summary>
        /// Check if two grid rectangles overlap
        /// </summary>
        private bool GridOverlap(int2 posA, int2 sizeA, int2 posB, int2 sizeB)
        {
            int2 minA = posA;
            int2 maxA = posA + sizeA;
            int2 minB = posB;
            int2 maxB = posB + sizeB;

            return minA.x < maxB.x && maxA.x > minB.x &&
                   minA.y < maxB.y && maxA.y > minB.y;
        }
    }

    /// <summary>
    /// Placement request - add this component to request building placement
    /// </summary>
    public struct PlacementRequest : IComponentData
    {
        public BuildingType BuildingType;
        public float3 Position;
        public int PlayerID;
    }
}

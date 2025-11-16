using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using DotsRTS.Components.Buildings;
using DotsRTS.Bootstrap;
using DotsRTS.Config;
using DotsRTS.Utilities;

namespace DotsRTS.Systems.Building
{
    /// <summary>
    /// Wall placement system - handles special wall snapping and connection logic
    /// Walls snap to each other and automatically determine orientation
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(BuildingSystemGroup))]
    [UpdateAfter(typeof(BuildingPlacementSystem))]
    public partial struct WallPlacementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<GameConfig>();
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // Process wall ghost previews
            foreach (var (ghost, transform, entity) in
                SystemAPI.Query<RefRW<PlacementGhost>, RefRW<LocalTransform>>()
                    .WithEntityAccess())
            {
                if (ghost.ValueRO.BuildingType != BuildingType.Wall)
                    continue;

                // Snap wall to grid (walls are 1x1)
                float3 snappedPos = MathHelpers.SnapToGrid(
                    transform.ValueRO.Position,
                    config.GridCellSize
                );
                transform.ValueRW.Position = snappedPos;

                int2 gridPos = MathHelpers.WorldToGrid(snappedPos, config.GridCellSize);

                // Check for neighboring walls to determine orientation
                var orientation = DetermineWallOrientation(
                    ref state,
                    gridPos,
                    config.GridCellSize
                );

                // Store orientation in ghost (we'd need to add this field to PlacementGhost)
                // For now, update rotation based on orientation
                UpdateWallRotation(ref transform.ValueRW, orientation);
            }

            // Update existing walls when new walls are placed
            UpdateWallConnections(ref state, config.GridCellSize, ref ecb);

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        /// <summary>
        /// Determine wall orientation based on neighboring walls
        /// </summary>
        [BurstCompile]
        private WallOrientation DetermineWallOrientation(
            ref SystemState state,
            int2 gridPos,
            float cellSize)
        {
            bool hasNorth = HasWallAt(ref state, gridPos + new int2(0, 1), cellSize);
            bool hasSouth = HasWallAt(ref state, gridPos + new int2(0, -1), cellSize);
            bool hasEast = HasWallAt(ref state, gridPos + new int2(1, 0), cellSize);
            bool hasWest = HasWallAt(ref state, gridPos + new int2(-1, 0), cellSize);

            int connectionCount = (hasNorth ? 1 : 0) + (hasSouth ? 1 : 0) +
                                  (hasEast ? 1 : 0) + (hasWest ? 1 : 0);

            // Corner if connected on two adjacent sides
            if (connectionCount >= 2)
            {
                if ((hasNorth && hasEast) || (hasEast && hasSouth) ||
                    (hasSouth && hasWest) || (hasWest && hasNorth))
                {
                    return WallOrientation.Corner;
                }
            }

            // Determine orientation based on connections
            if (hasNorth || hasSouth)
                return WallOrientation.Vertical;
            if (hasEast || hasWest)
                return WallOrientation.Horizontal;

            // Default to horizontal
            return WallOrientation.Horizontal;
        }

        /// <summary>
        /// Check if a wall exists at the specified grid position
        /// </summary>
        [BurstCompile]
        private bool HasWallAt(ref SystemState state, int2 gridPos, float cellSize)
        {
            foreach (var wallData in SystemAPI.Query<RefRO<WallData>>())
            {
                if (wallData.ValueRO.GridPosition.Equals(gridPos))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Get wall entity at specified grid position
        /// </summary>
        [BurstCompile]
        private Entity GetWallAt(ref SystemState state, int2 gridPos)
        {
            foreach (var (wallData, entity) in
                SystemAPI.Query<RefRO<WallData>>().WithEntityAccess())
            {
                if (wallData.ValueRO.GridPosition.Equals(gridPos))
                {
                    return entity;
                }
            }
            return Entity.Null;
        }

        /// <summary>
        /// Update wall rotation based on orientation
        /// </summary>
        [BurstCompile]
        private void UpdateWallRotation(ref LocalTransform transform, WallOrientation orientation)
        {
            switch (orientation)
            {
                case WallOrientation.Horizontal:
                    transform.Rotation = quaternion.identity;
                    break;
                case WallOrientation.Vertical:
                    transform.Rotation = quaternion.RotateY(math.PI / 2f);
                    break;
                case WallOrientation.Corner:
                    // Corner piece rotation (could be refined)
                    transform.Rotation = quaternion.RotateY(math.PI / 4f);
                    break;
            }
        }

        /// <summary>
        /// Update connections between adjacent walls
        /// </summary>
        [BurstCompile]
        private void UpdateWallConnections(
            ref SystemState state,
            float cellSize,
            ref EntityCommandBuffer ecb)
        {
            foreach (var (wallData, entity) in
                SystemAPI.Query<RefRW<WallData>>().WithEntityAccess())
            {
                int2 gridPos = wallData.ValueRO.GridPosition;

                // Find and connect to neighboring walls
                Entity northWall = GetWallAt(ref state, gridPos + new int2(0, 1));
                Entity southWall = GetWallAt(ref state, gridPos + new int2(0, -1));
                Entity eastWall = GetWallAt(ref state, gridPos + new int2(1, 0));
                Entity westWall = GetWallAt(ref state, gridPos + new int2(-1, 0));

                // Update connections
                wallData.ValueRW.ConnectedWallNorth = northWall;
                wallData.ValueRW.ConnectedWallSouth = southWall;
                wallData.ValueRW.ConnectedWallEast = eastWall;
                wallData.ValueRW.ConnectedWallWest = westWall;

                // Update orientation based on connections
                WallOrientation newOrientation = DetermineWallOrientation(
                    ref state,
                    gridPos,
                    cellSize
                );
                wallData.ValueRW.Orientation = newOrientation;

                // Check if it's a corner
                int connections = (northWall != Entity.Null ? 1 : 0) +
                                  (southWall != Entity.Null ? 1 : 0) +
                                  (eastWall != Entity.Null ? 1 : 0) +
                                  (westWall != Entity.Null ? 1 : 0);
                wallData.ValueRW.IsCorner = connections >= 2;
            }
        }
    }
}

using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace DotsRTS.Utilities
{
    /// <summary>
    /// Burst-compatible spatial hashing utilities for efficient spatial queries
    /// Used for finding nearby entities without expensive distance checks
    /// </summary>
    [BurstCompile]
    public static class SpatialHash
    {
        /// <summary>
        /// Calculate spatial hash cell index from world position
        /// </summary>
        [BurstCompile]
        public static int2 GetCell(float3 position, float cellSize)
        {
            return new int2(
                (int)math.floor(position.x / cellSize),
                (int)math.floor(position.z / cellSize)
            );
        }

        /// <summary>
        /// Calculate hash key from cell coordinates
        /// </summary>
        [BurstCompile]
        public static int GetHashKey(int2 cell, int gridSize)
        {
            // Wrap to grid size and calculate 1D index
            int x = ((cell.x % gridSize) + gridSize) % gridSize;
            int y = ((cell.y % gridSize) + gridSize) % gridSize;
            return y * gridSize + x;
        }

        /// <summary>
        /// Get hash key directly from position
        /// </summary>
        [BurstCompile]
        public static int GetHashKey(float3 position, float cellSize, int gridSize)
        {
            int2 cell = GetCell(position, cellSize);
            return GetHashKey(cell, gridSize);
        }

        /// <summary>
        /// Get neighboring cells (9 cells in 2D grid including center)
        /// </summary>
        [BurstCompile]
        public static void GetNeighborCells(int2 centerCell, NativeList<int2> outputCells)
        {
            outputCells.Clear();

            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    outputCells.Add(centerCell + new int2(x, y));
                }
            }
        }

        /// <summary>
        /// Get neighboring cell hash keys
        /// </summary>
        [BurstCompile]
        public static void GetNeighborHashKeys(int2 centerCell, int gridSize, NativeList<int> outputKeys)
        {
            outputKeys.Clear();

            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    int2 neighborCell = centerCell + new int2(x, y);
                    outputKeys.Add(GetHashKey(neighborCell, gridSize));
                }
            }
        }

        /// <summary>
        /// Get all cells within a radius (circular area)
        /// </summary>
        [BurstCompile]
        public static void GetCellsInRadius(float3 center, float radius, float cellSize, NativeList<int2> outputCells)
        {
            outputCells.Clear();

            int cellRadius = (int)math.ceil(radius / cellSize);
            int2 centerCell = GetCell(center, cellSize);

            for (int y = -cellRadius; y <= cellRadius; y++)
            {
                for (int x = -cellRadius; x <= cellRadius; x++)
                {
                    // Only add cells within circular radius
                    if (x * x + y * y <= cellRadius * cellRadius)
                    {
                        outputCells.Add(centerCell + new int2(x, y));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Simple spatial hash map component for storing entity positions
    /// Can be used as singleton for global spatial queries
    /// </summary>
    public struct SpatialHashMap : IComponentData
    {
        public float CellSize;
        public int GridSize;
        public bool IsInitialized;

        public static SpatialHashMap Create(float cellSize = 10f, int gridSize = 256)
        {
            return new SpatialHashMap
            {
                CellSize = cellSize,
                GridSize = gridSize,
                IsInitialized = true
            };
        }
    }
}

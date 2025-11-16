using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using DotsRTS.Components.Buildings;
using DotsRTS.Bootstrap;
using DotsRTS.Config;

namespace DotsRTS.Systems.Building
{
    /// <summary>
    /// Building grid management system
    /// Maintains a grid representation of occupied spaces for fast placement validation
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(BuildingSystemGroup), OrderFirst = true)]
    public partial struct BuildingGridSystem : ISystem
    {
        private NativeArray<GridCell> m_Grid;
        private int m_GridSize;
        private bool m_IsInitialized;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameConfig>();
            m_IsInitialized = false;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Initialize grid on first update
            if (!m_IsInitialized)
            {
                var config = SystemAPI.GetSingleton<GameConfig>();
                m_GridSize = config.MapSize;
                m_Grid = new NativeArray<GridCell>(
                    m_GridSize * m_GridSize,
                    Allocator.Persistent
                );

                // Initialize all cells as empty
                for (int i = 0; i < m_Grid.Length; i++)
                {
                    m_Grid[i] = new GridCell
                    {
                        IsOccupied = false,
                        OccupyingEntity = Entity.Null,
                        CellType = GridCellType.Empty
                    };
                }

                m_IsInitialized = true;
            }

            // Clear grid each frame (could be optimized to only update on changes)
            for (int i = 0; i < m_Grid.Length; i++)
            {
                m_Grid[i] = new GridCell
                {
                    IsOccupied = false,
                    OccupyingEntity = Entity.Null,
                    CellType = GridCellType.Empty
                };
            }

            // Mark cells occupied by buildings
            foreach (var (buildingData, entity) in
                SystemAPI.Query<RefRO<BuildingData>>().WithEntityAccess())
            {
                int2 gridPos = buildingData.ValueRO.GridPosition;
                int2 gridSize = buildingData.ValueRO.GridSize;

                // Mark all cells occupied by this building
                for (int y = 0; y < gridSize.y; y++)
                {
                    for (int x = 0; x < gridSize.x; x++)
                    {
                        int2 cellPos = gridPos + new int2(x, y);
                        int index = GetGridIndex(cellPos);

                        if (index >= 0 && index < m_Grid.Length)
                        {
                            m_Grid[index] = new GridCell
                            {
                                IsOccupied = true,
                                OccupyingEntity = entity,
                                CellType = GridCellType.Building
                            };
                        }
                    }
                }
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (m_IsInitialized && m_Grid.IsCreated)
            {
                m_Grid.Dispose();
            }
        }

        /// <summary>
        /// Convert 2D grid position to 1D array index
        /// </summary>
        [BurstCompile]
        private int GetGridIndex(int2 gridPos)
        {
            // Offset to center (grid goes from -size/2 to +size/2)
            int x = gridPos.x + m_GridSize / 2;
            int y = gridPos.y + m_GridSize / 2;

            if (x < 0 || x >= m_GridSize || y < 0 || y >= m_GridSize)
                return -1;

            return y * m_GridSize + x;
        }

        /// <summary>
        /// Check if a grid cell is occupied
        /// </summary>
        [BurstCompile]
        public bool IsOccupied(int2 gridPos)
        {
            int index = GetGridIndex(gridPos);
            if (index < 0 || index >= m_Grid.Length)
                return true; // Out of bounds = occupied

            return m_Grid[index].IsOccupied;
        }

        /// <summary>
        /// Check if a rectangular area is free
        /// </summary>
        [BurstCompile]
        public bool IsAreaFree(int2 gridPos, int2 gridSize)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                for (int x = 0; x < gridSize.x; x++)
                {
                    if (IsOccupied(gridPos + new int2(x, y)))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }

    /// <summary>
    /// Grid cell data
    /// </summary>
    public struct GridCell
    {
        public bool IsOccupied;
        public Entity OccupyingEntity;
        public GridCellType CellType;
    }

    /// <summary>
    /// Types of grid cell occupancy
    /// </summary>
    public enum GridCellType : byte
    {
        Empty,
        Building,
        Obstacle,
        Resource,
        Invalid
    }

    /// <summary>
    /// Singleton component storing grid reference
    /// </summary>
    public struct BuildingGridReference : IComponentData
    {
        public int GridSize;
        public float CellSize;
        public bool IsInitialized;
    }
}

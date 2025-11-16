using Unity.Mathematics;
using Unity.Burst;

namespace DotsRTS.Utilities
{
    /// <summary>
    /// Burst-compatible math utility functions for high-performance calculations
    /// </summary>
    [BurstCompile]
    public static class MathHelpers
    {
        /// <summary>
        /// Calculate distance squared between two 2D points (avoids expensive sqrt)
        /// </summary>
        [BurstCompile]
        public static float DistanceSquared(float2 a, float2 b)
        {
            float2 delta = b - a;
            return math.dot(delta, delta);
        }

        /// <summary>
        /// Calculate distance squared between two 3D points (avoids expensive sqrt)
        /// </summary>
        [BurstCompile]
        public static float DistanceSquared(float3 a, float3 b)
        {
            float3 delta = b - a;
            return math.dot(delta, delta);
        }

        /// <summary>
        /// Calculate distance between two 2D points
        /// </summary>
        [BurstCompile]
        public static float Distance(float2 a, float2 b)
        {
            return math.distance(a, b);
        }

        /// <summary>
        /// Calculate distance between two 3D points
        /// </summary>
        [BurstCompile]
        public static float Distance(float3 a, float3 b)
        {
            return math.distance(a, b);
        }

        /// <summary>
        /// Snap position to grid with specified cell size
        /// </summary>
        [BurstCompile]
        public static float3 SnapToGrid(float3 position, float cellSize)
        {
            return new float3(
                math.round(position.x / cellSize) * cellSize,
                position.y,
                math.round(position.z / cellSize) * cellSize
            );
        }

        /// <summary>
        /// Snap position to grid and return grid coordinates
        /// </summary>
        [BurstCompile]
        public static int2 WorldToGrid(float3 position, float cellSize)
        {
            return new int2(
                (int)math.round(position.x / cellSize),
                (int)math.round(position.z / cellSize)
            );
        }

        /// <summary>
        /// Convert grid coordinates to world position
        /// </summary>
        [BurstCompile]
        public static float3 GridToWorld(int2 gridPos, float cellSize, float yHeight = 0f)
        {
            return new float3(
                gridPos.x * cellSize,
                yHeight,
                gridPos.y * cellSize
            );
        }

        /// <summary>
        /// Calculate direction from point A to point B (normalized)
        /// </summary>
        [BurstCompile]
        public static float3 Direction(float3 from, float3 to)
        {
            float3 dir = to - from;
            return math.normalizesafe(dir);
        }

        /// <summary>
        /// Calculate 2D direction from point A to point B (normalized)
        /// </summary>
        [BurstCompile]
        public static float2 Direction2D(float2 from, float2 to)
        {
            float2 dir = to - from;
            return math.normalizesafe(dir);
        }

        /// <summary>
        /// Lerp between two values
        /// </summary>
        [BurstCompile]
        public static float Lerp(float a, float b, float t)
        {
            return math.lerp(a, b, t);
        }

        /// <summary>
        /// Clamp value between min and max
        /// </summary>
        [BurstCompile]
        public static float Clamp(float value, float min, float max)
        {
            return math.clamp(value, min, max);
        }

        /// <summary>
        /// Clamp value between 0 and 1
        /// </summary>
        [BurstCompile]
        public static float Clamp01(float value)
        {
            return math.clamp(value, 0f, 1f);
        }

        /// <summary>
        /// Calculate angle in radians between two 2D directions
        /// </summary>
        [BurstCompile]
        public static float AngleBetween(float2 from, float2 to)
        {
            return math.atan2(to.y - from.y, to.x - from.x);
        }

        /// <summary>
        /// Rotate a 2D vector by angle (in radians)
        /// </summary>
        [BurstCompile]
        public static float2 Rotate2D(float2 vector, float angleRadians)
        {
            float cos = math.cos(angleRadians);
            float sin = math.sin(angleRadians);
            return new float2(
                vector.x * cos - vector.y * sin,
                vector.x * sin + vector.y * cos
            );
        }

        /// <summary>
        /// Check if point is within bounds (AABB test)
        /// </summary>
        [BurstCompile]
        public static bool IsWithinBounds(float3 point, float3 boundsMin, float3 boundsMax)
        {
            return point.x >= boundsMin.x && point.x <= boundsMax.x &&
                   point.y >= boundsMin.y && point.y <= boundsMax.y &&
                   point.z >= boundsMin.z && point.z <= boundsMax.z;
        }

        /// <summary>
        /// Check if two axis-aligned bounding boxes overlap
        /// </summary>
        [BurstCompile]
        public static bool AABBOverlap(float3 aMin, float3 aMax, float3 bMin, float3 bMax)
        {
            return aMin.x <= bMax.x && aMax.x >= bMin.x &&
                   aMin.y <= bMax.y && aMax.y >= bMin.y &&
                   aMin.z <= bMax.z && aMax.z >= bMin.z;
        }

        /// <summary>
        /// Calculate hash from int2 position (useful for spatial hashing)
        /// </summary>
        [BurstCompile]
        public static uint Hash(int2 position)
        {
            return math.hash(position);
        }

        /// <summary>
        /// Calculate hash from int3 position
        /// </summary>
        [BurstCompile]
        public static uint Hash(int3 position)
        {
            return math.hash(position);
        }
    }
}

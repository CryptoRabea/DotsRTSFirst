using Unity.Mathematics;
using Unity.Burst;

namespace DotsRTS.Utilities
{
    /// <summary>
    /// Burst-compatible random utility functions using Unity.Mathematics.Random
    /// </summary>
    [BurstCompile]
    public static class RandomHelpers
    {
        /// <summary>
        /// Create a new Random instance with a seed based on entity index and time
        /// </summary>
        [BurstCompile]
        public static Random CreateRandom(uint seed)
        {
            // Ensure seed is never 0 (invalid for Unity.Mathematics.Random)
            return new Random(seed == 0 ? 1 : seed);
        }

        /// <summary>
        /// Create a random seed from entity index
        /// </summary>
        [BurstCompile]
        public static uint CreateSeed(int entityIndex)
        {
            uint hash = math.hash(new int2(entityIndex, entityIndex * 7919));
            return hash == 0 ? 1 : hash;
        }

        /// <summary>
        /// Create a random seed from two values
        /// </summary>
        [BurstCompile]
        public static uint CreateSeed(int value1, int value2)
        {
            uint hash = math.hash(new int2(value1, value2));
            return hash == 0 ? 1 : hash;
        }

        /// <summary>
        /// Get random position within a circle (2D)
        /// </summary>
        [BurstCompile]
        public static float2 RandomInCircle(ref Random random, float radius)
        {
            float angle = random.NextFloat(0f, math.PI * 2f);
            float distance = math.sqrt(random.NextFloat()) * radius;
            return new float2(
                math.cos(angle) * distance,
                math.sin(angle) * distance
            );
        }

        /// <summary>
        /// Get random position on circle perimeter (2D)
        /// </summary>
        [BurstCompile]
        public static float2 RandomOnCircle(ref Random random, float radius)
        {
            float angle = random.NextFloat(0f, math.PI * 2f);
            return new float2(
                math.cos(angle) * radius,
                math.sin(angle) * radius
            );
        }

        /// <summary>
        /// Get random position within a sphere (3D)
        /// </summary>
        [BurstCompile]
        public static float3 RandomInSphere(ref Random random, float radius)
        {
            float u = random.NextFloat();
            float v = random.NextFloat();
            float theta = u * 2f * math.PI;
            float phi = math.acos(2f * v - 1f);
            float r = math.pow(random.NextFloat(), 1f / 3f) * radius;

            float sinTheta = math.sin(theta);
            float cosTheta = math.cos(theta);
            float sinPhi = math.sin(phi);
            float cosPhi = math.cos(phi);

            return new float3(
                r * sinPhi * cosTheta,
                r * sinPhi * sinTheta,
                r * cosPhi
            );
        }

        /// <summary>
        /// Get random position on sphere surface (3D)
        /// </summary>
        [BurstCompile]
        public static float3 RandomOnSphere(ref Random random, float radius)
        {
            float u = random.NextFloat();
            float v = random.NextFloat();
            float theta = u * 2f * math.PI;
            float phi = math.acos(2f * v - 1f);

            float sinTheta = math.sin(theta);
            float cosTheta = math.cos(theta);
            float sinPhi = math.sin(phi);
            float cosPhi = math.cos(phi);

            return new float3(
                radius * sinPhi * cosTheta,
                radius * sinPhi * sinTheta,
                radius * cosPhi
            );
        }

        /// <summary>
        /// Get random direction (3D normalized vector)
        /// </summary>
        [BurstCompile]
        public static float3 RandomDirection3D(ref Random random)
        {
            return math.normalizesafe(RandomOnSphere(ref random, 1f));
        }

        /// <summary>
        /// Get random direction (2D normalized vector)
        /// </summary>
        [BurstCompile]
        public static float2 RandomDirection2D(ref Random random)
        {
            return math.normalizesafe(RandomOnCircle(ref random, 1f));
        }

        /// <summary>
        /// Get random position within bounds (AABB)
        /// </summary>
        [BurstCompile]
        public static float3 RandomInBounds(ref Random random, float3 min, float3 max)
        {
            return new float3(
                random.NextFloat(min.x, max.x),
                random.NextFloat(min.y, max.y),
                random.NextFloat(min.z, max.z)
            );
        }

        /// <summary>
        /// Get random boolean with specified probability (0-1)
        /// </summary>
        [BurstCompile]
        public static bool RandomBool(ref Random random, float probability = 0.5f)
        {
            return random.NextFloat() < probability;
        }

        /// <summary>
        /// Get random int in range [min, max) (exclusive max)
        /// </summary>
        [BurstCompile]
        public static int RandomInt(ref Random random, int min, int max)
        {
            return random.NextInt(min, max);
        }

        /// <summary>
        /// Get random float in range [min, max]
        /// </summary>
        [BurstCompile]
        public static float RandomFloat(ref Random random, float min, float max)
        {
            return random.NextFloat(min, max);
        }
    }
}

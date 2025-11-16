using UnityEngine;
using UnityEditor;
using DotsRTS.Authoring;
using DotsRTS.Components.Buildings;
using DotsRTS.Components.GameState;
using DotsRTS.Components.Resources;
using DotsRTS.Components.Units;

namespace DotsRTS.Editor
{
    /// <summary>
    /// Scene auto-builder - creates a complete test scene for the DOTS RTS game
    /// Generates terrain, buildings, spawners, resources, and bootstrap objects
    /// </summary>
    public class SceneAutoBuilder : EditorWindow
    {
        private int mapSize = 200;
        private int numResourceNodes = 20;
        private int numSpawners = 4;
        private bool includeStartingUnits = true;
        private int startingWorkers = 5;

        [MenuItem("DotsRTS/Scene Auto-Builder")]
        public static void ShowWindow()
        {
            GetWindow<SceneAutoBuilder>("RTS Scene Builder");
        }

        private void OnGUI()
        {
            GUILayout.Label("DOTS RTS Scene Auto-Builder", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Map Settings", EditorStyles.boldLabel);
            mapSize = EditorGUILayout.IntField("Map Size", mapSize);
            numResourceNodes = EditorGUILayout.IntField("Resource Nodes", numResourceNodes);
            numSpawners = EditorGUILayout.IntField("Enemy Spawners", numSpawners);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Starting Setup", EditorStyles.boldLabel);
            includeStartingUnits = EditorGUILayout.Toggle("Include Starting Units", includeStartingUnits);
            if (includeStartingUnits)
            {
                startingWorkers = EditorGUILayout.IntField("Starting Workers", startingWorkers);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Build Scene", GUILayout.Height(40)))
            {
                BuildScene();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "This will create a complete test scene with:\n" +
                "- Terrain\n" +
                "- Player Headquarters\n" +
                "- Resource nodes (wood, stone, food)\n" +
                "- Enemy spawners\n" +
                "- Game bootstrap\n" +
                "- Starting workers (optional)",
                MessageType.Info
            );
        }

        private void BuildScene()
        {
            Debug.Log("[SceneAutoBuilder] Building scene...");

            // Create root objects
            CreateBootstrap();
            CreateTerrain();
            CreateHeadquarters();
            CreateResourceNodes();
            CreateSpawners();

            if (includeStartingUnits)
            {
                CreateStartingUnits();
            }

            // Create SubScene for DOTS
            // Note: In a real Unity project, you'd create a SubScene GameObject here
            // For this code-only implementation, we'll document the requirement

            Debug.Log("[SceneAutoBuilder] Scene built successfully!");
            EditorUtility.DisplayDialog(
                "Scene Built",
                "Test scene has been created!\n\n" +
                "NOTE: You need to create a SubScene GameObject manually and add all " +
                "entities to it for DOTS conversion.",
                "OK"
            );
        }

        private void CreateBootstrap()
        {
            var bootstrapObj = new GameObject("GameBootstrap");
            bootstrapObj.AddComponent<Bootstrap.GameBootstrap>();

            Debug.Log("[SceneAutoBuilder] Created GameBootstrap");
        }

        private void CreateTerrain()
        {
            var terrainObj = new GameObject("Terrain");
            var terrainData = new TerrainData();
            terrainData.size = new Vector3(mapSize, 10, mapSize);
            terrainData.heightmapResolution = 513;

            var terrain = terrainObj.AddComponent<Terrain>();
            terrain.terrainData = terrainData;

            var collider = terrainObj.AddComponent<TerrainCollider>();
            collider.terrainData = terrainData;

            // Center terrain
            terrainObj.transform.position = new Vector3(-mapSize / 2f, 0, -mapSize / 2f);

            Debug.Log($"[SceneAutoBuilder] Created terrain ({mapSize}x{mapSize})");
        }

        private void CreateHeadquarters()
        {
            var hqObj = new GameObject("PlayerHQ");
            hqObj.transform.position = Vector3.zero;

            var buildingAuth = hqObj.AddComponent<BuildingAuthoring>();
            buildingAuth.buildingType = BuildingType.Headquarters;
            buildingAuth.playerID = 0;
            buildingAuth.maxHealth = 2000f;
            buildingAuth.startConstructed = true;
            buildingAuth.gridSize = new Unity.Mathematics.int2(4, 4);
            buildingAuth.acceptsWood = true;
            buildingAuth.acceptsStone = true;
            buildingAuth.acceptsFood = true;
            buildingAuth.acceptsGold = true;

            // Visual representation
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(hqObj.transform);
            cube.transform.localScale = new Vector3(4, 3, 4);
            cube.transform.localPosition = new Vector3(0, 1.5f, 0);
            var renderer = cube.GetComponent<Renderer>();
            renderer.material.color = Color.blue;

            Debug.Log("[SceneAutoBuilder] Created Player Headquarters");
        }

        private void CreateResourceNodes()
        {
            var resourceParent = new GameObject("ResourceNodes");

            System.Random random = new System.Random(12345);

            for (int i = 0; i < numResourceNodes; i++)
            {
                // Determine resource type
                ResourceType type;
                Color color;
                int amount;

                float roll = (float)random.NextDouble();
                if (roll < 0.5f) // 50% wood
                {
                    type = ResourceType.Wood;
                    color = new Color(0.4f, 0.2f, 0.0f); // Brown
                    amount = 1000;
                }
                else if (roll < 0.8f) // 30% stone
                {
                    type = ResourceType.Stone;
                    color = Color.gray;
                    amount = 800;
                }
                else // 20% food
                {
                    type = ResourceType.Food;
                    color = Color.yellow;
                    amount = 600;
                }

                // Random position (avoid center where HQ is)
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float distance = 20f + (float)random.NextDouble() * (mapSize / 2f - 30f);
                Vector3 position = new Vector3(
                    Mathf.Cos(angle) * distance,
                    0,
                    Mathf.Sin(angle) * distance
                );

                var nodeObj = new GameObject($"ResourceNode_{type}_{i}");
                nodeObj.transform.SetParent(resourceParent.transform);
                nodeObj.transform.position = position;

                var resourceAuth = nodeObj.AddComponent<ResourceNodeAuthoring>();
                resourceAuth.resourceType = type;
                resourceAuth.startingAmount = amount;
                resourceAuth.maxAmount = amount;
                resourceAuth.regenerationRate = type == ResourceType.Food ? 1f : 0f;
                resourceAuth.maxGatherers = 3;
                resourceAuth.gatherRadius = 2f;

                // Visual representation
                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.SetParent(nodeObj.transform);
                sphere.transform.localScale = Vector3.one * 2f;
                sphere.transform.localPosition = Vector3.up;
                var renderer = sphere.GetComponent<Renderer>();
                renderer.material.color = color;
            }

            Debug.Log($"[SceneAutoBuilder] Created {numResourceNodes} resource nodes");
        }

        private void CreateSpawners()
        {
            var spawnerParent = new GameObject("EnemySpawners");

            // Create spawners at cardinal directions
            SpawnerType[] directions = {
                SpawnerType.North,
                SpawnerType.East,
                SpawnerType.South,
                SpawnerType.West
            };

            for (int i = 0; i < numSpawners && i < 4; i++)
            {
                Vector3 position = Vector3.zero;
                float spawnDistance = mapSize / 2f - 10f;

                switch (directions[i])
                {
                    case SpawnerType.North:
                        position = new Vector3(0, 0, spawnDistance);
                        break;
                    case SpawnerType.East:
                        position = new Vector3(spawnDistance, 0, 0);
                        break;
                    case SpawnerType.South:
                        position = new Vector3(0, 0, -spawnDistance);
                        break;
                    case SpawnerType.West:
                        position = new Vector3(-spawnDistance, 0, 0);
                        break;
                }

                var spawnerObj = new GameObject($"Spawner_{directions[i]}");
                spawnerObj.transform.SetParent(spawnerParent.transform);
                spawnerObj.transform.position = position;

                var spawnerAuth = spawnerObj.AddComponent<SpawnerAuthoring>();
                spawnerAuth.spawnerType = directions[i];
                spawnerAuth.spawnRadius = 10f;
                spawnerAuth.maxActiveEnemies = 10000;
                spawnerAuth.startActive = false;

                // Visual marker
                var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                marker.transform.SetParent(spawnerObj.transform);
                marker.transform.localScale = new Vector3(10, 0.1f, 10);
                marker.transform.localPosition = Vector3.up * 0.1f;
                var renderer = marker.GetComponent<Renderer>();
                renderer.material.color = Color.red;
            }

            Debug.Log($"[SceneAutoBuilder] Created {numSpawners} enemy spawners");
        }

        private void CreateStartingUnits()
        {
            var unitsParent = new GameObject("StartingUnits");

            for (int i = 0; i < startingWorkers; i++)
            {
                float angle = (i / (float)startingWorkers) * Mathf.PI * 2f;
                Vector3 position = new Vector3(
                    Mathf.Cos(angle) * 5f,
                    0,
                    Mathf.Sin(angle) * 5f
                );

                var workerObj = new GameObject($"Worker_{i}");
                workerObj.transform.SetParent(unitsParent.transform);
                workerObj.transform.position = position;

                var unitAuth = workerObj.AddComponent<UnitAuthoring>();
                unitAuth.isWorker = true;
                unitAuth.moveSpeed = 5f;
                unitAuth.maxHealth = 50f;
                unitAuth.playerID = 0;
                unitAuth.isPlayerControlled = true;
                unitAuth.maxCarryCapacity = 10;
                unitAuth.gatherSpeed = 1f;

                // Visual representation
                var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                capsule.transform.SetParent(workerObj.transform);
                capsule.transform.localPosition = Vector3.up;
                var renderer = capsule.GetComponent<Renderer>();
                renderer.material.color = Color.green;
            }

            Debug.Log($"[SceneAutoBuilder] Created {startingWorkers} starting workers");
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonGeneration {
    public enum Algoritmes { BFS, DFS, DFSRandom, DFSRecursive, DFSRandomRecursive, None };

    public class DungeonManager : MonoBehaviour {
        public static DungeonManager instance;

        public DungeonData dungeonData = new();
        
        private enum Generator { Recursive, Async };
        [Header("Dungeon Generator Settings")]
        [SerializeField] private Generator generator = Generator.Recursive;
        public Algoritmes routeLimiter = Algoritmes.DFS;

        public int setSeed = 1234;
        public Vector2Int dungeonSize = new(100, 50);
        public int minRoomSize = 10;
        public int doorWidth = 2;
        public int percentToRemove = 20;

        [Space(10)]
        [Header("Dungeon")]
        //[SerializeField] private GameObject doorPrefab;
        //[SerializeField] private GameObject wallPrefab;
        //[SerializeField] private GameObject floorPrefab;
        //[SerializeField] private GameObject playerPrefab;
        //private GridManager gridManager;
        private GameObject player;
        private GameObject dungeonMesh;
        public GameObject wallMarker;

        [Header("Add case 0 - 16")]
        public List<GameObject> objectPlacementCases;

        [Space(10)]
        [Header("Visualisation")]
        [SerializeField] private int height = 5;
        [SerializeField] private float wireframeFixedUpdate = 1;
        [SerializeField] private bool drawRooms = true;
        [SerializeField] private bool drawInnerWalls = true;
        [SerializeField] private bool drawDoors = true;
        [SerializeField] private bool drawGraph = true;

        [Space(10)]
        [Header("Debug")]
        public int recursiveMaxTreads = 2;

        private DateTime startTime;
        private TimeSpan timeTaken;

        private void Awake() {
            if (instance == null) instance = this;
            else Destroy(gameObject);
        }

        private void Start() {
            ResetDungeon();
            //gridManager = GridManager.instance;
        }

        private void DrawDebug() {
            StartCoroutine(DrawRooms());
            StartCoroutine(DrawDoors());
            StartCoroutine(DrawGraph());

            IEnumerator DrawGraph() {
                while (true) {
                    if (drawGraph) {
                        Color roomNodeColor = Color.magenta;
                        Color doorNodeColor = Color.red;
                        Color edgeColor = Color.yellow;

                        HashSet<DoorData> discovered = new();
                        foreach (RoomData room in dungeonData.GetDungeonRooms()) {
                            Vector3 roomPosition = new(room.Bounds.center.x, 0, room.Bounds.center.y);
                            DebugCircle(roomPosition, roomNodeColor, duration: wireframeFixedUpdate);

                            foreach (DoorData connectedDoor in room.ConnectedDoors) {
                                if (!discovered.Contains(connectedDoor)) {
                                    discovered.Add(connectedDoor);

                                    Vector3 doorPosition = new(connectedDoor.Bounds.center.x, 0, connectedDoor.Bounds.center.y);
                                    DebugCircle(doorPosition, doorNodeColor, duration: wireframeFixedUpdate);
                                }

                                Vector3 connectedDoorPosition = new(connectedDoor.Bounds.center.x, 0, connectedDoor.Bounds.center.y);
                                Debug.DrawLine(roomPosition, connectedDoorPosition, edgeColor, duration: wireframeFixedUpdate);
                            }
                        }
                    }

                    yield return new WaitForSeconds(wireframeFixedUpdate);
                }
            }

            IEnumerator DrawRooms() {
                while (true) {
                    if (drawRooms) {
                        Color roomColour = Color.cyan;

                        foreach (RoomData room in dungeonData.GetDungeonRooms()) {
                            if (drawInnerWalls) {
                                RectInt innerBounds = new(room.Bounds.xMin + 1, room.Bounds.yMin + 1, room.Bounds.width - 2, room.Bounds.height - 2);
                                DebugRectInt(innerBounds, roomColour, height: height, duration: wireframeFixedUpdate);
                            }

                            DebugRectInt(room.Bounds, roomColour, height: height, duration: wireframeFixedUpdate);
                        }
                    }

                    yield return new WaitForSeconds(wireframeFixedUpdate);
                }
            }

            IEnumerator DrawDoors() {
                while (true) {
                    if (drawDoors) {
                        Color doorColour = Color.green;

                        foreach (DoorData door in dungeonData.GetDungeonDoors()) {
                            DebugRectInt(door.Bounds, doorColour, height: height, duration: wireframeFixedUpdate);
                        }
                    }

                    yield return new WaitForSeconds(wireframeFixedUpdate);
                }
            }
        }

        [ContextMenu("Reset Dungeon")]
        public void ResetDungeon() {
            Debug.Log("LOG: Restting dungeon...");

            // stop active processes (if active)
            StopAllCoroutines();
            // clear dungeonData
            dungeonData.Clear();

            foreach (Transform child in transform) Destroy(child.gameObject);

            // destroy created meshes/player and set to null
            if (dungeonMesh != null) {
                Destroy(dungeonMesh);
                dungeonMesh = null;
            }
            if (player != null) {
                Destroy(player);
                player = null;
            }

            // restart debugger
            DrawDebug();

            // set random seed and startRoom at 0x0
            RoomData room = new(new(0, 0, dungeonSize.x, dungeonSize.y));
            dungeonData.AddRoom(room);
        }

        [ContextMenu("Generate Dungeon")]
        public void GenerateDungeon() {
            ResetDungeon();
            StartCoroutine(GenerationLogic());
        }

        private IEnumerator GenerationLogic() {
            Debug.Log("LOG: Generating dungeon...");

            startTime = DateTime.Now;

            // generate dungeon data
            switch (generator) {
                case Generator.Recursive:
                    DungeonGeneratorRecursive generatorScript = gameObject.AddComponent<DungeonGeneratorRecursive>();
                    yield return StartCoroutine(generatorScript.Generate());
                    Destroy(gameObject.GetComponent<DungeonGeneratorRecursive>());
                    break;
                case Generator.Async:
                    Debug.Log("NOT IMPLEMENTED");
                    // Call async generation method
                    break;
            }

            // limit dungeon routes
            switch (routeLimiter) {
                case Algoritmes.BFS:
                    yield return StartCoroutine(dungeonData.RemoveCyclesBFS());
                    break;
                case Algoritmes.DFS:
                    yield return StartCoroutine(dungeonData.RemoveCyclesDFS());
                    break;
                case Algoritmes.DFSRandom:
                    Debug.Log("nope not here yet");
                    //yield return StartCoroutine(dungeonData.RemoveCyclesDFS(randomised: true));
                    break;
                case Algoritmes.DFSRecursive:
                    yield return StartCoroutine(dungeonData.RemoveCyclesDFSRecursive());
                    break;
                case Algoritmes.DFSRandomRecursive:
                    Debug.Log("nope not here yet");
                    //yield return StartCoroutine(dungeonData.RemoveCyclesDFSRecursive(randomised: true));
                    break;
                case Algoritmes.None:
                    break;
            }

            // meshbuilder acording to generated data
            // transforming data to tilemap and using marching square algoritme
            MeshCreation meshbuilber = gameObject.AddComponent<MeshCreation>();
            yield return StartCoroutine(meshbuilber.CreateMesh());

            // generate floor


            DateTime endTime = DateTime.Now;
            timeTaken = endTime - startTime;

            Debug.Log($"Room Generation timer: {timeTaken.TotalSeconds}");
            yield break;
        }

        private static void DebugRectInt(RectInt rectInt, Color color, float duration = 0f, bool depthTest = false, float height = 0f) =>
            DebugExtension.DebugBounds(new Bounds(new Vector3(rectInt.center.x, height * 0.5f, rectInt.center.y), new Vector3(rectInt.width, height, rectInt.height)), color, duration, depthTest);
        private static void DebugCircle(Vector3 position, Color color, float radius = .5f, float duration = 0f, bool depthTest = false) =>
                DebugExtension.DebugCircle(position, color, radius: radius, duration: duration, depthTest: depthTest);
    }
}
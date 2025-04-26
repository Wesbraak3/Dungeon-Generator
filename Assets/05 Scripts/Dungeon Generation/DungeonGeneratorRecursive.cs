using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEditor.PlayerSettings;

namespace DungeonGeneration {    
    public class DungeonGeneratorRecursive : MonoBehaviour {
        private DungeonData dungeonData = new();
        private System.Random random;

        [Header("Room Settings")]
        [SerializeField] private int seed = 1234;
        [SerializeField] private RectInt dungeonSize = new(0, 0, 100, 50);
        [SerializeField] private int minRoomSize = 10;
        [SerializeField] private int doorSize = 2;
        [SerializeField] private int height = 5;
        [SerializeField] private int percentToRemove = 0;
        private enum Algoritmes { BFS, DFS, DFSRandom, None};
        [SerializeField] private Algoritmes algorithme = Algoritmes.BFS;


        [Space(10)]
        [Header("Visualisation")]
        [SerializeField] private float wireframeFixedUpdate = 1;
        [SerializeField] private bool drawRooms = true;
        [SerializeField] private bool drawInnerWalls = true;
        [SerializeField] private bool drawDoors = true;
        [SerializeField] private bool drawGraph = true;

        [Space(10)]
        [Header("Dungeon")]
        [SerializeField] private GameObject doorPrefab;
        [SerializeField] private GameObject wallPrefab;
        [SerializeField] private GameObject floorPrefab;
        [SerializeField] private GameObject playerPrefab;
        private GameObject player;
        private GameObject dungeonObject;

        [Space(10)]
        [Header("Debug")]
        [SerializeField] private int maxTreads = 2;
        [SerializeField] private int activeTreads = 0;
        [SerializeField] private int ActiveSplits = 0;


        private DateTime startTime;
        private TimeSpan timeTaken;
        private bool generating = false;

        private void Start() {
            ResetDungeon();
        }

        private void StartTime() {
            startTime = DateTime.Now;
            generating = true;
        }
        private void StopTime() {
            DateTime endTime = DateTime.Now;
            timeTaken = endTime - startTime;
            generating = false;

            Debug.Log($"Room Generation timer: {timeTaken.TotalSeconds}");
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
                                RectInt innerBounds = new(room.Bounds.xMin + 1, room.Bounds.yMin + 1, room.Bounds.width-2, room.Bounds.height - 2);
                                DebugRectInt(innerBounds, roomColour, height: room.Height, duration: wireframeFixedUpdate);
                            }

                            DebugRectInt(room.Bounds, roomColour, height: room.Height, duration: wireframeFixedUpdate);
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
                            DebugRectInt(door.Bounds, doorColour, height: door.Height, duration: wireframeFixedUpdate);
                        }
                    }

                    yield return new WaitForSeconds(wireframeFixedUpdate);
                }
            }
        }

        public void ResetDungeonButton() => ResetDungeon();
        private RoomData ResetDungeon() {
            StopAllCoroutines();
            dungeonData.Clear();

            if (dungeonObject != null) {
                Destroy(dungeonObject);
                dungeonObject = null;
            }
            if (player != null) {
                Destroy(player);
                player = null;
            }

            generating = false;
            activeTreads = 0;
            DrawDebug();

            random = new System.Random(seed);
            RoomData room = new(dungeonSize, height);
            dungeonData.AddRoom(room);

            return room;
        }

        [ContextMenu("Generate Dungeon")]
        public void GenerateDungeonButton() {
            RoomData rootRoom = ResetDungeon();
            StartCoroutine(GenerateDungeon(rootRoom));
        }

        private IEnumerator GenerateDungeon(RoomData rootRoom) {
            Debug.Log("generating dungeon");
            StartTime();

            // generate dungeon information
            ActiveSplits = 1;
            yield return StartCoroutine(RecursiveSplit(rootRoom, (callback) => {
                ActiveSplits = 0;
            }));

            yield return StartCoroutine(RemoveSmallRooms(dungeonData.GetDungeonRooms(), percentage: percentToRemove));

            switch (algorithme) {
                case Algoritmes.BFS:
                    Debug.Log("BFS");
                    yield return StartCoroutine(BFS());
                    break;
                case Algoritmes.DFS:
                    Debug.Log("DFS");
                    yield return StartCoroutine(DFS());
                    break;
                case Algoritmes.DFSRandom:
                    Debug.Log("DFS Random");
                    yield return StartCoroutine(DFSRandom());
                    break;
                case Algoritmes.None:
                    break;
            }

            // make dungeon physical
            // setup hirarchy
            // create grid

            dungeonObject = new("== Dungeon ==", typeof(Grid)) { transform = { position = Vector3.zero } };
            GameObject structureObject = new("== Structure ==") { transform = { parent = dungeonObject.transform } };
            Grid grid = dungeonObject.GetComponent<Grid>();

            Queue<(Vector2Int, GameObject)> dungeonStruct = new();
            Dictionary<Vector2, GameObject> placedItems = new();

            foreach (DoorData door in dungeonData.GetDungeonDoors()) {
                Vector2Int doorMin = door.Bounds.min;
                Vector2Int doorMax = door.Bounds.max;

                QueDoorPlacement(doorMin, doorMax);
            }

            foreach (RoomData room in dungeonData.GetDungeonRooms()) {
                Vector2Int roomMin = room.Bounds.min;
                Vector2Int roomMax = room.Bounds.max;

                QueWallPlacement(roomMin, roomMax);
                QueFoorPlacement(roomMin, roomMax);
            }

            PlaceObjects(dungeonStruct);


            // create doors
            void QueDoorPlacement(Vector2Int bottomLeft, Vector2Int topRight) {
                topRight = new(topRight.x - 1, topRight.y - 1);
                QueFromTo(bottomLeft, topRight, doorPrefab);
            }

            // create walls
            void QueWallPlacement(Vector2Int bottomLeft, Vector2Int topRight) {
                topRight = new(topRight.x - 1, topRight.y - 1);
                Vector2Int topLeft = new (bottomLeft.x, topRight.y);
                Vector2Int bottomRight = new (topRight.x, bottomLeft.y);

                QueFromTo(bottomLeft, bottomRight, wallPrefab);
                QueFromTo(topLeft, topRight, wallPrefab);
                QueFromTo(bottomLeft, topLeft, wallPrefab);
                QueFromTo(bottomRight, topRight, wallPrefab);                
            }

            // create floor
            void QueFoorPlacement(Vector2Int bottomLeft, Vector2Int topRight) {
                bottomLeft = new(bottomLeft.x + 1, bottomLeft.y + 1);
                topRight = new(topRight.x - 2, topRight.y - 2);

                int roomHeight = topRight.y - bottomLeft.y;
                for (int i = 0; i <= roomHeight; i++) {
                    int row = bottomLeft.y + i;
                    Vector2Int left = new (bottomLeft.x, row);
                    Vector2Int right = new(topRight.x, row);

                    QueFromTo(left, right, floorPrefab);
                }
            }

            void QueFromTo(Vector2Int from, Vector2Int to, GameObject prefab) {
                if (from.x == to.x) {
                    for (int y = from.y; y <= to.y; y++) {
                        Vector2Int pos = new(from.x, y);
                        dungeonStruct.Enqueue((pos, prefab));
                    }
                }
                else if (from.y == to.y) {
                    for (int x = from.x; x <= to.x; x++) {
                        Vector2Int pos = new(x, from.y);
                        dungeonStruct.Enqueue((pos, prefab));
                    }
                }
                else {
                    Debug.LogWarning($"Invalid range between {from} and {to}. Neither vertical nor horizontal.");
                }
            }

            void PlaceObjects(Queue<(Vector2Int, GameObject)> Q) {
                while (Q.Count > 0) {
                    var (pos, wallPrefab) = Q.Dequeue();

                    if (placedItems.ContainsKey(pos)) continue;

                    GameObject instantiated = Instantiate(wallPrefab, new Vector3(pos.x, 0, pos.y), Quaternion.identity);
                    instantiated.transform.SetParent(structureObject.transform);
                    placedItems.Add(pos, instantiated);
                }
            }

            // spawn player
            Vector2 spawnLocation = dungeonData.GetDungeonRooms()[0].Bounds.center;
            Vector3 localPosition = grid.WorldToCell(spawnLocation);
            player = Instantiate(playerPrefab, new Vector3(localPosition.x, 0, localPosition.y), Quaternion.identity);

            StopTime();

        #region Local Functions

        // O(N) equal splits
        // O(n*m) with door placement
        IEnumerator RecursiveSplit(RoomData roomData, Action<List<RoomData>> callback) {
                RectInt room = roomData.Bounds;

                bool splitH = room.height / 2 > minRoomSize;
                bool splitV = room.width / 2 > minRoomSize;

                if (!splitH && !splitV) {
                    callback(new() { roomData });
                    yield break;
                }

                RectInt a, b;
                if (!splitH || (splitV && room.width >= room.height * 2)) (a, b) = VSplit(room);
                else if (!splitV || (splitH && room.height >= room.width * 2)) (a, b) = HSplit(room);
                else {
                    bool verticalSplit = random.Next(0, 2) == 0;
                    if (verticalSplit) (a, b) = VSplit(room);
                    else (a, b) = HSplit(room);
                }

                RoomData roomA = new(a, height);
                RoomData roomB = new(b, height);

                dungeonData.RemoveRoom(roomData);
                dungeonData.AddRoom(roomA);
                dungeonData.AddRoom(roomB);

                List<RoomData> splitRoomsA = new();
                List<RoomData> splitRoomsB = new();

                ActiveSplits += 2;
                if (activeTreads < maxTreads) {
                    activeTreads += 1;
                    StartCoroutine(RecursiveSplit(roomA, (splitCallback) => {
                        splitRoomsA = splitCallback;
                        activeTreads -= 1;
                        ActiveSplits -= 1;
                    }));
                }
                else {
                    yield return StartCoroutine(RecursiveSplit(roomA, (splitCallback) => {
                        splitRoomsA = splitCallback;
                        ActiveSplits -= 1;
                    }));
                }
                StartCoroutine(RecursiveSplit(roomB, (splitCallback) => {
                    splitRoomsB = splitCallback;
                    ActiveSplits -= 1;
                }));

                yield return new WaitUntil(() => splitRoomsA.Count != 0 && splitRoomsB.Count != 0);
                StartCoroutine(AddDoors(splitRoomsA, splitRoomsB));

                splitRoomsA.AddRange(splitRoomsB);
                callback(splitRoomsA);

                (RectInt, RectInt) VSplit(RectInt room) {
                    int randomInt = random.Next(minRoomSize, room.width - minRoomSize + 1);

                    int splitA = randomInt;
                    int splitB = room.width - randomInt;

                    RectInt a = new(room.x, room.y, splitA, room.height);
                    RectInt b = new(room.x + splitA - 1, room.y, splitB + 1, room.height);
                    return (
                        new(room.x, room.y, splitA, room.height),
                        new(room.x + splitA - 1, room.y, splitB + 1, room.height)
                    );
                }
                (RectInt, RectInt) HSplit(RectInt room) {
                    int randomInt = random.Next(minRoomSize, room.height - minRoomSize + 1);

                    int splitA = randomInt;
                    int splitB = room.height - randomInt;

                    return (
                        new(room.x, room.y, room.width, splitA),
                        new(room.x, room.y + splitA - 1, room.width, splitB + 1)
                    );
                }

                // Big O(n*m)
                IEnumerator AddDoors(List<RoomData> splitRoomsA, List<RoomData> splitRoomsB) {
                    int doorSpace = doorSize + 4;

                    // Add doors between rooms
                    foreach (RoomData roomA in splitRoomsA) {
                        foreach (RoomData roomB in splitRoomsB) {
                            RectInt? intersection = Intersect(roomA.Bounds, roomB.Bounds);
                            if (!intersection.HasValue) continue;

                            RectInt rect = intersection.Value;
                            // Horizontal door
                            if (rect.height == 1) {
                                if (rect.width < doorSpace) continue;

                                // between xmin + wallThickness * 2 and xmax - (wallThickness * 2 + doorSize)  
                                int randomX = random.Next(rect.xMin + 2, rect.xMax - (doorSize + 2));

                                DoorData door = new(new(randomX, rect.y, doorSize, 1), height, roomA, roomB);
                                dungeonData.AddDoor(door);
                            }
                            // Vertical door
                            else if (rect.width == 1) {
                                if (rect.height < doorSpace) continue;

                                // between xmin + wallThickness * 2 and xmax - (wallThickness * 2 + doorSize)  
                                int randomY = random.Next(rect.yMin + 2, rect.yMax - (doorSize + 2));

                                DoorData door = new(new(rect.x, randomY, 1, doorSize), height, roomA, roomB);
                                dungeonData.AddDoor(door);
                            }
                        }
                    }

                    yield break;

                    // Big O(1)
                    RectInt? Intersect(RectInt a, RectInt b) {
                        int x = Mathf.Max(a.xMin, b.xMin);
                        int y = Mathf.Max(a.yMin, b.yMin);
                        int width = Mathf.Min(a.xMax, b.xMax) - x;
                        int height = Mathf.Min(a.yMax, b.yMax) - y;

                        return (width > 0 && height > 0) ? new(x, y, width, height) : null;
                    }
                }
            }

            //O(n log n)
            IEnumerator RemoveSmallRooms(List<RoomData> roomList, int percentage = 0) {
                int totalRooms = roomList.Count;
                int removeRooms = totalRooms * percentage / 100;

                if (removeRooms == 0)
                    yield break;

                LinkedList<RoomData> sortedRooms = new(roomList.OrderBy(room => room.Surface));

                List<RoomData> removedRooms = new();
                while (removeRooms > 0) {
                    foreach (RoomData room in removedRooms) sortedRooms.Remove(room);

                    foreach (RoomData room in sortedRooms) {
                        if (removeRooms == 0) break;

                        bool removed = dungeonData.RemoveRoom(room, checkIfCreatesIsland: true);
                        if (removed) {
                            removedRooms.Add(room);
                            removeRooms--;;
                        } 
                    }
                }

                yield break;
            }


            [ContextMenu("Run BFS")]
            IEnumerator BFS() {
                dungeonData.RemoveCyclesBFS();
                yield break;
            }

            [ContextMenu("Run DFS")]
            IEnumerator DFS() {
                dungeonData.RemoveCyclesDFS();
                yield break;
            }

            [ContextMenu("Run DFS Random")]
            IEnumerator DFSRandom() {
                dungeonData.RemoveCyclesDFS(randomised:true);
                yield break;
            }
            
            #endregion
        }


        private static void DebugRectInt(RectInt rectInt, Color color, float duration = 0f, bool depthTest = false, float height = 0f) =>
            DebugExtension.DebugBounds(new Bounds(new Vector3(rectInt.center.x, height * 0.5f, rectInt.center.y), new Vector3(rectInt.width, height, rectInt.height)), color, duration, depthTest);
        private static void DebugCircle(Vector3 position, Color color, float radius = .5f, float duration = 0f, bool depthTest = false) =>
                DebugExtension.DebugCircle(position, color, radius:radius, duration: duration, depthTest: depthTest);
    }
}
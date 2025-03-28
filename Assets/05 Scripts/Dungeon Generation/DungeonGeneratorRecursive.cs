using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGeneration {
    public class DungeonGeneratorRecursive : MonoBehaviour {
        private DungeonData dungeonData = new();
        private System.Random random;

        [Header("Room Settings")]
        [SerializeField] private int seed = 1234;
        [SerializeField] private RectInt dungeonSize = new(0, 0, 100, 50);
        [SerializeField] private int minRoomSize = 10;
        [SerializeField] private int maxRoomSize = 20;
        [SerializeField] private int doorSize = 2;
        [SerializeField] private int height = 5;
        
        [Space(10)]
        [Header("Visualisation")]
        [SerializeField] private float wireframeFixedUpdate = 0;
        [SerializeField] private bool drawRooms = true;
        [SerializeField] private bool drawGraph = true;
        [SerializeField] private bool drawDoors = true;

        [SerializeField] private bool randomColour = false;


        [Space(10)]
        [Header("Debug")]
        [SerializeField] private int ActiveSplits = 0;

        private DateTime startTime;
        private TimeSpan timeTaken;
        private bool generating = false;

        private void Start() {
            ResetDungeon();
        }
        private void Update() {
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



        [ContextMenu("draw debug")]
        private void DrawDebug() {
            RectInt innerWall = dungeonSize;
            innerWall.x = dungeonSize.x + 1;
            innerWall.y = dungeonSize.y + 1;
            innerWall.width = dungeonSize.width - 2;
            innerWall.height = dungeonSize.height - 2;

            DebugRectInt(innerWall, Color.blue, height: height, duration: wireframeFixedUpdate);

            HashSet<(RoomData, RoomData)> discovered = new();
            foreach (RoomData room in dungeonData.GetDungeonRooms()) {
                RectInt roomBounds = room.Bounds;
                Color finishedRoomColour = randomColour ? new(
                        UnityEngine.Random.value,
                        UnityEngine.Random.value,
                        UnityEngine.Random.value
                ) : Color.cyan;
                bool splitH = roomBounds.height / 2 <= minRoomSize;
                bool splitV = roomBounds.width / 2 <= minRoomSize;
                if (splitH && splitV) {
                    Vector3 roomPosition = new Vector3(roomBounds.center.x, 0, roomBounds.center.y);

                    if (drawGraph) {
                        foreach (RoomData connectedRoom in room.ConnectedRooms) {
                            if (discovered.Contains((connectedRoom, room))) {
                                //Debug.Log("discoverred");
                                continue;
                            }
                            discovered.Add((room, connectedRoom));
                            Vector3 connectedRoomPosition = new Vector3(connectedRoom.Bounds.center.x, 0, connectedRoom.Bounds.center.y);
                            Vector3 direction = (connectedRoomPosition - roomPosition);
                            DebugArrow(roomPosition, direction, Color.white, duration: wireframeFixedUpdate);
                        }
                        DebugPoint(roomPosition, Color.magenta, duration: wireframeFixedUpdate);
                    }
                    if (drawRooms) {
                        DebugRectInt(roomBounds, finishedRoomColour, height: room.Height, duration: wireframeFixedUpdate);
                    }
                }
                else {
                    DebugRectInt(room.Bounds, Color.blue, height: room.Height, duration: wireframeFixedUpdate);
                }
            }
            if (drawDoors) {
                foreach (DoorData door in dungeonData.GetDungeonDoors()) {
                    if (door.IsLocked) {
                        DebugRectInt(door.Bounds, Color.red, height: door.Height, duration: wireframeFixedUpdate);
                    }
                    else {
                        DebugRectInt(door.Bounds, Color.red, height: door.Height, duration: wireframeFixedUpdate);
                    }
                }
            }
        }

        IEnumerator DrawDungeon() {
            DrawDebug();
            while (true) {
                DrawDebug();
                yield return new WaitForSeconds(wireframeFixedUpdate);
            }
        }

        public void ResetDungeonButton() => ResetDungeon();
        private RoomData ResetDungeon() {
            StopAllCoroutines();
            dungeonData.Clear();

            generating = false;

            StartCoroutine(DrawDungeon());
            random = new System.Random(seed);
            RoomData room = new(dungeonSize, height);
            dungeonData.AddRoom(room);

            return room;
        }

        public void GenerateDungeonButton() => StartCoroutine(GenerateDungeon());
        private IEnumerator GenerateDungeon() {
            Debug.Log("generating dungeon");

            RoomData rootRoom = ResetDungeon();

            StartTime();
            ActiveSplits = 1;
            yield return StartCoroutine(RecursiveSplit(rootRoom, (callback) => {
                ActiveSplits--;
                StopTime();
            }));

            Debug.Log("after recursion fine");
            yield return new WaitUntil(() => !generating);
            Debug.Log("waituntil good");
            yield return new WaitWhile(() => generating);
            Debug.Log("waitwhile good");
        }

        private IEnumerator RecursiveSplit(RoomData roomData, Action<List<RoomData>> callback) {
            yield return null;

            RectInt room = roomData.Bounds;

            bool splitH = room.height / 2 > minRoomSize;
            bool splitV = room.width / 2 > minRoomSize;

            if (!splitH && !splitV) {
                List<RoomData> finishedRoomList = new() { roomData };
                callback(finishedRoomList);
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
            StartCoroutine(RecursiveSplit(roomA, (splitCallback) => {
                splitRoomsA = splitCallback;
                ActiveSplits--;
            }));
            StartCoroutine(RecursiveSplit(roomB, (splitCallback) => {
                splitRoomsB = splitCallback;
                ActiveSplits--;
            }));

            yield return new WaitUntil(() => splitRoomsA.Count != 0 && splitRoomsB.Count != 0);
            StartCoroutine(AddConnections(splitRoomsA, splitRoomsB));

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

            IEnumerator AddConnections(List<RoomData> splitRoomsA, List<RoomData> splitRoomsB) {
                int doorSpace = doorSize + 4;
                foreach (RoomData roomA in splitRoomsA) {
                    foreach (RoomData roomB in splitRoomsB) {
                        RectInt? intersection = Intersect(roomA.Bounds, roomB.Bounds);
                        if (!intersection.HasValue) continue;

                        RectInt rect = intersection.Value;
                        if (intersection.HasValue && rect.height >= doorSpace || rect.width >= doorSpace)
                            roomA.AddConnection(roomA, roomB);
                    }
                }

                yield break;

                RectInt? Intersect(RectInt a, RectInt b) {
                    int x = Mathf.Max(a.xMin, b.xMin);
                    int y = Mathf.Max(a.yMin, b.yMin);
                    int width = Mathf.Min(a.xMax, b.xMax) - x;
                    int height = Mathf.Min(a.yMax, b.yMax) - y;

                    return (width > 0 && height > 0) ? new(x, y, width, height) : null;
                }
            }
        }

        private IEnumerator AddDoors(List<RoomData> splitRoomsA, List<RoomData> splitRoomsB) {
            int doorSpace = doorSize + 4;
            foreach (RoomData roomA in splitRoomsA) {
                foreach (RoomData roomB in splitRoomsB) {
                    RectInt? intersection = Intersect(roomA.Bounds, roomB.Bounds);

                    if (!intersection.HasValue) {
                        continue;
                    }

                    RectInt rect = intersection.Value;
                    // Horizontal door
                    if (rect.height == 1 && rect.width >= doorSpace) {
                        // place door at
                        int randomX = random.Next(rect.xMin + 2, rect.xMax - (doorSize + 2));

                        DoorData door = new(new(randomX, rect.y, doorSize, 1), height, roomA, roomB);
                        dungeonData.AddDoor(door);
                    }
                    // Vertical door
                    else if (rect.width == 1 && rect.height >= doorSpace) {
                        // place door at
                        int randomY = random.Next(rect.yMin + 2, rect.yMax - (doorSize + 2));

                        DoorData door = new(new(rect.x, randomY, 1, doorSize), height, roomA, roomB);
                        dungeonData.AddDoor(door);
                    }
                }
            }

            yield break;

            RectInt? Intersect(RectInt a, RectInt b) {
                int x = Mathf.Max(a.xMin, b.xMin);
                int y = Mathf.Max(a.yMin, b.yMin);
                int width = Mathf.Min(a.xMax, b.xMax) - x;
                int height = Mathf.Min(a.yMax, b.yMax) - y;

                return (width > 0 && height > 0) ? new(x, y, width, height) : null;
            }
        }


        [ContextMenu("Run BFS")]
        private void BFS() => dungeonData.RemoveCyclesBFS();

        [ContextMenu("Run DFS")]
        private void DFS() => dungeonData.RemoveCyclesDFS();
        private static void DebugRectInt(RectInt rectInt, Color color, float duration = 0f, bool depthTest = false, float height = 0.01f) =>
            DebugExtension.DebugBounds(new Bounds(new Vector3(rectInt.center.x, 0, rectInt.center.y), new Vector3(rectInt.width, height, rectInt.height)), color, duration, depthTest);
        private static void DebugArrow(Vector3 position, Vector3 direction, Color color, float duration = 0f, bool depthTest = false, float height = 0.01f) =>
            DebugExtension.DebugArrow(position, direction, color, duration: duration, depthTest: depthTest);
        private static void DebugPoint(Vector3 position, Color color, float scale = 1, float duration = 0f, bool depthTest = false, float height = 0.01f) =>
            DebugExtension.DebugPoint(position, color, scale:scale, duration:duration, depthTest:depthTest);
    }
}
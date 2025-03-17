using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

namespace DungeonGeneration {
    public class DungeonGeneratorRecursive : MonoBehaviour {
        [SerializeField] DungeonData dungeonData = new();

        #region Room Settings
        [Header("Room Settings")]
        [SerializeField] private RectInt initialRoomSize = new(0, 0, 100, 50);
        [SerializeField] private int minRoomSize = 10;
        [SerializeField] private int roomHeight = 5;

        [SerializeField] private bool placeDoors = true;
        [SerializeField] private int doorSize = 2;
        [SerializeField] private int doorHeight = 5;
        #endregion

        public HashSet<(RoomData, RoomData)> processedConnections = new();

        public int ActiveSplits = 0;
        public int maxThreads = 1;
        private int currentTreads = 1;

        public int seed = 1234;
        private System.Random random;

        public float updateWireframe = 5;

        public DateTime startTime;
        public TimeSpan timeTaken;
        public bool generating = false;


        private void Start() {
            
        }
        private void Update() {
        }
        void StopTime() {
            DateTime endTime = DateTime.Now;
            timeTaken = endTime - startTime;
            Debug.Log($"Time taken: {timeTaken.TotalSeconds} seconds");
        }


        private void DrawDebug() {
            foreach (RoomData room in dungeonData.GetDungeonRooms()) {
                RectInt roomBounds = room.Bounds;
                // Calculate posible splits
                bool splitH = roomBounds.height / 2 <= minRoomSize;
                bool splitV = roomBounds.width / 2 <= minRoomSize;

                RectInt innerWall = initialRoomSize;
                innerWall.x = initialRoomSize.x + 1;
                innerWall.y = initialRoomSize.y + 1;
                innerWall.width = initialRoomSize.width - 2;
                innerWall.height = initialRoomSize.height - 2;

                DebugRectInt(innerWall, Color.blue, height: roomHeight, duration: updateWireframe);
                if (splitH && splitV) {
                    DebugRectInt(room.Bounds, Color.cyan, height: room.Height, duration: updateWireframe);
                }
                else {
                    DebugRectInt(room.Bounds, Color.blue, height: room.Height, duration: updateWireframe);
                }
            }
            foreach (DoorData door in dungeonData.GetDungeonDoors()) {
                if (door.IsLocked) {
                    DebugRectInt(door.Bounds, Color.red, height: door.Height, duration: updateWireframe);
                }
                else {
                    DebugRectInt(door.Bounds, Color.red, height: door.Height, duration: updateWireframe);
                }
            }
        }

        IEnumerator DrawDungeon() {
            DrawDebug();
            while (true) {
                DrawDebug();
                yield return new WaitForSeconds(updateWireframe);
            }
        }

        public void GenerateDungeonButton() => StartCoroutine(GenerateDungeon());
        private IEnumerator GenerateDungeon() {
            Debug.Log("generating dungeon");

            RoomData rootRoom = ResetDungeon();
            generating = true;

            StartCoroutine(Generate(rootRoom));
            yield return new WaitWhile(() => generating);
            
            Debug.Log("Generating done");
        }


        IEnumerator Generate(RoomData rootRoom) {
            ActiveSplits = 1;
            yield return StartCoroutine(RecursiveSplit(rootRoom, (callback) => {
                ActiveSplits--;
                generating = false;
                StopTime();
            }));

            IEnumerator RecursiveSplit(RoomData roomData, Action<List<RoomData>> callback) {
                yield return new WaitForSeconds(Time.deltaTime);

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
                    bool verticalSplit = random.Next(0, 2) == 0; // Fix: `random.Next(0,1)` is always 0!
                    if (verticalSplit) (a, b) = VSplit(room);
                    else (a, b) = HSplit(room);
                }

                RoomData roomA = new(a, roomHeight);
                RoomData roomB = new(b, roomHeight);

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

                if (placeDoors) {
                    yield return new WaitUntil(() => splitRoomsA.Count != 0 && splitRoomsB.Count != 0);
                    yield return StartCoroutine(AddDoors(splitRoomsA, splitRoomsB));
                }

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

                IEnumerator AddDoors(List<RoomData> splitRoomsA, List<RoomData> splitRoomsB) {
                    foreach (RoomData roomA in splitRoomsA) {
                        foreach (RoomData roomB in splitRoomsB) {
                            var connection1 = (roomA, roomB);
                            var connection2 = (roomB, roomA);

                            if (processedConnections.Contains(connection1) || processedConnections.Contains(connection2)) {
                                Debug.Log("room was processed before");
                                yield return null;
                                continue;
                            }

                            RectInt? intersection = Intersect(roomA.Bounds, roomB.Bounds);
                            if (!intersection.HasValue) {
                                processedConnections.Add(connection1);
                                yield return null;
                                continue;
                            }

                            RectInt rect = intersection.Value;
                            int doorSpace = doorSize + 4;

                            // Horizontal door
                            if (rect.height == 1 && rect.width >= doorSpace) {
                                int centerX = rect.x + (rect.width / 2);
                                DoorData door = new(new(centerX, rect.y, doorSize, 1), doorHeight, roomA, roomB);
                                dungeonData.AddDoor(door);
                            }
                            // Vertical door
                            else if (rect.width == 1 && rect.height >= doorSpace) {
                                int centerY = rect.y + (rect.height / 2);
                                DoorData door = new(new(rect.x, centerY, 1, doorSize), doorHeight, roomA, roomB);
                                dungeonData.AddDoor(door);
                            }

                            processedConnections.Add(connection1);
                        }
                    }

                    RectInt? Intersect(RectInt a, RectInt b) {
                        int x = Mathf.Max(a.xMin, b.xMin);
                        int y = Mathf.Max(a.yMin, b.yMin);
                        int width = Mathf.Min(a.xMax, b.xMax) - x;
                        int height = Mathf.Min(a.yMax, b.yMax) - y;

                        return (width > 0 && height > 0) ? new(x, y, width, height) : null;
                    }
                }
            }
        }


        public void ResetDungeonButton() => ResetDungeon();
        private RoomData ResetDungeon() {
            StopAllCoroutines();
            dungeonData.Clear();

            generating = false;
            startTime = DateTime.Now;

            StartCoroutine(DrawDungeon());
            random = new System.Random(seed);
            RoomData room = new(initialRoomSize, roomHeight);
            dungeonData.AddRoom(room);

            return room;
        }

        IEnumerator GenerateGraph() {
            yield return null;
        }

        private static void DebugRectInt(RectInt rectInt, Color color, float duration = 0f, bool depthTest = false, float height = 0.01f) =>
            DebugExtension.DebugBounds(new Bounds(new Vector3(rectInt.center.x, 0, rectInt.center.y), new Vector3(rectInt.width, height, rectInt.height)), color, duration, depthTest);
    }
}


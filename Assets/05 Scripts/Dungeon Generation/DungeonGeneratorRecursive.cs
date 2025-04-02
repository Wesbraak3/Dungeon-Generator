using NUnit.Framework.Constraints;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst.Intrinsics;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.tvOS;

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

        [Space(10)]
        [Header("Visualisation")]
        [SerializeField] private float wireframeFixedUpdate = 1;
        [SerializeField] private bool drawRooms = true;
        [SerializeField] private bool drawDoors = true;
        [SerializeField] private bool drawGraph = true;

        [Space(10)]
        [Header("Debug stats")]
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
            StartCoroutine(DrawInnerWalls());
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
                            DebugPoint(roomPosition, roomNodeColor, duration: wireframeFixedUpdate);
                            
                            foreach (DoorData connectedDoor in room.ConnectedDoors) {
                                if (!discovered.Contains(connectedDoor)) {
                                    discovered.Add(connectedDoor);

                                    Vector3 doorPosition = new(connectedDoor.Bounds.center.x, 0, connectedDoor.Bounds.center.y);
                                    DebugPoint(doorPosition, doorNodeColor, duration: wireframeFixedUpdate);
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

            IEnumerator DrawInnerWalls() {
                while (true) {
                    RectInt innerWall = dungeonSize;
                    innerWall.x = dungeonSize.x + 1;
                    innerWall.y = dungeonSize.y + 1;
                    innerWall.width = dungeonSize.width - 2;
                    innerWall.height = dungeonSize.height - 2;

                    DebugRectInt(innerWall, Color.blue, height: height, duration: wireframeFixedUpdate);
                    yield return new WaitForSeconds(wireframeFixedUpdate);
                }
            }
        }

        public void ResetDungeonButton() => ResetDungeon();
        private RoomData ResetDungeon() {
            StopAllCoroutines();
            dungeonData.Clear();

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

            ActiveSplits = 1;
            yield return StartCoroutine(RecursiveSplit(rootRoom, (callback) => {
                ActiveSplits = 0;
            }));

            Debug.Log("remove smaller rooms");
            yield return StartCoroutine(RemoveSmallRooms(dungeonData.GetDungeonRooms(), percentage:percentToRemove));
            StopTime();

            // O(logN) with early stops
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

            //O(N log N) 
            IEnumerator RemoveSmallRooms(List<RoomData> roomList, int percentage = 0) {
                int totalRooms = roomList.Count;
                Debug.Log("1: " + totalRooms);

                int removeRooms = totalRooms * percentage / 100;
                Debug.Log("2: " + removeRooms);

                LinkedList<RoomData> linkedSortedRooms = new(roomList.OrderBy(room => room.Surface).ToList());


                DateTime startTime = DateTime.Now; // Start time of the operation
                TimeSpan timeout = TimeSpan.FromSeconds(10); // Set the timeout limit (e.g., 5 seconds)
                
                int removed = 0;
                while (removed < removeRooms) {
                    if (DateTime.Now - startTime > timeout) {
                        Console.WriteLine("Timeout reached, exiting loop.");
                        break; // Exit the loop if the timeout is exceeded
                    }

                    for (int i = 0; i < linkedSortedRooms.Count; i++) {
                        RoomData room = linkedSortedRooms.ElementAt(i);

                        if (dungeonData.CheckConnection(dungeonData.GetIndexOfRoom(room))) {
                            dungeonData.RemoveRoom(room);
                            linkedSortedRooms.Remove(room);
                            removed++;
                            continue;
                        }

                        //if (room.ConnectedDoors.Count == 1) {
                        //    dungeonData.RemoveRoom(room);
                        //    linkedSortedRooms.Remove(room);
                        //    removeRooms--;
                        //    break;
                        //}
                    }
                }

                yield break;
            }
        }

        [ContextMenu("Run BFS")]
        private void BFS() => dungeonData.RemoveCyclesBFS();

        [ContextMenu("Run DFS")]
        private void DFS() => dungeonData.RemoveCyclesDFS();
        private static void DebugRectInt(RectInt rectInt, Color color, float duration = 0f, bool depthTest = false, float height = 0f) =>
            DebugExtension.DebugBounds(new Bounds(new Vector3(rectInt.center.x, 0, rectInt.center.y), new Vector3(rectInt.width, height, rectInt.height)), color, duration, depthTest);
        private static void DebugArrow(Vector3 position, Vector3 direction, Color color, float duration = 0f, bool depthTest = false, float height = 0.01f) =>
            DebugExtension.DebugArrow(position, direction, color, duration: duration, depthTest: depthTest);
        private static void DebugPoint(Vector3 position, Color color, float scale = 1, float duration = 0f, bool depthTest = false, float height = 0.01f) =>
            DebugExtension.DebugPoint(position, color, scale:scale, duration:duration, depthTest:depthTest);
    }
}
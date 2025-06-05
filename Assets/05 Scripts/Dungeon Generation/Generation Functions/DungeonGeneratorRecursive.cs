using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonGeneration {    
    public class DungeonGeneratorRecursive : MonoBehaviour {
        public static DungeonGeneratorRecursive instance;

        private DungeonData dungeonData = DungeonManager.instance.dungeonData;
        private System.Random random;

        [Header("Room Settings")]
        [SerializeField] private int minRoomSize = DungeonManager.instance.minRoomSize;
        [SerializeField] private int doorSize = DungeonManager.instance.doorWidth;
        [SerializeField] private int percentToRemove = DungeonManager.instance.percentToRemove;
        [SerializeField] private Algoritmes algorithme = DungeonManager.instance.routeLimiter;
        [SerializeField] private int seed = DungeonManager.instance.setSeed;

        [Space(10)]
        [Header("Generation settings")]
        [SerializeField] private int maxTreads = DungeonManager.instance.recursiveMaxTreads;
        [SerializeField] private int activeTreads = 0;

        private void Awake() {
            if (DungeonManager.instance == null) Destroy(gameObject);
            if (instance == null) instance = this;
            else Destroy(gameObject);

            if (seed == 0)
                seed = UnityEngine.Random.Range(100000, 1000000);

            // Initializes a deterministic random generator
            random = new System.Random(seed);
            Debug.Log("Random initialized with seed: " + seed);
        }

        public IEnumerator Generate() {
            Debug.Log("generating dungeon...");

            RoomData rootRoom = dungeonData.GetDungeonRooms().First();
            
            // generate dungeon information
            activeTreads++;
            yield return StartCoroutine(RecursiveSplit(rootRoom, (callback) => {
                activeTreads--;
            }));
            Debug.Log("LOG: Splitting rooms done.");

            yield return StartCoroutine(RemoveSmallRooms(dungeonData.GetDungeonRooms(), percentage: percentToRemove));
            Debug.Log("LOG: Done removing rooms.");

            yield break;

            // O(N) equal splits
            // O(n*m) with door placement
            IEnumerator RecursiveSplit(RoomData roomData, Action<List<RoomData>> callback) {
                Debug.Log("LOG: Splitting room...");
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

                RoomData roomA = new(a);
                RoomData roomB = new(b);

                dungeonData.RemoveRoom(roomData);
                dungeonData.AddRoom(roomA);
                dungeonData.AddRoom(roomB);

                List<RoomData> splitRoomsA = new();
                List<RoomData> splitRoomsB = new();

                if (activeTreads < maxTreads) {
                    activeTreads += 1;
                    StartCoroutine(RecursiveSplit(roomA, (splitCallback) => {
                        splitRoomsA = splitCallback;
                        activeTreads -= 1;
                    }));
                }
                else {
                    yield return StartCoroutine(RecursiveSplit(roomA, (splitCallback) => {
                        splitRoomsA = splitCallback;
                    }));
                }
                StartCoroutine(RecursiveSplit(roomB, (splitCallback) => {
                    splitRoomsB = splitCallback;
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

                                DoorData door = new(new(randomX, rect.y, doorSize, 1), roomA, roomB);
                                dungeonData.AddDoor(door);
                            }
                            // Vertical door
                            else if (rect.width == 1) {
                                if (rect.height < doorSpace) continue;

                                // between xmin + wallThickness * 2 and xmax - (wallThickness * 2 + doorSize)  
                                int randomY = random.Next(rect.yMin + 2, rect.yMax - (doorSize + 2));

                                DoorData door = new(new(rect.x, randomY, 1, doorSize), roomA, roomB);
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
                Debug.Log("LOG: Removing rooms...");
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
                            removeRooms--; ;
                        }
                    }
                }

                yield break;
            }
        }
    }
}
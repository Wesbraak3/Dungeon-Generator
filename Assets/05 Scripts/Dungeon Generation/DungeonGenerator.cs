using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGeneration {
    public class DungeonGenerator : MonoBehaviour {
        [SerializeField] DungeonData dungeonData = new();

        #region Room Settings
        [Header("Room Settings")]
        [SerializeField] private RectInt initialRoomSize = new(0, 0, 100, 50);
        [SerializeField] private int minRoomSize = 10;
        [SerializeField] private int roomHeight = 5;

        [SerializeField] private int doorSize = 2;
        [SerializeField] private int doorHeight = 5;

        [SerializeField] private float splitRoomDelay = 5f;
        [SerializeField, Range(0f, 1f)] private float verticalSplitBias = 0.5f;
        #endregion

        [Header("Debugging")]
        [SerializeField] private int activeSplits = 0;
        private bool Started = false;

        void Start() {
            Started = true;
            CreateInitialRoom();
        }

        void Update() {
            RectInt innerWall = initialRoomSize;
            innerWall.x = initialRoomSize.x + 1;
            innerWall.y = initialRoomSize.y + 1;
            innerWall.width = initialRoomSize.width - 2;
            innerWall.height = initialRoomSize.height - 2;

            //DebugRectInt(innerWall, Color.blue, height: roomHeight);
            foreach (RoomData room in dungeonData.GetDungeonRooms()) {
                DebugRectInt(room.Bounds, Color.blue, height: room.Height);
            }
            foreach (DoorData door in dungeonData.GetDungeonDoors()) {
                DebugRectInt(door.Bounds, Color.cyan, height: door.Height);
            }
        }
        
        public void CreateInitialRoom() {
            if (!Started) return;

            StopAllCoroutines();
            dungeonData.Clear();

            RectInt roomSize = initialRoomSize;
            RoomData room = new (roomSize, roomHeight);
            dungeonData.AddRoom(room);
        }

        IEnumerator GenerateDungeon() {
            if (dungeonData.GetDungeonRooms().Count <= 0) {
                Debug.Log("You need a room to start splitting idiot");
                yield break;
            }
            yield return StartCoroutine(SplitRooms());
            yield return StartCoroutine(AddDoors());
            yield return StartCoroutine(GenerateGraph());
        }

        IEnumerator SplitRooms() {
            activeSplits = 1;
            StartCoroutine(RecursiveSplit(dungeonData.GetDungeonRooms()[0]));

            yield return new WaitWhile(() => activeSplits > 0);
            Debug.Log("Done splitting continuing with something else again");
            Debug.Log("Recursion didnt kill me");

            IEnumerator RecursiveSplit(RoomData roomData) {
                RectInt room = roomData.Bounds;

                // Calculate posible splits
                bool splitH = room.height / 2 > minRoomSize;
                bool splitV = room.width / 2 > minRoomSize;

                if (!splitH && !splitV) {
                    activeSplits--;
                    yield break;
                }

                RectInt a, b;
                // split V when
                if (!splitH || (splitV && room.width >= room.height * 2)) VSplit(room, out a, out b);
                else if (!splitV || (splitH && room.height >= room.width * 2)) HSplit(room, out a, out b);
                else {
                    bool verticalSplit = UnityEngine.Random.value < verticalSplitBias;
                    if (verticalSplit) VSplit(room, out a, out b);
                    else HSplit(room, out a, out b);
                }

                RoomData roomA = new(a, roomHeight);
                RoomData roomB = new(b, roomHeight);

                dungeonData.RemoveRoom(roomData);
                dungeonData.AddRoom(roomA);
                dungeonData.AddRoom(roomB);

                // wait till next split
                yield return new WaitForSeconds(splitRoomDelay);

                activeSplits+=2;
                StartCoroutine(RecursiveSplit(roomA));
                StartCoroutine(RecursiveSplit(roomB));
                activeSplits--;

                void VSplit(RectInt room, out RectInt a, out RectInt b) {
                    int randomInt = UnityEngine.Random.Range(minRoomSize, room.width - minRoomSize + 1);

                    int splitA = randomInt;
                    int splitB = room.width - randomInt;

                    a = new(room.x, room.y, splitA, room.height);
                    b = new(room.x + splitA - 1, room.y, splitB + 1, room.height);
                }
                void HSplit(RectInt room, out RectInt a, out RectInt b) {                    
                    int randomInt = UnityEngine.Random.Range(minRoomSize, room.height - minRoomSize + 1);

                    int splitA = randomInt;
                    int splitB = room.height - randomInt;

                    a = new(room.x, room.y, room.width, splitA);
                    b = new(room.x, room.y + splitA - 1, room.width, splitB + 1);
                }                
            }
        }
        
        // can make doors better by randomising location on the wall
        // and adding better detection of chared walls
        IEnumerator AddDoors() {
            List<RoomData> rooms = dungeonData.GetDungeonRooms();

            foreach (RoomData r in rooms) {
                foreach (RoomData r2 in rooms) {
                    if (Intersects(r.Bounds, r2.Bounds)) {
                        RectInt interserction = Intersect(r.Bounds, r2.Bounds);
                        int doorSpace = doorSize + 4;
                        // horizontal door
                        if (interserction.height == 1 && interserction.width >= doorSpace) {
                            DoorData door = new(new(Mathf.FloorToInt(interserction.center.x), interserction.y, doorSize, 1), doorHeight, r, r2);
                            dungeonData.AddDoor(door);
                        }
                        // vertical door
                        else if(interserction.width == 1 && interserction.height >= doorSpace) {
                            DoorData door = new(new(interserction.x, Mathf.FloorToInt(interserction.center.y), 1, doorSize), doorHeight, r, r2);
                            dungeonData.AddDoor(door);
                        }
                    }
                }
            }


            yield return null;

            static bool Intersects(RectInt a, RectInt b) {
                return a.xMin < b.xMax &&
                   a.xMax > b.xMin &&
                   a.yMin < b.yMax &&
                   a.yMax > b.yMin;
            }
            RectInt Intersect(RectInt a, RectInt b) {
                int x = Mathf.Max(a.xMin, b.xMin);
                int y = Mathf.Max(a.yMin, b.yMin);
                int width = Mathf.Min(a.xMax, b.xMax) - x;
                int height = Mathf.Min(a.yMax, b.yMax) - y;

                if (width <= 0 || height <= 0) {
                    return new RectInt();
                }
                else {
                    return new RectInt(x, y, width, height);
                }
            }
        }

        IEnumerator GenerateGraph() {
            yield return null;
        }

        private void OnDrawGizmos() {
            if (!Started) DebugExtension.DebugBounds(new Bounds(new Vector3(initialRoomSize.center.x, 0, initialRoomSize.center.y), new Vector3(initialRoomSize.width, roomHeight, initialRoomSize.height)), Color.yellow);
        }

        public void GenerateDungeonButton() => StartCoroutine(GenerateDungeon());
        private static void DebugRectInt(RectInt rectInt, Color color, float duration = 0f, bool depthTest = false, float height = 0.01f) =>
            DebugExtension.DebugBounds(new Bounds(new Vector3(rectInt.center.x, 0, rectInt.center.y), new Vector3(rectInt.width, height, rectInt.height)), color, duration, depthTest);
    }
}
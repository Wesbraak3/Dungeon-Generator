using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;

namespace DungeonGeneration {
    public class DungeonGeneratorAsync : MonoBehaviour {
        [SerializeField] DungeonData dungeonData = new();

        #region Room Settings
        [Header("Room Settings")]
        [SerializeField] private RectInt initialRoomSize = new(0, 0, 100, 50);
        [SerializeField] private int minRoomSize = 10;
        [SerializeField] private int roomHeight = 5;

        [SerializeField] private int doorSize = 2;
        [SerializeField] private int doorHeight = 5;

        //[SerializeField] private float splitRoomDelay = 5f;
        #endregion

        public int num = 0;

        private System.Random random = new System.Random(12345);

        private void Start() {
        }

        void Update() {
            DrawDebug();
        }

        private void DrawDebug() {
            RectInt innerWall = initialRoomSize;
            innerWall.x = initialRoomSize.x + 1;
            innerWall.y = initialRoomSize.y + 1;
            innerWall.width = initialRoomSize.width - 2;
            innerWall.height = initialRoomSize.height - 2;

            DebugRectInt(innerWall, Color.blue, height: roomHeight);
            foreach (RoomData room in dungeonData.GetDungeonRooms()) {
                RectInt roomBounds = room.Bounds;
                // Calculate posible splits
                bool splitH = roomBounds.height / 2 <= minRoomSize;
                bool splitV = roomBounds.width / 2 <= minRoomSize;

                if (splitH && splitV) {
                    DebugRectInt(room.Bounds, Color.green, height: room.Height);
                }
                else {
                    DebugRectInt(room.Bounds, Color.blue, height: room.Height);
                }
            }
            foreach (DoorData door in dungeonData.GetDungeonDoors()) {
                if (door.IsLocked) {
                    DebugRectInt(door.Bounds, Color.red, height: door.Height);
                }
                else {
                    DebugRectInt(door.Bounds, Color.green, height: door.Height);
                }
            }
        }

        public async void GenerateDungeonButton() {
            await GenerateDungeon();
        }

        private async Task GenerateDungeon() {
            Debug.Log("generating dungeon");
            RoomData rootRoom = ResetDungeon();
            await SplitRooms(rootRoom);
            //yield return StartCoroutine(AddDoors());
            //yield return StartCoroutine(GenerateGraph());
            return;
        }

        public void ResetDungeonButton() {
            ResetDungeon();
        }
        private RoomData ResetDungeon() {
            StopAllCoroutines();
            dungeonData.Clear();
            random = new System.Random(12345);
            RoomData room = new(initialRoomSize, roomHeight);
            dungeonData.AddRoom(room);

            return room;
        }

        Task SplitRooms(RoomData rootRoom) {
            RecursiveSplit(rootRoom);

            Debug.Log("Done splitting continuing with something else again");
            Debug.Log("Recursion didnt kill me");
            return Task.CompletedTask;
            void RecursiveSplit(RoomData roomData) {
                RectInt room = roomData.Bounds;

                // Calculate posible splits
                bool splitH = room.height / 2 > minRoomSize;
                bool splitV = room.width / 2 > minRoomSize;

                if (!splitH && !splitV) {
                    return;
                }

                RectInt a, b;
                if (!splitH || (splitV && room.width >= room.height * 2)) (a, b) = VSplit(room);
                else if (!splitV || (splitH && room.height >= room.width * 2)) (a, b) = HSplit(room);
                else {
                    bool verticalSplit = random.Next(0, 1) < .5f;
                    if (verticalSplit) (a, b) = VSplit(room);
                    else (a, b) = HSplit(room);
                }

                RoomData roomA = new(a, roomHeight);
                RoomData roomB = new(b, roomHeight);

                dungeonData.RemoveRoom(roomData);
                dungeonData.AddRoom(roomA);
                dungeonData.AddRoom(roomB);

                RecursiveSplit(roomA);
                RecursiveSplit(roomB);

                (RectInt, RectInt) VSplit(RectInt room) {
                    int randomInt = random.Next(minRoomSize, room.width - minRoomSize + 1);

                    int splitA = randomInt;
                    int splitB = room.width - randomInt;

                    a = new(room.x, room.y, splitA, room.height);
                    b = new(room.x + splitA - 1, room.y, splitB + 1, room.height);
                    return (a, b);
                }
                (RectInt, RectInt) HSplit(RectInt room) {
                    int randomInt = random.Next(minRoomSize, room.height - minRoomSize + 1);

                    int splitA = randomInt;
                    int splitB = room.height - randomInt;

                    a = new(room.x, room.y, room.width, splitA);
                    b = new(room.x, room.y + splitA - 1, room.width, splitB + 1);
                    return (a, b);
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
                        else if (interserction.width == 1 && interserction.height >= doorSpace) {
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
            //if (false) DebugExtension.DebugBounds(new Bounds(new Vector3(initialRoomSize.center.x, 0, initialRoomSize.center.y), new Vector3(initialRoomSize.width, roomHeight, initialRoomSize.height)), Color.yellow);
        }

        private static void DebugRectInt(RectInt rectInt, Color color, float duration = 0f, bool depthTest = false, float height = 0.01f) =>
            DebugExtension.DebugBounds(new Bounds(new Vector3(rectInt.center.x, 0, rectInt.center.y), new Vector3(rectInt.width, height, rectInt.height)), color, duration, depthTest);
    }
}


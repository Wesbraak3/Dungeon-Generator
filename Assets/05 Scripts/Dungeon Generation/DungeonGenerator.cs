using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGeneration {
    [Serializable]
    public class RoomData {
        public RectInt roomBounds;
        public Action debugActions;

        public RoomData(RectInt bounds, Action debugAction = null) {
            this.roomBounds = bounds;
            this.debugActions = debugAction;
        }

        public void ExecuteDebugActions() {
            debugActions?.Invoke();
        }
    }
    public class DoorData {
        public RectInt doorBounds;
        public Action debugActions;

        public DoorData(RectInt bounds, Action debugAction = null) {
            this.doorBounds = bounds;
            this.debugActions = debugAction;
        }

        public void ExecuteDebugActions() {
            debugActions?.Invoke();
        }
    }

    public class DungeonGenerator : MonoBehaviour {
        #region Room Settings
        [Header("Room Settings")]
        [SerializeField]
        private RectInt initialRoom = new(0, 0, 100, 50);
        [SerializeField]
        private int height = 5;
        [SerializeField]
        private int minRoomSize = 10;
        [SerializeField]
        private Color dungeonColor = Color.blue;

        [Header("generated Rooms")]
        [SerializeField] private List<RoomData> rooms = new();
        [SerializeField] private List<DoorData> doors = new();

        [SerializeField]
        private float splitRoomDelay = 5f;
        [SerializeField, Range(0f, 1f)]
        private float verticalSplitBias = 0.5f;

        [SerializeField]
        private int activeSplits = 0;

        private bool Started = false;
        #endregion

        void Start() {
            Started = true;
            CreateInitialRoom();
        }

        void Update() {
            RectInt outerWall = initialRoom;
            outerWall.x = initialRoom.x + 1;
            outerWall.y = initialRoom.y + 1;
            outerWall.width = initialRoom.width - 2;
            outerWall.height = initialRoom.height - 2;

            DebugRectInt(outerWall, dungeonColor, height: height);
            foreach (RoomData room in rooms) {
                room.ExecuteDebugActions();
            }
            foreach (DoorData door in doors) {
                door.ExecuteDebugActions();
            }
        }
        
        public void CreateInitialRoom() {
            if (!Started) return;

            StopAllCoroutines();
            rooms.Clear();

            RectInt room = initialRoom;

            RoomData newRoom = new (
                room, 
                debugAction: () => 
                    DebugRectInt(room, dungeonColor, height: height)
            );
           
            rooms.Add(newRoom);
        }

        IEnumerator GenerateDungeon() {
            if (rooms.Count <= 0) {
                Debug.Log("You need a room to start splitting idiot");
                yield break;
            }
            yield return StartCoroutine(SplitRooms());
            yield return StartCoroutine(AddDoors());
            yield return StartCoroutine(GenerateGraph());
        }

        IEnumerator SplitRooms() {
            activeSplits = 1;
            StartCoroutine(RecursiveSplit(rooms[0]));

            yield return new WaitWhile(() => activeSplits > 0);
            Debug.Log("Done splitting continuing with something else again");
            Debug.Log("Recursion didnt kill me");

            IEnumerator RecursiveSplit(RoomData roomData) {
                RectInt room = roomData.roomBounds;

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

                RoomData roomA = new(
                    a, debugAction: () => DebugRectInt(a, dungeonColor, height: height));
                RoomData roomB = new(
                    b, debugAction: () => DebugRectInt(b, dungeonColor, height: height));

                rooms.Remove(roomData);
                rooms.Add(roomA);
                rooms.Add(roomB);

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
        IEnumerator AddDoors() {
            yield return null;
        }
        IEnumerator GenerateGraph() {
            yield return null;
        }

        private void OnDrawGizmos() {
            if (!Started) DebugExtension.DebugBounds(new Bounds(new Vector3(initialRoom.center.x, 0, initialRoom.center.y), new Vector3(initialRoom.width, height, initialRoom.height)), Color.yellow);
        }


        public void GenerateDungeonButton() => StartCoroutine(GenerateDungeon());
        private static void DebugRectInt(RectInt rectInt, Color color, float duration = 0f, bool depthTest = false, float height = 0.01f) =>
            DebugExtension.DebugBounds(new Bounds(new Vector3(rectInt.center.x, 0, rectInt.center.y), new Vector3(rectInt.width, height, rectInt.height)), color, duration, depthTest);
    }
}
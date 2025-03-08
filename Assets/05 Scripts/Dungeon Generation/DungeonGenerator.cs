using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

namespace DungeonGeneration {
    [Serializable]
    public class RoomData {
        public RectInt roomBounds;
        public bool roomIsDone;
        public Action debugActions;

        public RoomData(RectInt bounds, bool isDone=false, Action debugAction = null) {
            this.roomBounds = bounds;
            this.roomIsDone = isDone;
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
        [SerializeField]
        private List<RoomData> rooms = new();
        [SerializeField]
        private float splitRoomDelay = 5f;
        [SerializeField]
        private int splitTillPercentageSmall = 100;


        private bool Started = false;
        #endregion

        void Start() {
            Started = true;
            CreateInitialRoom();
        }

        void Update() {
            foreach (RoomData room in rooms) {
                room.ExecuteDebugActions();
            }
        }

        /// <summary>
        /// Testing setup for manuel creating splitting etc..
        /// Later use logic to automate the proces
        ///// </summary>
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

        public void GenerateDungeonButton() {
            if (rooms.Count <= 0) {
                Debug.Log("You need a room to start splitting idiot");
                return;
            }

            StartCoroutine(GenerateDungeon());
        }

        IEnumerator GenerateDungeon() {
            yield return StartCoroutine(SplitRooms());
            yield return StartCoroutine(AddDoors());
            yield return StartCoroutine(GenerateGraph());
        }

        /// <summary>
        /// Split rooms while
        /// 1. rooms not small enouth
        /// 2. a precentage of rooms not done
        /// room not done when % of rooms still to big 
        /// </summary>
        /// <returns></returns>
        IEnumerator SplitRooms() {
            int activeSplits = 0;

            StartCoroutine(RecursiveSplit(rooms[0].roomBounds));

            yield return new WaitWhile(() => activeSplits > 0);
            Debug.Log("Done splitting continuing with something else again");
            Debug.Log("Recursion didnt kill me");

            IEnumerator RecursiveSplit(RectInt room) {
                activeSplits++;
                
                // stop if global stop or room to small
                if (!ContiueSplitting()) {
                    activeSplits--;
                    yield break;
                }

                //split and call again
                //split logic PUT IT HERE BRUH

                //    Debug.Log("render new room");

                //for (int i = 0; i <= rooms.Count - 1; i++) {
                //    RectInt room = rooms[i];

                //    // random between 20 and room.width/height
                //    int splitA = Mathf.CeilToInt(room.width / 2);
                //    int splitB = room.width - splitA;

                //    if (splitA < minRoomSize || splitB < minRoomSize) {
                //        // mark room as done.
                //        continue;
                //    }

                //    RectInt roomA = new(room.x, room.y, splitA, room.height);
                //    RectInt roomB = new(room.x + splitA - 1, room.y, splitB + 1, room.height);

                //    rooms.Remove(room);
                //    rooms.Add(roomA);
                //    rooms.Add(roomB);
                //}

                //DebugRenderer(rooms);
                //roomCount = rooms.Count;

                RectInt roomA = room;
                RectInt roomB = room;

                // wait till next split
                yield return new WaitForSeconds(splitRoomDelay);
                StartCoroutine(RecursiveSplit(roomA));
                StartCoroutine(RecursiveSplit(roomB));
            }

            bool ContiueSplitting() {
                //int smallRooms = rooms.FindAll(r => r.width <= minRoomSize || r.height <= minRoomSize).Count;
                //return (float)smallRooms / rooms.Count >= splitTillPercentageSmall;
                return false;
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

        private static void DebugRectInt(RectInt rectInt, Color color, float duration = 0f, bool depthTest = false, float height = 0.01f) =>
            DebugExtension.DebugBounds(new Bounds(new Vector3(rectInt.center.x, 0, rectInt.center.y), new Vector3(rectInt.width, height, rectInt.height)), color, duration, depthTest);
    }
}
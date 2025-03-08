using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DungeonGeneration {
    public class DungeonGenerator : MonoBehaviour {
        #region Room Settings

        [Header("Room Settings")]
        [SerializeField]
        private RectInt initialSize = new(0, 0, 100, 50);
        [SerializeField]
        private int height = 5;
        [SerializeField]
        private int minRoomSize = 10;
        [SerializeField]
        private Color dungeonColor = Color.blue;

        [Header("Room Debug")]
        [SerializeField]
        private List<RectInt> rooms = new();

        [Header("Room manager")]
        [SerializeField]
        private int roomCount = 0;
        private bool Started = false;

        #endregion


        void Start() {
            Started = true;
            ResetDungeon();
        }
        void Update() {
        }

        public void ResetDungeon() {
            DebugDrawingBatcher.ClearCalls();
            rooms.Clear();
            roomCount = 0;
        }

        /// <summary>
        /// Testing setup for manuel creating splitting etc..
        /// Later use logic to automate the proces
        ///// </summary>
        public void CreateInitialRoom() {
            if (!Started) return;
            ResetDungeon();

            RectInt initialRoom = new(
                initialSize.x,
                initialSize.y,
                initialSize.width,
                initialSize.height
            );

            DebugDrawingBatcher.BatchCall(() => {
                AlgorithmsUtils.DebugRectInt(initialRoom, dungeonColor, height:height);
            });

            rooms.Add(initialRoom);
            roomCount = rooms.Count;
        }

        public void SplitRooms() {
            if (roomCount == 0) return;

            for (int i = 0; i <= rooms.Count - 1; i++) {
                RectInt room = rooms[i];

                // random between 20 and room.width/height
                int splitA = Mathf.CeilToInt(room.width / 2);
                int splitB = room.width - splitA;

                if (splitA < minRoomSize || splitB < minRoomSize) {
                    // mark room as done.
                    continue;
                }

                RectInt roomA = new(room.x, room.y, splitA, room.height);
                RectInt roomB = new(room.x + splitA - 1, room.y, splitB + 1, room.height);

                rooms.Remove(room);
                rooms.Add(roomA);
                rooms.Add(roomB);
            }

            DebugRenderer(rooms);
            roomCount = rooms.Count;
        }
        void DebugRenderer(List<RectInt> rooms) {
            DebugDrawingBatcher.ClearCalls();

            foreach (RectInt room in rooms) {
                DebugDrawingBatcher.BatchCall(() => {
                    AlgorithmsUtils.DebugRectInt(room, dungeonColor, height: height);
                });
            }
        }

        private void OnDrawGizmos() {
            if (rooms.Count == 0) DebugExtension.DebugBounds(new Bounds(new Vector3(initialSize.center.x, 0, initialSize.center.y), new Vector3(initialSize.width, height, initialSize.height)), Color.yellow);
        }
    }
}
using System;
using System.Collections.Generic;
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

            RectInt dungeonSize = new(
                initialSize.x,
                initialSize.y,
                initialSize.width,
                initialSize.height
            );

            DebugDrawingBatcher.BatchCall(() => {
                AlgorithmsUtils.DebugRectInt(dungeonSize, dungeonColor, height:height);
            });

            rooms.Add(initialSize);
            roomCount = rooms.Count;
        }

        public void SplitRooms() {
            if (roomCount == 0) return;

            roomCount = rooms.Count;
            throw new NotImplementedException();
        }

        private void OnDrawGizmos() {
            if (rooms.Count == 0) DebugExtension.DebugBounds(new Bounds(new Vector3(initialSize.center.x, 0, initialSize.center.y), new Vector3(initialSize.width, height, initialSize.height)), Color.yellow);
        }
    }
}
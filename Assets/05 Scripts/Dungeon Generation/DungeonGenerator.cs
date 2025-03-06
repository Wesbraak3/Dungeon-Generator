using System;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace DungeonGeneration {
    public class DungeonGenerator : MonoBehaviour {
        #region Room Settings

        [Header("Room Settings")]
        [DisableIf("Enabled"), SerializeField] 
        private RectInt initialSize = new(0, 0, 100, 50);
        [DisableIf("Enabled"), SerializeField] 
        private int height = 5;
        [DisableIf("Enabled"), SerializeField] 
        private Color dungeonColor = Color.blue;

        [HorizontalLine]
        [Header("Room Debug")]
        [ReadOnly, SerializeField] 
        private List<RectInt> rooms = new();

        [HorizontalLine]
        [Header("Room manager")]
        [ReadOnly, SerializeField] 
        private int roomCount = 0;

        #endregion

        // For disabling the inspector room settings after start
        public bool Enabled() => roomCount > 0;

        void Start() {
        }
        void Update() {
        }

        /// <summary>
        /// Testing setup for manuel creating splitting etc..
        /// Later use logic to automate the proces
        /// </summary>
        [Button(enabledMode: EButtonEnableMode.Playmode)]
        private void CreateInitialRoom() {
            // clear stored values
            DebugDrawingBatcher.ClearCalls();
            rooms.Clear();

            // make so battcher accept list as input instead of objects?
            DebugDrawingBatcher.BatchCall(() => {
                AlgorithmsUtils.DebugRectInt(initialSize, dungeonColor, height:height);
            });

            rooms.Add(initialSize);
            roomCount = rooms.Count;
        }

        [Button(enabledMode: EButtonEnableMode.Playmode)]
        private void SplitRooms() {
            roomCount = rooms.Count;
            throw new NotImplementedException();
        }

        private void OnDrawGizmos() {
            if (rooms.Count == 0) DebugExtension.DebugBounds(new Bounds(new Vector3(initialSize.center.x, 0, initialSize.center.y), new Vector3(initialSize.width, height, initialSize.height)), dungeonColor);
        }
    }
}
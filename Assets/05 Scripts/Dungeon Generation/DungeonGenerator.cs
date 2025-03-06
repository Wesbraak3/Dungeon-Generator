using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGeneration {
    public class DungeonGenerator : MonoBehaviour {
        [SerializeField]
        private List<RectInt> rooms = new();

        [SerializeField] private RectInt initialSize = new(0, 0, 100, 50);
        [SerializeField] private int height = 10;
        [SerializeField] private Color dungeonColor = Color.red;

        void Start() {
            DebugDrawingBatcher.BatchCall(() => {
                AlgorithmsUtils.DebugRectInt(initialSize, dungeonColor, height:height);
            });

            rooms.Add(initialSize);
        }

        private void SplitRooms() {
            throw new NotImplementedException();
        }

        void Update() {
        }
    }
}
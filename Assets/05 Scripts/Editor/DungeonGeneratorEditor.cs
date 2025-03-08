using UnityEngine;
using UnityEditor;

namespace DungeonGeneration {
    [CustomEditor(typeof(DungeonGenerator))]
    public class CreateRoomEditor : Editor {
        public override void OnInspectorGUI() {
            if (GUILayout.Button("Reset Dungeon")) {
                DungeonGenerator button = (DungeonGenerator)target;
                button.ResetDungeon();
            }
            if (GUILayout.Button("Create First Room")) {
                DungeonGenerator button = (DungeonGenerator)target;
                button.CreateInitialRoom();
            }
            if (GUILayout.Button("Split room")) {
                DungeonGenerator button = (DungeonGenerator)target;
                button.SplitRooms();
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            DrawDefaultInspector();
        }
    }
}
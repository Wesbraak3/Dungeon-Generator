using UnityEngine;
using UnityEditor;

namespace DungeonGeneration {
    [CustomEditor(typeof(DungeonGenerator))]
    public class CreateRoomEditor : Editor {
        public override void OnInspectorGUI() {
            if (GUILayout.Button("Create First Room")) {
                DungeonGenerator button = (DungeonGenerator)target;
                button.CreateInitialRoom();
            }
            if (GUILayout.Button("Generate Dungeon Button")) {
                DungeonGenerator button = (DungeonGenerator)target;
                button.GenerateDungeonButton();
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            DrawDefaultInspector();
        }
    }
}
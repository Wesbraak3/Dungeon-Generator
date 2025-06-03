using UnityEngine;
using UnityEditor;

namespace DungeonGeneration {
    [CustomEditor(typeof(DungeonManager))]
    public class CreateRoomEditor : Editor {

        public override void OnInspectorGUI() {
            DungeonManager button = (DungeonManager)target;

            // Reset Dungeon Button
            GUI.enabled = EditorApplication.isPlaying;
            if (GUILayout.Button("Reset Dungeon")) {
                button.ResetDungeon();
            }

            // Generate Dungeon Button
            GUI.enabled = EditorApplication.isPlaying;
            if (GUILayout.Button("Generate Dungeon")) {
                button.GenerateDungeon();
            }

            // Restore GUI state (after buttons)
            GUI.enabled = true;

            // Space and horizontal slider separator
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // Draw the default inspector for the rest of the properties
            DrawDefaultInspector();
        }
    }
}
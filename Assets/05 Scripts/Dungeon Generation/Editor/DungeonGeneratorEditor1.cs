using UnityEngine;
using UnityEditor;

namespace DungeonGeneration {
    [CustomEditor(typeof(DungeonGeneratorAsync))]
    public class CreateRoomEditor1 : Editor {

        public override void OnInspectorGUI() {
            DungeonGeneratorAsync button = (DungeonGeneratorAsync)target;

            // Reset Dungeon Button
            GUI.enabled = EditorApplication.isPlaying;
            if (GUILayout.Button("Reset Dungeon")) {
                button.ResetDungeonButton();
            }

            // Generate Dungeon Button
            GUI.enabled = EditorApplication.isPlaying;
            if (GUILayout.Button("Generate Dungeon")) {
                button.GenerateDungeonButton();
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
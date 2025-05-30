using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEditor.PlayerSettings;

namespace DungeonGeneration {
    public class DungeonManager : MonoBehaviour {
        private DungeonData dungeonData = new();
        private System.Random random;

        private enum Generator { Recursive, Async };
        [Header("Dungeon Generator Settings")]
        [SerializeField] private Generator generator = Generator.Recursive;

        [SerializeField] private int setSeed = 1234;
        [SerializeField] private Vector2Int dungeonSize = new(100, 50);
        [SerializeField] private int minRoomSize = 10;
        [SerializeField] private int doorWidth = 2;
        [SerializeField] private int height = 5;
        [SerializeField] private int percentToRemove = 0;
        private enum Algoritmes { BFS, DFS, DFSRandom, None };
        [SerializeField] private Algoritmes algorithme = Algoritmes.BFS;


        [Space(10)]
        [Header("Visualisation")]
        [SerializeField] private float wireframeFixedUpdate = 1;
        [SerializeField] private bool drawRooms = true;
        [SerializeField] private bool drawInnerWalls = true;
        [SerializeField] private bool drawDoors = true;
        [SerializeField] private bool drawGraph = true;

        [Space(10)]
        [Header("Dungeon")]
        [SerializeField] private GameObject doorPrefab;
        [SerializeField] private GameObject wallPrefab;
        [SerializeField] private GameObject floorPrefab;
        [SerializeField] private GameObject playerPrefab;
        private GridManager gridManager;
        private GameObject player;
        private GameObject dungeonObject;

        [Space(10)]
        [Header("Debug")]
        [SerializeField] private int maxTreads = 2;
        [SerializeField] private int activeTreads = 0;
        [SerializeField] private int ActiveSplits = 0;


        private DateTime startTime;
        private TimeSpan timeTaken;
        private bool generating = false;


        private void Start() {
            gridManager = GridManager.instance;
            ResetDungeon();
        }


        private void StartTime() {
            startTime = DateTime.Now;
            generating = true;
        }
        private void StopTime() {
            DateTime endTime = DateTime.Now;
            timeTaken = endTime - startTime;
            generating = false;

            Debug.Log($"Room Generation timer: {timeTaken.TotalSeconds}");
        }

        private void DrawDebug() {
            StartCoroutine(DrawRooms());
            StartCoroutine(DrawDoors());
            StartCoroutine(DrawGraph());

            IEnumerator DrawGraph() {
                while (true) {
                    if (drawGraph) {
                        Color roomNodeColor = Color.magenta;
                        Color doorNodeColor = Color.red;
                        Color edgeColor = Color.yellow;

                        HashSet<DoorData> discovered = new();
                        foreach (RoomData room in dungeonData.GetDungeonRooms()) {
                            Vector3 roomPosition = new(room.Bounds.center.x, 0, room.Bounds.center.y);
                            DebugCircle(roomPosition, roomNodeColor, duration: wireframeFixedUpdate);

                            foreach (DoorData connectedDoor in room.ConnectedDoors) {
                                if (!discovered.Contains(connectedDoor)) {
                                    discovered.Add(connectedDoor);

                                    Vector3 doorPosition = new(connectedDoor.Bounds.center.x, 0, connectedDoor.Bounds.center.y);
                                    DebugCircle(doorPosition, doorNodeColor, duration: wireframeFixedUpdate);
                                }

                                Vector3 connectedDoorPosition = new(connectedDoor.Bounds.center.x, 0, connectedDoor.Bounds.center.y);
                                Debug.DrawLine(roomPosition, connectedDoorPosition, edgeColor, duration: wireframeFixedUpdate);
                            }
                        }
                    }

                    yield return new WaitForSeconds(wireframeFixedUpdate);
                }
            }

            IEnumerator DrawRooms() {
                while (true) {
                    if (drawRooms) {
                        Color roomColour = Color.cyan;

                        foreach (RoomData room in dungeonData.GetDungeonRooms()) {
                            if (drawInnerWalls) {
                                RectInt innerBounds = new(room.Bounds.xMin + 1, room.Bounds.yMin + 1, room.Bounds.width - 2, room.Bounds.height - 2);
                                DebugRectInt(innerBounds, roomColour, height: room.Height, duration: wireframeFixedUpdate);
                            }

                            DebugRectInt(room.Bounds, roomColour, height: room.Height, duration: wireframeFixedUpdate);
                        }
                    }

                    yield return new WaitForSeconds(wireframeFixedUpdate);
                }
            }

            IEnumerator DrawDoors() {
                while (true) {
                    if (drawDoors) {
                        Color doorColour = Color.green;

                        foreach (DoorData door in dungeonData.GetDungeonDoors()) {
                            DebugRectInt(door.Bounds, doorColour, height: door.Height, duration: wireframeFixedUpdate);
                        }
                    }

                    yield return new WaitForSeconds(wireframeFixedUpdate);
                }
            }
        }

        public void ResetDungeonButton() => ResetDungeon();
        private RoomData ResetDungeon() {
            StopAllCoroutines();
            dungeonData.Clear();

            if (dungeonObject != null) {
                Destroy(dungeonObject);
                dungeonObject = null;
            }
            if (player != null) {
                Destroy(player);
                player = null;
            }

            generating = false;
            activeTreads = 0;
            DrawDebug();

            random = new System.Random(seed);
            RoomData room = new(dungeonSize, height);
            dungeonData.AddRoom(room);

            return room;
        }

        [ContextMenu("Generate Dungeon")]
        public void GenerateDungeonButton() {
            RoomData rootRoom = ResetDungeon();
            //StartCoroutine(GenerateDungeon(rootRoom));
        }

        private static void DebugRectInt(RectInt rectInt, Color color, float duration = 0f, bool depthTest = false, float height = 0f) =>
            DebugExtension.DebugBounds(new Bounds(new Vector3(rectInt.center.x, height * 0.5f, rectInt.center.y), new Vector3(rectInt.width, height, rectInt.height)), color, duration, depthTest);
        private static void DebugCircle(Vector3 position, Color color, float radius = .5f, float duration = 0f, bool depthTest = false) =>
                DebugExtension.DebugCircle(position, color, radius: radius, duration: duration, depthTest: depthTest);

    }
}
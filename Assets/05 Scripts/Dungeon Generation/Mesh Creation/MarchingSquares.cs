using NUnit;
using System.Collections;
using System.Collections.Generic;
using TMPro.EditorUtilities;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static UnityEditor.PlayerSettings;

namespace DungeonGeneration {
    public class MeshCreation : MonoBehaviour {
        public static MeshCreation instance;

        [SerializeField] private string dungeonParentName = "== Dungeon ==";
        [SerializeField] private string dungeonStructureName = "== Structure ==";

        [Header("Add case 0 - 16")]
        [SerializeField] private List<GameObject> objectPlacementCases = DungeonManager.instance.objectPlacementCases;

        private GameObject dungeonParent;
        private GameObject dungeonStructure;
        public GameObject floor = DungeonManager.instance.floor;
        public GameObject wallMarker = DungeonManager.instance.wallMarker;

        private Grid grid;
        private GridManager gridManager;
        private GridData gridData;

        private void Awake() {
            if (instance == null) instance = this;
            else Destroy(gameObject);
            Setup();
        }

        private void Setup() {
            if (objectPlacementCases.Count != 16)
                Debug.LogError($"ERROR: Not all cases provided, Provided: {objectPlacementCases.Count}");

            foreach (Transform child in transform) {
                DestroyImmediate(child.gameObject);
            }
            dungeonParent = new(dungeonParentName);
            dungeonParent.transform.SetParent(transform);
            dungeonStructure = new(dungeonStructureName);
            dungeonStructure.transform.SetParent(transform);

            // Try to add GridManager safely
            if (dungeonParent.GetComponent<GridManager>() == null) {
                dungeonParent.AddComponent<GridManager>();
            }
            else {
                Debug.LogWarning("GridManager already exists on DungeonParent.");
            }

            grid = dungeonParent.AddComponent<Grid>();
            gridManager = GridManager.instance;
            gridData = gridManager.gridData;
        }

        public IEnumerator CreateMesh() {
            Vector2Int dungeonSize = DungeonManager.instance.dungeonSize;

            CreateTileMap(
                dungeonSize,
                DungeonManager.instance.dungeonData.GetDungeonRooms(),
                DungeonManager.instance.dungeonData.GetDungeonDoors()
            );

            yield return StartCoroutine(MarchSquares(new Vector2Int(-1, -1), dungeonSize));

            Vector2 centerOfRoom = DungeonManager.instance.dungeonData.GetDungeonRooms()[0].Bounds.center;
            Vector3Int startGridCoord = grid.WorldToCell(new Vector3(centerOfRoom.x, 0, centerOfRoom.y));
            yield return StartCoroutine(FloodFill(startGridCoord));

            Destroy(this);
        }

        private void CreateTileMap(Vector2Int dungeonSize, List<RoomData> rooms, List<DoorData> doors) {
            foreach (RoomData room in rooms) {
                Vector2Int bottomLeft = new(room.Bounds.xMin, room.Bounds.yMin);
                Vector2Int topLeft = new(room.Bounds.xMin, room.Bounds.yMax - 1);
                Vector2Int bottomRight = new(room.Bounds.xMax - 1, room.Bounds.yMin);
                Vector2Int topRight = new(room.Bounds.xMax - 1, room.Bounds.yMax - 1);

                // Bottom edge
                SetWallHor(bottomLeft, bottomRight);
                // Top edge
                SetWallHor(topLeft, topRight);
                // Left edge
                SetWallVert(bottomLeft, topLeft);
                // Right edge
                SetWallVert(bottomRight, topRight);
            }

            foreach (DoorData door in doors) {
                Vector2Int bottomLeft = new(door.Bounds.xMin, door.Bounds.yMin);
                Vector2Int topLeft = new(door.Bounds.xMin, door.Bounds.yMax);
                Vector2Int bottomRight = new(door.Bounds.xMax, door.Bounds.yMin);
                Vector2Int topRight = new(door.Bounds.xMax, door.Bounds.yMax);

                for (int x = door.Bounds.xMin; x < door.Bounds.xMax; x++) {
                    for (int y = door.Bounds.yMin; y < door.Bounds.yMax; y++) {
                        Vector3Int pos = new(x, 0, y);
                        GameObject placedObj = gridData.GetPlacementData(pos).PlaceObject;
                        gridData.RemoveObjectAt(pos);
                        Destroy(placedObj);
                    }
                }
            }

            void SetWallVert(Vector2Int start, Vector2Int end) {
                for (int y = start.y; y <= end.y; y++) {
                    Vector3Int pos = new(start.x, 0, y);
                    if (gridData.CanPlaceObjectAt(pos, new(1, 1))) {
                        GameObject placedObject = Instantiate(wallMarker, pos, Quaternion.identity);
                        placedObject.transform.SetParent(dungeonStructure.transform);
                        gridData.AddObjectAt(pos, new(1, 1), placedObject, false);
                    }
                }
            }

            void SetWallHor(Vector2Int start, Vector2Int end) {
                for (int x = start.x; x <= end.x; x++) {
                    Vector3Int pos = new(x, 0, start.y);
                    if (gridData.CanPlaceObjectAt(pos, new(1, 1))) {
                        GameObject placedObject = Instantiate(wallMarker, pos, Quaternion.identity);
                        placedObject.transform.SetParent(dungeonStructure.transform);
                        gridData.AddObjectAt(pos, new(1, 1), placedObject, false);
                    }
                }
            }
        }

        private IEnumerator MarchSquares(Vector2Int start, Vector2Int end) {
            for (int i = start.y; i < end.y; i++) {
                for (int i2 = start.x; i2 < end.x; i2++) {
                    Vector2Int _01 = new(i2, i + 1);
                    Vector2Int _02 = new(i2 + 1, i + 1);
                    Vector2Int _03 = new(i2, i);
                    Vector2Int _04 = new(i2 + 1, i);

                    int index = 0;
                    if (!gridData.CanPlaceObjectAt(new(_01.x, 0, _01.y), new(1, 1))) {
                        index += 8;
                    }
                    if (!gridData.CanPlaceObjectAt(new(_02.x, 0, _02.y), new(1, 1))) {
                        index += 4;
                    }
                    if (!gridData.CanPlaceObjectAt(new(_03.x, 0, _03.y), new(1, 1))) {
                        index += 2;
                    }
                    if (!gridData.CanPlaceObjectAt(new(_04.x, 0, _04.y), new(1, 1))) {
                        index += 1;
                    }

                    Vector3Int pos = new(_03.x, 0, _03.y);
                    GameObject objectToPlace = objectPlacementCases[index];

                    if (objectToPlace == null) {
                        continue;
                    }

                    if (!gridData.CanPlaceObjectAt(pos, new(1, 1))) {
                        GameObject placedObj = gridData.GetPlacementData(pos).PlaceObject;
                        Destroy(placedObj);
                    }

                    GameObject placedObject = Instantiate(objectToPlace, pos, Quaternion.identity);
                    placedObject.transform.SetParent(dungeonStructure.transform);

                    gridData.ReplaceObjectAt(pos, new(1, 1), placedObject, false);
                }
                yield return null;
            }
        }

        private IEnumerator FloodFill(Vector3Int start) {
            Queue<Vector3Int> Q = new();
            Q.Enqueue(start);
            int i = 0;
            while (Q.Count != 0) {
                Vector3Int v = Q.Dequeue();

                foreach (Vector3Int w in gridManager.GetAdjecentNodes(v)) {
                    if (gridData.CanPlaceObjectAt(w, new(1, 1))) {
                        Q.Enqueue(w);

                        GameObject placedObject = Instantiate(floor, w, Quaternion.identity);
                        placedObject.transform.SetParent(dungeonStructure.transform);
                        gridData.AddObjectAt(w, new(1, 1), placedObject, true);
                    }
                }

                if (i % 50 == 1) {
                    yield return null;
                }

                i++;
            }
        }
    }
}
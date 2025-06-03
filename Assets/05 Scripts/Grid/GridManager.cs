using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class GridManager : MonoBehaviour {
    public static GridManager instance;

    [SerializeField] private Grid grid;
    public GridData gridData = new();

    private void Awake() {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void Start() {
        if (grid == null) grid = GetComponent<Grid>();
    }

    public List<Vector3Int> GetAdjecentNodes(Vector3 from) {
        Vector3Int pos = grid.WorldToCell(from);
        List<Vector3Int> adjacentNodes = new ();

        Vector3Int[] directions = {
            // Cardinal directions
            new (1, 0, 0),
            new (0, 0, 1),
            new (-1, 0, 0),
            new (0, 0, -1),

            // Diagonal directions
            new (1, 0, 1),
            new (1, 0, -1),
            new (-1, 0, 1),
            new (-1, 0, -1)
        };

        foreach (Vector3Int dir in directions) {
            adjacentNodes.Add(pos + dir);
        }

        return adjacentNodes;
    }

    public bool IsTileTraversable(Vector3 position) {
        Vector3Int gridPosition = grid.WorldToCell(position);
        PlacementData placementData = gridData.GetPlacementData(gridPosition);
        if (placementData == null) return false;
        return placementData.IsTraversable;
    }

    public Vector3Int GetClosestGridPosition(Vector3 position) {
        Vector3 localPos = grid.transform.InverseTransformPoint(position);
        Vector3 cellSize = grid.cellSize;

        int x = Mathf.RoundToInt(localPos.x / cellSize.x);
        int y = Mathf.RoundToInt(localPos.y / cellSize.y);
        int z = Mathf.RoundToInt(localPos.z / cellSize.z);

        return new Vector3Int(x, y, z);
    }
    public Grid GetGrid() => grid;
    public Vector3Int GetGridPosition(Vector3 position) => grid.WorldToCell(position);
}

using System.Collections.Generic;
using UnityEngine;

public class GridData {
    Dictionary<Vector3Int, PlacementData> placedObjects = new();

    public void AddObjectAt(Vector3Int gridPosition,
                            Vector2Int objectSize,
                            GameObject placedObjectIndex,
                            bool isTraversable
        ) {
        List<Vector3Int> positionToOccupy = CalculatePositions(gridPosition, objectSize);
        PlacementData data = new(positionToOccupy, placedObjectIndex, isTraversable);
        foreach (var pos in positionToOccupy) {
            if (placedObjects.ContainsKey(pos)) {
                throw new System.Exception($"Dictionary already contains this cell position {pos}");
            }
            placedObjects[pos] = data;
        }
    }

    public bool CanPlaceObjectAt(Vector3Int gridPosition, Vector2Int objectSize) {
        List<Vector3Int> positionToOccupy = CalculatePositions(gridPosition, objectSize);
        foreach (var pos in positionToOccupy) {
            if (placedObjects.ContainsKey(pos)) {
                return false;
            }
        }
        return true;
    }

    private List<Vector3Int> CalculatePositions(Vector3Int gridPosition, Vector2Int objectSize) {
        List<Vector3Int> returnVal = new();
        for (int x = 0; x < objectSize.x; x++) {
            for (int y = 0; y < objectSize.y; y++) {
                returnVal.Add(gridPosition + new Vector3Int(x, 0, y));
            }
        }
        return returnVal;
    }

    public PlacementData GetPlacementData(Vector3Int gridPosition) {
        if (placedObjects.TryGetValue(gridPosition, out PlacementData data)) {
            return data;
        }
        return null;
    }

    public void RemoveObjectAt(Vector3Int gridPosition) {
        foreach (var pos in placedObjects[gridPosition].occupiedPositions) {
            placedObjects.Remove(pos);
        }
    }
}

public class PlacementData {
    public List<Vector3Int> occupiedPositions;
    public bool IsTraversable;
    public GameObject PlaceObject { get; private set; }

    public PlacementData(List<Vector3Int> occupiedPositions, GameObject placeObject, bool isTraversable) {
        this.occupiedPositions = occupiedPositions;
        this.IsTraversable = isTraversable;
        PlaceObject = placeObject;
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonGeneration {
    public class DungeonData {
        private List<RoomData> RoomList = new();
        private List<DoorData> DoorList = new();

        public void AddRoom(RoomData newRoom) => RoomList.Add(newRoom);
        public void AddDoor(DoorData newDoor) {
            RoomData roomA = newDoor.ConnectedRooms[0];
            RoomData roomB = newDoor.ConnectedRooms[1];

            // check if door already exists between these rooms
            foreach (DoorData door in DoorList) {
                if (door.ConnectedRooms.Contains(roomA) && door.ConnectedRooms.Contains(roomB)) {
                    Debug.Log("Door already exists between these rooms!");
                    return;
                }
            }

            roomA.AddConnectedDoor(newDoor);
            roomB.AddConnectedDoor(newDoor);

            DoorList.Add(newDoor);
        }

        public bool RemoveRoom(RoomData room, bool checkIfCreatesIsland = false) {
            if(checkIfCreatesIsland && CreatesIsland(RoomList.IndexOf(room))){
                return false;
            }

            List<DoorData> connectedDoors = new(room.ConnectedDoors);
            foreach (DoorData door in connectedDoors) {
                RemoveDoor(door);
            }

            RoomList.Remove(room);
            return true;
        }

        public void RemoveDoor(DoorData door) {
            foreach (RoomData room in door.ConnectedRooms) {
                room.ConnectedDoors.Remove(door);
            }

            DoorList.Remove(door);
        }

        public int GetIndexOfRoom(RoomData room) => RoomList.IndexOf(room);
        public List<RoomData> GetDungeonRooms() => new(RoomList);
        public List<DoorData> GetDungeonDoors() => new(DoorList);

        public void Clear() {
            RoomList.Clear();
            DoorList.Clear();
        }

        private bool CreatesIsland(int removeRoom) {
            int rootroom = 0;

            if (removeRoom == rootroom)
                rootroom = 1;

            HashSet<RoomData> discoveredRooms = new() { RoomList[rootroom] };
            Queue<RoomData> roomQue = new();
            roomQue.Enqueue(RoomList[rootroom]);

            RoomData roomToRemove = RoomList[removeRoom];
            while (roomQue.Count > 0) {
                RoomData room = roomQue.Dequeue();

                for (int i = room.ConnectedDoors.Count - 1; i >= 0; i--) {
                    DoorData door = room.ConnectedDoors[i];

                    RoomData connectedRoom = null;
                    foreach (RoomData doorRoom in door.ConnectedRooms) {
                        if (doorRoom == room) continue;
                        connectedRoom = doorRoom;
                    }

                    if (discoveredRooms.Contains(connectedRoom) || connectedRoom == roomToRemove)
                        continue;

                    roomQue.Enqueue(connectedRoom);
                    discoveredRooms.Add(connectedRoom);
                }
            }

            return discoveredRooms.Count != RoomList.Count - 1;
        }

        public void RemoveCyclesBFS(int startRoomIndex = 0) {
            Queue<RoomData> roomQue = new();
            HashSet<RoomData> discoveredRooms = new() { RoomList[startRoomIndex] };
            HashSet<DoorData> discoveredDoors = new();
            roomQue.Enqueue(RoomList[startRoomIndex]);

            while (roomQue.Count > 0) {
                RoomData room = roomQue.Dequeue();

                for (int i = room.ConnectedDoors.Count - 1; i >= 0; i--) {
                    DoorData door = room.ConnectedDoors[i];

                    if (discoveredDoors.Contains(door)) continue;

                    RoomData connectedRoom = null;
                    foreach (RoomData doorRoom in door.ConnectedRooms) {
                        if (doorRoom == room) continue;
                        connectedRoom = doorRoom;
                    }

                    if (discoveredRooms.Contains(connectedRoom)) {
                        RemoveDoor(door);
                        continue;
                    }

                    roomQue.Enqueue(connectedRoom);
                    discoveredRooms.Add(connectedRoom);
                    discoveredDoors.Add(door);
                }
            }
        }

        public void RemoveCyclesDFS(int startRoomIndex = 0, bool randomised = false) {
            Stack<RoomData> roomStack = new();
            HashSet<RoomData> discoveredRooms = new() { RoomList[startRoomIndex] };
            HashSet<DoorData> discoveredDoors = new();
            roomStack.Push(RoomList[startRoomIndex]);

            while (roomStack.Count > 0) {
                RoomData room = roomStack.Pop();

                for (int i = room.ConnectedDoors.Count - 1; i >= 0; i--) {
                    DoorData door = room.ConnectedDoors[i];

                    if (discoveredDoors.Contains(door)) continue;

                    RoomData connectedRoom = null;
                    foreach (RoomData doorRoom in door.ConnectedRooms) {
                        if (doorRoom == room) continue;
                        connectedRoom = doorRoom;
                    }

                    if (discoveredRooms.Contains(connectedRoom)) {
                        RemoveDoor(door);
                        continue;
                    }

                    roomStack.Push(connectedRoom);
                    discoveredRooms.Add(connectedRoom);
                    discoveredDoors.Add(door);
                }
            }
        }



        // Fisher-Yates Shuffle
        // https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle
        private static void Shuffle<T>(List<T> list) {
            int n = list.Count;
            for (int i = n - 1; i > 0; i--) {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }

    public class RoomData {
        public RectInt Bounds { get; private set; }
        public int Height { get; private set; }
        public float Surface { get; private set; }
        public List<DoorData> ConnectedDoors { get; private set; } = new();

        public RoomData(RectInt bounds, int height) {
            Bounds = bounds;
            Height = height;
            Surface = bounds.height * bounds.width;
        }

        public void AddConnectedDoor(DoorData Door) => ConnectedDoors.Add(Door);
    }

    public class DoorData {
        public RectInt Bounds { get; private set; }
        public int Height { get; private set; }
        public RoomData[] ConnectedRooms { get; private set; } = new RoomData[2];

        public DoorData(RectInt bounds, int height, RoomData roomA, RoomData roomB) {
            Bounds = bounds;
            Height = height;
            ConnectedRooms[0] = roomA;
            ConnectedRooms[1] = roomB;
        }
    }


    public class GridData {
        Dictionary<Vector3Int, PlacementData> placedObjects = new();

        public void AddObjectAt(Vector3Int gridPosition,
                                Vector2Int objectSize,
                                int ID,
                                GameObject placedObjectIndex
            ) {
            List<Vector3Int> positionToOccupy = CalculatePositions(gridPosition, objectSize);
            PlacementData data = new(positionToOccupy, ID, placedObjectIndex);
            foreach (var pos in positionToOccupy) {
                if (placedObjects.ContainsKey(pos)) {
                    throw new System.Exception($"Dictionary already contains this cell position {pos}");
                }
                placedObjects[pos] = data;
            }
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

        public bool CanPlaceObjectAt(Vector3Int gridPosition, Vector2Int objectSize) {
            List<Vector3Int> positionToOccupy = CalculatePositions(gridPosition, objectSize);
            foreach (var pos in positionToOccupy) {
                if (placedObjects.ContainsKey(pos)) {
                    return false;
                }
            }
            return true;
        }

        public GameObject GetRepresentationIndex(Vector3Int gridPosition) {
            if (placedObjects.ContainsKey(gridPosition) == false) {
                return null;
            }
            return placedObjects[gridPosition].PlaceObject;
        }

        public void RemoveObjectAt(Vector3Int gridPosition) {
            foreach (var pos in placedObjects[gridPosition].occupiedPositions) {
                placedObjects.Remove(pos);
            }
        }
    }

    public class PlacementData {
        public List<Vector3Int> occupiedPositions;

        public int ID { get; private set; }
        public GameObject PlaceObject { get; private set; }

        public PlacementData(List<Vector3Int> occupiedPositions, int iD, GameObject placeObject) {
            this.occupiedPositions = occupiedPositions;
            ID = iD;
            PlaceObject = placeObject;
        }
    }


}
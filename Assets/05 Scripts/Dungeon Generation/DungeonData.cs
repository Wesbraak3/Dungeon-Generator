using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonGeneration {
    [Serializable]
    public class DungeonData {
        [SerializeField] private List<RoomData> RoomList = new();
        [SerializeField] private List<DoorData> DoorList = new();

        public void AddRoom(RoomData newRoom) {
            RoomList.Add(newRoom);
        }
        public void AddDoor(DoorData newDoor) {
            RoomData roomA = newDoor.ConnectedRooms[0];
            RoomData roomB = newDoor.ConnectedRooms[1];

            foreach (DoorData door in DoorList) {
                if (door.ConnectedRooms.Contains(roomA) && door.ConnectedRooms.Contains(roomB)) {
                    Debug.Log("Door already exists between these rooms!");
                    return;
                }
            }

            DoorList.Add(newDoor);
        }

        public void RemoveRoom(RoomData room) {
            foreach (DoorData door in DoorList) {
                if (door.ConnectedRooms.Contains(room)) {
                    RemoveDoor(door);
                }
            }

            RoomList.Remove(room);
        }
        public void RemoveDoor(DoorData door) {
            DoorList.Remove(door);
        }

        public void Clear() {
            RoomList.Clear();
            DoorList.Clear();
        }

        public List<RoomData> GetDungeonRooms() => RoomList;
        public List<DoorData> GetDungeonDoors() => DoorList;
    }

    [Serializable]
    public class RoomData {
        public RectInt Bounds;
        public int Height;

        public RoomData(RectInt bounds, int height) {
            Bounds = bounds;
            Height = height;
        }
    }

    [Serializable]
    public class DoorData {
        public RectInt Bounds;
        public int Height; 
        public RoomData[] ConnectedRooms = new RoomData[2];

        public DoorData(RectInt bounds, int height, RoomData roomA, RoomData roomB) {
            Bounds = bounds;
            Height = height;
            ConnectedRooms[0] = roomA;
            ConnectedRooms[1] = roomB;
        }
    }
}
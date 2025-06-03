using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
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

        public IEnumerator RemoveCyclesBFS(int startRoomIndex = 0) {
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
            yield break;
        }

        public IEnumerator RemoveCyclesDFS(int startRoomIndex = 0) {
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
            yield break;
        }

        public IEnumerator RemoveCyclesDFSRecursive(int startRoomIndex = 0) {
            HashSet<RoomData> discoveredRooms = new();
            HashSet<DoorData> discoveredDoors = new();

            RoomData startRoom = RoomList[startRoomIndex];
            discoveredRooms.Add(startRoom);

            void DFS(RoomData room) {
                foreach (DoorData door in room.ConnectedDoors.ToArray()) {
                    if (discoveredDoors.Contains(door)) continue;

                    //get the other room where room is not this room
                    RoomData connectedRoom = Array.Find(door.ConnectedRooms, r => r != room);

                    if (connectedRoom == null) continue;

                    if (discoveredRooms.Contains(connectedRoom)) {
                        RemoveDoor(door);
                        continue;
                    }

                    discoveredRooms.Add(connectedRoom);
                    discoveredDoors.Add(door);
                    DFS(connectedRoom);
                }
            }

            DFS(startRoom);
            yield break;
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
        public float Surface { get; private set; }
        public List<DoorData> ConnectedDoors { get; private set; } = new();

        public RoomData(RectInt bounds) {
            Bounds = bounds;
            Surface = bounds.height * bounds.width;
        }

        public void AddConnectedDoor(DoorData Door) => ConnectedDoors.Add(Door);
    }

    public class DoorData {
        public RectInt Bounds { get; private set; }
        public RoomData[] ConnectedRooms { get; private set; } = new RoomData[2];

        public DoorData(RectInt bounds, RoomData roomA, RoomData roomB) {
            Bounds = bounds;
            ConnectedRooms[0] = roomA;
            ConnectedRooms[1] = roomB;
        }
    }
}
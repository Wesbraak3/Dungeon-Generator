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

        public void RemoveRoom(RoomData room) {
            List<DoorData> connectedDoors = new(room.ConnectedDoors);
            foreach (DoorData door in connectedDoors) {
                RemoveDoor(door);
            }

            RoomList.Remove(room);
        }

        public void RemoveDoor(DoorData door) {
            foreach (RoomData room in door.ConnectedRooms) {
                room.ConnectedDoors.Remove(door);
            }

            DoorList.Remove(door);
        }

        public List<RoomData> GetDungeonRooms() => new(RoomList);
        public List<DoorData> GetDungeonDoors() => new(DoorList);

        public void Clear() {
            RoomList.Clear();
            DoorList.Clear();
        }

        //public void BFS(int startRoomIndex = 0) {
        //    Queue<RoomData> Q = new ();
        //    HashSet<RoomData> discovered = new() { RoomList[startRoomIndex] };
        //    Q.Enqueue(RoomList[startRoomIndex]);

        //    while (Q.Count > 0) {
        //        RoomData v = Q.Dequeue();

        //        List<RoomData> removeList = new();
        //        foreach (RoomData vNew in v.ConnectedDoors) {
        //            if (!discovered.Contains(vNew)) {
        //                Q.Enqueue(vNew);
        //                discovered.Add(vNew);
        //            }
        //            else {
        //                removeList.Add(vNew);
        //            }
        //        }
        //        foreach (RoomData room in removeList) v.ConnectedDoors.Remove(room);
        //    }
        //}

        /* RemoveCyclesDFS
        public void RemoveCyclesDFS(bool randomised = false) {
            Stack<RoomData> Q = new();
            HashSet<RoomData> discovered = new() { RoomList[0] };
            Q.Push(RoomList[0]);

            while (Q.Count > 0) {
                RoomData v = Q.Pop();

                List<RoomData> tempList = new();
                List<RoomData> removeList = new();
                foreach (RoomData vConnecction in v.ConnectedDoors) {
                    if (!discovered.Contains(vConnecction)) {
                        if (randomised) tempList.Add(vConnecction);
                        else Q.Push(vConnecction);
                        discovered.Add(vConnecction);
                    } 
                    else {
                        removeList.Add(vConnecction);
                    }
                }
                foreach (RoomData room in removeList) v.ConnectedDoors.Remove(room);

                if (randomised) {
                    Shuffle(tempList);
                    foreach (RoomData room in tempList) Q.Push(room);
                }
            }
        }
        */

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
        public HashSet<DoorData> ConnectedDoors { get; private set; } = new();

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
}
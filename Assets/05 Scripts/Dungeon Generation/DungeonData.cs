using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Search;
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

        public void RemoveCyclesBFS() {
            Queue<RoomData> Q = new ();
            HashSet<RoomData> discovered = new() { RoomList[0] };
            Q.Enqueue(RoomList[0]);

            while (Q.Count > 0) {
                RoomData v = Q.Dequeue();

                List<RoomData> removeList = new();
                foreach (RoomData vNew in v.ConnectedRooms) {
                    if (!discovered.Contains(vNew)) {
                        Q.Enqueue(vNew);
                        discovered.Add(vNew);
                    }
                    else {
                        removeList.Add(vNew);
                    }
                }
                foreach (RoomData room in removeList) v.ConnectedRooms.Remove(room);
            }
        }

        public void RemoveCyclesDFS(bool randomised = true) {
            Stack<RoomData> Q = new();
            HashSet<RoomData> discovered = new() { RoomList[0] };
            Q.Push(RoomList[0]);

            while (Q.Count > 0) {
                RoomData v = Q.Pop();

                List<RoomData> tempList = new();
                List<RoomData> removeList = new();
                foreach (RoomData vNew in v.ConnectedRooms) {
                    if (!discovered.Contains(vNew)) {
                        if (randomised) tempList.Add(vNew);
                        else Q.Push(vNew);
                        discovered.Add(vNew);
                    } 
                    else {
                        removeList.Add(vNew);
                    }
                }
                foreach (RoomData room in removeList) v.ConnectedRooms.Remove(room);

                if (randomised) {
                    Shuffle(tempList);
                    foreach (RoomData room in tempList) Q.Push(room);
                }
            }
        }

        // Fisher-Yates Shuffle
        private static void Shuffle<T>(List<T> list) {
        int n = list.Count;
            for (int i = n - 1; i > 0; i--) {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        public List<RoomData> GetDungeonRooms() => RoomList;
        public List<DoorData> GetDungeonDoors() => DoorList;
    }

    [Serializable]
    public class RoomData {
        public RectInt Bounds { get; private set; }
        public int Height { get; private set; }
        public float Surface { get; private set; }
        public HashSet<RoomData> ConnectedRooms { get; private set; } = new();

        public RoomData(RectInt bounds, int height) {
            Bounds = bounds;
            Height = height;
            Surface = bounds.height * bounds.width;
        }

        public void AddConnection(RoomData roomA, RoomData roomB) {
            roomA.ConnectedRooms.Add(roomB);
            roomB.ConnectedRooms.Add(roomA);
        }
    }

    [Serializable]
    public class DoorData {
        public RectInt Bounds;
        public int Height;
        public RoomData[] ConnectedRooms = new RoomData[2];
        public bool IsLocked = false;

        public DoorData(RectInt bounds, int height, RoomData roomA, RoomData roomB) {
            Bounds = bounds;
            Height = height;
            ConnectedRooms[0] = roomA;
            ConnectedRooms[1] = roomB;
        }
    }

    public class Graph {
        
    }
}
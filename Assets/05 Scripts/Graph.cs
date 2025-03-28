using System.Collections.Generic;
using UnityEngine;

namespace AlgoBootcamp {
    public class Graph<T> {
        private Dictionary<T, List<T>> adjacencyDict;

        public Graph() {
            adjacencyDict = new Dictionary<T, List<T>>();
        }

        public void AddNode(T node) {
            if (adjacencyDict.ContainsKey(node)) {
                Debug.Log("it already exist my man");
                return;
            }

            adjacencyDict[node] = new List<T>();
        }
        public void AddEdge(T node, T edge) {
            if (!adjacencyDict.ContainsKey(node) || adjacencyDict[node].Contains(edge)) {
                Debug.Log("it doesnt exist or aleady is connected bimbo");
                return;
            }

            adjacencyDict[node].Add(edge);
            adjacencyDict[edge].Add(node);
        }

        public List<T> GetNeighbors(T node) => new(adjacencyDict[node]);

        public void BFS(T v) {
            Queue<T> Q = new();
            Q.Enqueue(v);

            // hash set is faster for looking up values in an list
            // it doesnt go trought it 1 by one but jumps to position
            HashSet<T> discovered = new() { v };
            //List<T> discovered = new() {v};

            while (Q.Count > 0) {
                v = Q.Dequeue();
                Debug.Log(v);

                foreach (T w in GetNeighbors(v)) {
                    if (!discovered.Contains(w)) {
                        Q.Enqueue(w);
                        discovered.Add(w);
                    }
                }
            }
        }
        public void DFS(T v) {
            Stack<T> S = new();
            S.Push(v);

            HashSet<T> discovered = new() { v };

            while (S.Count > 0) {
                v = S.Pop();
                Debug.Log(v);

                foreach (T w in GetNeighbors(v)) {
                    if (!discovered.Contains(w)) {
                        S.Push(w);
                        discovered.Add(w);
                    }
                }
            }
        }

        public void PrintGraph() {

            foreach (var node in adjacencyDict) {

                Debug.Log("Node: " + node.Key + ", Edges: " + string.Join(", ", node.Value));
            }
        }
    }
}
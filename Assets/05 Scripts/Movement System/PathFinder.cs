using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public enum Algorithms {
    BFS,
    Dijkstra,
    AStar
}

public class PathFinder : MonoBehaviour {
    private Vector3 startNode;
    private Vector3 endNode;

    public List<Vector3> path = new();
    HashSet<Vector3> discovered = new();

    public Algorithms algorithm = Algorithms.BFS;

    public GridManager gridManager;

    private void Start() {
        gridManager = GridManager.instance;
    }

    public List<Vector3> CalculatePath(Vector3 from, Vector3 to) {
        startNode = gridManager.GetClosestGridPosition(from);
        endNode = gridManager.GetGridPosition(to);
        startNode.y = 0;
        endNode.y = 0;
        Debug.Log("from: " + from + "to: " + to);
        Debug.Log("startNode: " + startNode + "endNode: " + endNode); 

        List<Vector3> shortestPath = new();

        switch (algorithm) {
            case Algorithms.BFS:
                shortestPath = BFS(startNode, endNode);
                break;
            case Algorithms.Dijkstra:
                shortestPath = Dijkstra(startNode, endNode);
                break;
            case Algorithms.AStar:
                shortestPath = AStar(startNode, endNode);
                break;
        }

        path = shortestPath; //Used for drawing the path
        return shortestPath;
    }

    List<Vector3> BFS(Vector3 start, Vector3 end) {
        //Use this "discovered" list to see the nodes in the visual debugging used on OnDrawGizmos()
        discovered.Clear();

        Queue<Vector3> Q = new();
        Q.Enqueue(start);
        discovered.Add(start);

        Dictionary<Vector3, Vector3> P = new(); // dict node and parent

        while (Q.Count != 0) {
            Vector3 v = Q.Dequeue();

            if (v == end) return ReconstructPath(P, start, end);

            foreach (Vector3 w in gridManager.GetAdjecentNodes(v)) {
                if (!discovered.Contains(w)) {
                    Q.Enqueue(w);
                    discovered.Add(w);
                    P[w] = v;
                }
            }
        }
        return new List<Vector3>(); // No path found
    }

    public List<Vector3> Dijkstra(Vector3 start, Vector3 end) {
        //Use this "discovered" list to see the nodes in the visual debugging used on OnDrawGizmos()
        discovered.Clear();

        PriorityQueue<Vector3, float> Q = new();
        Q.Enqueue(start, 0);

        Dictionary<Vector3, Vector3> P = new(); // dict node and parent
        Dictionary<Vector3, float> C = new() {
            [start] = 0
        }; // dict for traferse cost

        while (Q.Count != 0) {
            Vector3 v = Q.Dequeue();
            discovered.Add(v);

            if (v == end) return ReconstructPath(P, start, end);

            foreach (Vector3 w in gridManager.GetAdjecentNodes(v)) {
                float newCost = C[v] + Cost(v, w);

                if (!C.ContainsKey(w) || newCost < C[w]) {
                    C[w] = newCost;
                    P[w] = v;
                    Q.Enqueue(w, newCost);
                }
            }
        }
        return new List<Vector3>(); // No path found
    }

    List<Vector3> AStar(Vector3 start, Vector3 end) {
        //Use this "discovered" list to see the nodes in the visual debugging used on OnDrawGizmos()
        discovered.Clear();

        PriorityQueue<Vector3, float> Q = new();
        Q.Enqueue(start, 0);

        Dictionary<Vector3, Vector3> P = new(); // dict node and parent
        Dictionary<Vector3, float> C = new() {
            [start] = 0
        }; // dict for traferse cost

        while (Q.Count != 0) {
            Vector3 v = Q.Dequeue();
            discovered.Add(v);

            if (v == end) return ReconstructPath(P, start, end);

            foreach (Vector3 w in gridManager.GetAdjecentNodes(v)) {
                float newCost = C[v] + Cost(v, w);

                if (!C.ContainsKey(w) || newCost < C[w]) {
                    C[w] = newCost;
                    P[w] = v;
                    Q.Enqueue(w, newCost + Heuristic(w, end));
                }
            }
        }
        return new List<Vector3>(); // No path found
    }

    public float Cost(Vector3 from, Vector3 to) {
        float distance = Vector3.Distance(from, to);
        bool isTraversable = gridManager.IsTileTraversable(to);
        if (!isTraversable) {
            return Mathf.Infinity;
        }
        return Vector3.Distance(from, to);
    }

    public float Heuristic(Vector3 from, Vector3 to) {
        return Vector3.Distance(from, to);
    }

    List<Vector3> ReconstructPath(Dictionary<Vector3, Vector3> parentMap, Vector3 start, Vector3 end) {
        List<Vector3> path = new ();
        Vector3 currentNode = end;

        while (currentNode != start) {
            path.Add(currentNode);
            currentNode = parentMap[currentNode];
        }

        path.Add(start);
        path.Reverse();
        return path;
    }

    void OnDrawGizmos() {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(startNode, .3f);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(endNode, .3f);

        if (discovered != null) {
            foreach (var node in discovered) {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(node, .3f);
            }
        }

        if (path != null) {
            foreach (var node in path) {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(node, .3f);
            }
        }
    }
}

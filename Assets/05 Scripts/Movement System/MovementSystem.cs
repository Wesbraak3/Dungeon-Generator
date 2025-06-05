using DungeonGeneration;
using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class MovementSystem : MonoBehaviour {
    [SerializeField]
    private PathFinder pathFinder;

    [SerializeField]
    private float speed = 5f;
    private bool isMoving = false;
    Coroutine test;

    [Header("Movement")]
    public bool useNavMesh = true;
    public NavMeshSurface navMeshSurface;
    public NavMeshAgent navMeshAgent;

    private void Awake() {
        if (navMeshSurface == null) {
            navMeshSurface = DungeonManager.instance.GetNavMeshSurface();
            navMeshAgent = gameObject.AddComponent<NavMeshAgent>();
        }
    }

    public void GoToDestination(Vector3 destination) {
        if (useNavMesh) {
            Debug.Log("test");
            navMeshAgent.destination = destination;
            return;
        }


        if (test != null) {
            StopCoroutine(test);
        }
        test = StartCoroutine(FollowPathCoroutine(pathFinder.CalculatePath(transform.position, destination)));
    }

    IEnumerator FollowPathCoroutine(List<Vector3> path) {
        if (path == null || path.Count == 0) {
            yield break;
        }

        isMoving = true;
        for (int i = 1; i < path.Count; i++) {
            Vector3 target = path[i];

            while (Vector3.Distance(transform.position, target) > 0.1f) {
                transform.position = Vector3.MoveTowards(transform.position, target, Time.deltaTime * speed);
                yield return null;
            }
        }
        isMoving = false;
    }
}
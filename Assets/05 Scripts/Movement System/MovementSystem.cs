using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementSystem : MonoBehaviour {
    [SerializeField]
    private PathFinder pathFinder;

    [SerializeField]
    private float speed = 5f;
    private bool isMoving = false;
    Coroutine test;

    public void GoToDestination(Vector3 destination) {
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

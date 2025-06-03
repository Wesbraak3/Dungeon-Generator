using UnityEngine;
using UnityEngine.Events;

public class MouseClickController : MonoBehaviour {
    private Vector3 clickPosition;
    [SerializeField] private Camera camera;

    public UnityEvent<Vector3> OnClick;

    private void Start() {
        camera = camera == null ? Camera.main : camera;
    }

    void Update() {
        if (Input.GetMouseButtonDown(0)) {
            Ray mouseRay = camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(mouseRay, out RaycastHit hitInfo)) {
                Vector3 clickWorldPosition = hitInfo.point;
                clickPosition = clickWorldPosition;

                OnClick.Invoke(clickPosition);
            }
        }

        DebugExtension.DebugWireSphere(clickPosition, Color.yellow, .1f);
        Debug.DrawLine(camera.transform.position, clickPosition, Color.yellow);
    }
}

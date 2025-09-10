using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BallClickable : MonoBehaviour
{
    [Tooltip("Drag your RobotFollowManager (with RobotFollowController) here.")]
    public RobotFollowController follower; // assign in Inspector

    bool isHeld = false;
    Camera cam;
    Plane dragPlane;

    void Start()
    {
        cam = Camera.main;
        if (cam == null) Debug.LogWarning("BallClickable: No Camera tagged MainCamera found.");
        dragPlane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
    }

    void OnMouseDown() => BeginHold();
    void OnMouseDrag()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        float enter;
        if (dragPlane.Raycast(ray, out enter)) transform.position = ray.GetPoint(enter);
        else transform.position = ray.GetPoint(5f);
    }
    void OnMouseUp() => EndHold();
    void Update() { if (isHeld && Input.GetMouseButtonUp(0)) EndHold(); }

    void BeginHold()
    {
        if (isHeld) return;
        isHeld = true;
        // follower should already be assigned in Inspector
        if (follower == null) Debug.LogWarning("BallClickable: follower not assigned in Inspector.");
        follower?.StartFollowing(transform);
    }

    void EndHold()
    {
        if (!isHeld) return;
        isHeld = false;
        follower?.StopFollowing();
    }

    void OnDisable() { if (isHeld) EndHold(); }
}

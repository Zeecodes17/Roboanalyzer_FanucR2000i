using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BallClickable_Raycast : MonoBehaviour
{
    public RobotFollowController follower;   // assign in Inspector
    private bool isHeld;
    private Camera cam;

    // NEW: store distance from camera when hold begins
    private float holdDistance = 0f;

    void Start()
    {
        cam = Camera.main;
        if (cam == null) Debug.LogWarning("BallClickable: No Camera tagged MainCamera found!");
    }

    void Update()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                if (hit.collider.gameObject == gameObject)
                {
                    // record the distance along the ray where the click hit the ball
                    holdDistance = hit.distance;
                    BeginHold();
                }
            }
        }

        if (isHeld && Input.GetMouseButton(0))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            // use the *same* camera-space distance so depth stays constant
            transform.position = ray.GetPoint(holdDistance);
        }

        if (isHeld && Input.GetMouseButtonUp(0))
            EndHold();
    }

    void BeginHold()
    {
        if (isHeld) return;
        isHeld = true;
        if (follower == null) Debug.LogWarning("Follower not assigned!");
        Debug.Log($"Ball: BeginHold (holdDistance={holdDistance:F3})");
        follower?.StartFollowing(transform);
    }

    void EndHold()
    {
        if (!isHeld) return;
        isHeld = false;
        Debug.Log($"Ball: EndHold");
        follower?.StopFollowing();
    }

    void OnDisable()
    {
        if (isHeld) EndHold();
    }
}

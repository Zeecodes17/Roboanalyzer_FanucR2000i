using UnityEngine;

/// <summary>
/// Snap this GameObject to the given effector position at Start.
/// Keeps the target a free world-space object (not parented).
/// </summary>
[DisallowMultipleComponent]
public class SnapTargetToEffector : MonoBehaviour
{
    [Tooltip("Effector/Flange transform to snap to on Start.")]
    public Transform effector;

    [Tooltip("If true, also copy rotation from the effector.")]
    public bool copyRotation = false;

    [Tooltip("If true, do not snap when the target was moved in editor (non-default position).")]
    public bool skipIfMovedInEditor = true;

    void Start()
    {
        if (effector == null) return;

        // If user already positioned the target in editor (non-zero and not the same as effector), skip snapping
        if (skipIfMovedInEditor)
        {
            if (transform.position != Vector3.zero && transform.position != effector.position) return;
        }

        transform.position = effector.position;
        if (copyRotation) transform.rotation = effector.rotation;
    }
}

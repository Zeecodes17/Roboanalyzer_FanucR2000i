using UnityEngine;

public class MeasureReach : MonoBehaviour
{
    [Tooltip("End-effector Transform (Effector_Marker)")]
    public Transform effector;
    [Tooltip("Robot base/origin Transform (where reach is measured from)")]
    public Transform baseTransform;

    void Start()
    {
        if (effector == null || baseTransform == null)
        {
            Debug.LogError("MeasureReach: assign effector and baseTransform in Inspector.");
            return;
        }

        float d = Vector3.Distance(effector.position, baseTransform.position);
        Debug.Log($"MeasureReach: straight-line distance = {d:F3} units. " +
                  $"Suggested maxReach = {d * 0.95f:F3} (95% of full reach).");
        // optional: auto-destroy so it won't keep printing if you forget
        Destroy(this);
    }
}
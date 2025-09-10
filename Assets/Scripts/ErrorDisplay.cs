using UnityEngine;
using TMPro;

public class ErrorDisplay : MonoBehaviour
{
    [Header("References")]
    public Transform effector;   // Effector_Marker
    public Transform target;     // IK_Target
    public TextMeshProUGUI textUI; // ErrorText_TMP in Canvas

    [Header("Thresholds (units)")]
    public float warnThreshold = 0.2f;   // yellow if error > 0.2
    public float dangerThreshold = 0.5f; // red if error > 0.5

    void Update()
    {
        if (effector == null || target == null || textUI == null) return;

        // compute error
        float errorDist = Vector3.Distance(effector.position, target.position);

        // format text
        textUI.text = $"Pose Error: {errorDist:F3}";

        // color-code
        if (errorDist < warnThreshold)
            textUI.color = Color.green;
        else if (errorDist < dangerThreshold)
            textUI.color = Color.yellow;
        else
            textUI.color = Color.red;
    }
}

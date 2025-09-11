using UnityEngine;
using TMPro;  // << important: gives access to TMP_Text

[RequireComponent(typeof(Canvas))]
public class ErrorVisualizer_TMP : MonoBehaviour   // << class name must match filename
{
    [Header("References")]
    public Transform ikTarget;        // desired target position
    public Transform endEffector;     // robot's actual end effector
    public TMP_Text errorText;        // << now TMP_Text, not UnityEngine.UI.Text

    [Header("Display")]
    public float warnThreshold = 0.02f;   // <= green -> yellow
    public float alertThreshold = 0.06f;  // <= yellow -> red
    public string prefix = "IK Error: ";

    void Update()
    {
        if (ikTarget == null || endEffector == null || errorText == null)
        {
            if (errorText != null) errorText.gameObject.SetActive(false);
            return;
        }

        float dist = Vector3.Distance(endEffector.position, ikTarget.position);
        errorText.text = $"{prefix}{dist:F3} m";

        if (dist <= warnThreshold)
            errorText.color = Color.green;
        else if (dist <= alertThreshold)
            errorText.color = new Color(1f, 0.65f, 0f); // orange
        else
            errorText.color = Color.red;
    }
}

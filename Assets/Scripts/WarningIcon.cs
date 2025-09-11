using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class WarningIcon : MonoBehaviour
{
    [Header("References")]
    public Transform ikTarget;
    public Transform endEffector;
    public Image iconImage; // the UI Image component to color/pulse

    [Header("Thresholds")]
    public float warnThreshold = 0.02f;   // > green -> yellow
    public float alertThreshold = 0.06f;  // > yellow -> red

    [Header("Pulse")]
    public float pulseSpeed = 3f;
    public float maxScale = 1.25f;

    Vector3 baseScale;

    void Awake()
    {
        if (iconImage == null) iconImage = GetComponent<Image>();
        baseScale = transform.localScale;
    }

    void Update()
    {
        if (ikTarget == null || endEffector == null || iconImage == null)
        {
            iconImage?.gameObject.SetActive(false);
            return;
        }

        float dist = Vector3.Distance(endEffector.position, ikTarget.position);

        // color logic
        if (dist <= warnThreshold) iconImage.color = Color.green;
        else if (dist <= alertThreshold) iconImage.color = new Color(1f, 0.65f, 0f); // orange
        else iconImage.color = Color.red;

        // pulse when dist > warnThreshold
        float t = (dist > warnThreshold) ? (Mathf.PingPong(Time.time * pulseSpeed, 1f)) : 0f;
        float s = 1f + (maxScale - 1f) * t * (dist > warnThreshold ? 1f : 0f);
        transform.localScale = baseScale * s;
    }
}

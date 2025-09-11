using UnityEngine;
using System.Collections.Generic;

public class PathFollower : MonoBehaviour
{
    [Header("Smoothing")]
    public float followSmoothing = 10f;     // higher = tighter follow (try 8–20)
    public float lookaheadDistance = 0.06f; // meters ahead along path (try 0.03–0.12)

    [Tooltip("The IK target transform your IK solver reads")]
    public Transform ikTarget;

    [Tooltip("Robot base used to clamp reach")]
    public Transform robotBase;
    public float maxReach = 1.5f;

    public float speed = 0.5f;      // meters per second
    public bool loop = false;

    private List<Vector3> path = new List<Vector3>();
    private float[] segLengths;
    private float[] cumLengths;
    private float totalLength = 0f;
    private float progress = 0f;
    private bool moving = false;

    // Called by PathDrawer (or manually) to set the path
    public void SetPath(List<Vector3> points, bool loopPath = false)
    {
        if (points == null || points.Count < 2)
        {
            ClearPath();
            return;
        }

        path = new List<Vector3>(points);
        loop = loopPath;
        BuildLengths();
        progress = 0f;
        moving = true;
        Debug.Log($"[PathFollower] Received path with {path.Count} points, totalLength={totalLength:F2}");
    }

    public void ClearPath()
    {
        path.Clear();
        segLengths = null;
        cumLengths = null;
        totalLength = 0f;
        moving = false;
    }

    void BuildLengths()
    {
        int n = path.Count;
        segLengths = new float[n - 1];
        cumLengths = new float[n];
        cumLengths[0] = 0f;
        totalLength = 0f;
        for (int i = 0; i < n - 1; i++)
        {
            float l = Vector3.Distance(path[i], path[i + 1]);
            segLengths[i] = l;
            totalLength += l;
            cumLengths[i + 1] = totalLength;
        }
    }

    void Update()
    {
        if (!moving || path == null || path.Count < 2) return;

        // Advance along path
        progress += speed * Time.deltaTime;
        if (loop && totalLength > 0f)
            progress = Mathf.Repeat(progress, totalLength);
        else
            progress = Mathf.Min(progress, totalLength);

        // Sample current + lookahead points
        Vector3 desired = SampleAtDistance(progress);
        float lookDist = loop ? Mathf.Repeat(progress + lookaheadDistance, totalLength)
                              : Mathf.Min(progress + lookaheadDistance, totalLength);
        Vector3 lookPoint = SampleAtDistance(lookDist);

        Vector3 targetPos = lookPoint;

        // Clamp to max reach
        if (robotBase != null)
        {
            Vector3 dir = targetPos - robotBase.position;
            if (dir.magnitude > maxReach)
                targetPos = robotBase.position + dir.normalized * maxReach;
        }

        // Smoothly move ikTarget toward targetPos
        if (ikTarget != null)
        {
            float k = 1f - Mathf.Exp(-followSmoothing * Time.deltaTime);
            ikTarget.position = Vector3.Lerp(ikTarget.position, targetPos, k);
        }

        if (!loop && Mathf.Approximately(progress, totalLength))
            moving = false;
    }


    Vector3 SampleAtDistance(float distance)
    {
        if (distance <= 0f) return path[0];
        if (distance >= totalLength) return path[path.Count - 1];

        int idx = 0;
        while (idx < segLengths.Length && distance > cumLengths[idx + 1]) idx++;

        float segStart = cumLengths[idx];
        float segLen = segLengths[idx];
        float t = (segLen > 0f) ? (distance - segStart) / segLen : 0f;
        return Vector3.Lerp(path[idx], path[idx + 1], t);
    }

    void OnDrawGizmos()
    {
        if (path == null || path.Count < 2) return;
        Gizmos.color = Color.green;
        for (int i = 0; i < path.Count - 1; i++)
            Gizmos.DrawLine(path[i], path[i + 1]);
    }
}
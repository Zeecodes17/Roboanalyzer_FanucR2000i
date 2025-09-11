using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

[RequireComponent(typeof(LineRenderer))]
public class PathDrawer : MonoBehaviour
{
    public Camera drawCamera;                 // defaults to Camera.main if not set
    public LayerMask groundLayer;             // set to your floor/ground
    public float minPointDistance = 0.05f;    // distance between recorded points
    public float groundY = 0f;                // fallback plane height if raycast misses

    [Header("Smoothing")]
    public int smoothingSamplesPerSegment = 6;
    public bool closedLoop = false;

    [Header("Behavior")]
    public bool autoStartOnPlay = true;       // auto-start capture when Play begins
    public bool debugLog = false;             // enable to print sampled points

    [Header("Optional (assign later)")]
    public PathFollower pathFollower;

    private LineRenderer lineRenderer;
    private List<Vector3> rawPoints = new List<Vector3>();
    private bool capturing = false;

    void Start()
    {
        if (drawCamera == null) drawCamera = Camera.main;
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;

        if (autoStartOnPlay) StartCapture();
    }

    void Update()
    {
        if (!capturing) return;

        if (Input.GetMouseButtonDown(0))
        {
            rawPoints.Clear();
            lineRenderer.positionCount = 0;
            SamplePointUnderMouse();
        }

        if (Input.GetMouseButton(0))
        {
            SamplePointUnderMouse();
        }

        if (Input.GetMouseButtonUp(0))
        {
            capturing = false; // stop recording
            if (debugLog) Debug.Log("[PathDrawer] Finished capture. Raw points: " + rawPoints.Count);
            FinishCapture();
        }
    }

    void SamplePointUnderMouse()
    {
        if (drawCamera == null) return;
        Ray ray = drawCamera.ScreenPointToRay(Input.mousePosition);
        Vector3 worldPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
        {
            worldPoint = hit.point;
        }
        else
        {
            Plane plane = new Plane(Vector3.up, new Vector3(0, groundY, 0));
            if (!plane.Raycast(ray, out float enter)) return;
            worldPoint = ray.GetPoint(enter);
        }

        if (rawPoints.Count == 0 || Vector3.Distance(worldPoint, rawPoints[rawPoints.Count - 1]) >= minPointDistance)
        {
            rawPoints.Add(worldPoint);
            lineRenderer.positionCount = rawPoints.Count;
            lineRenderer.SetPosition(rawPoints.Count - 1, worldPoint + Vector3.up * 0.01f);

            if (debugLog) Debug.Log("[PathDrawer] sample at: " + worldPoint.ToString("F3"));
        }
    }

    void FinishCapture()
    {
        if (rawPoints.Count < 2) return;

        // 1) Simplify raw points using RDP
        float simplifyEps = 0.06f; // tweak: 0.03 (more detail) .. 0.12 (straighter)
        List<Vector3> simplified = RamerDouglasPeucker(rawPoints, simplifyEps);

        // 2) Smooth with Catmull-Rom
        List<Vector3> smooth = CatmullRomSpline(simplified, Mathf.Max(1, smoothingSamplesPerSegment), closedLoop);

        // 3) Show smoothed curve
        lineRenderer.positionCount = smooth.Count;
        for (int i = 0; i < smooth.Count; i++)
            lineRenderer.SetPosition(i, smooth[i] + Vector3.up * 0.01f);

        // 4) Send to PathFollower
        if (pathFollower != null)
            pathFollower.SetPath(smooth, closedLoop);
    }

    // --- Helpers inside PathDrawer ---
    List<Vector3> RamerDouglasPeucker(List<Vector3> points, float eps)
    {
        if (points == null || points.Count < 3) return new List<Vector3>(points);

        int n = points.Count;
        bool[] keep = new bool[n];
        keep[0] = keep[n - 1] = true;

        Stack<(int a, int b)> stack = new Stack<(int, int)>();
        stack.Push((0, n - 1));

        while (stack.Count > 0)
        {
            var (a, b) = stack.Pop();
            float maxDist = 0f;
            int index = -1;
            Vector3 A = points[a], B = points[b];

            for (int i = a + 1; i < b; i++)
            {
                float dist = DistancePointToSegment(points[i], A, B);
                if (dist > maxDist) { maxDist = dist; index = i; }
            }

            if (maxDist > eps && index != -1)
            {
                keep[index] = true;
                stack.Push((a, index));
                stack.Push((index, b));
            }
        }

        List<Vector3> outPts = new List<Vector3>();
        for (int i = 0; i < n; i++) if (keep[i]) outPts.Add(points[i]);
        return outPts;
    }

    static float DistancePointToSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float ab2 = Vector3.SqrMagnitude(ab);
        if (ab2 == 0f) return Vector3.Distance(p, a);
        float t = Vector3.Dot(p - a, ab) / ab2;
        t = Mathf.Clamp01(t);
        Vector3 proj = a + ab * t;
        return Vector3.Distance(p, proj);
    }


    // control API
    public void StartCapture() { capturing = true; }
    public void StopCapture() { capturing = false; }
    public void ClearAll() { rawPoints.Clear(); lineRenderer.positionCount = 0; }
    public List<Vector3> GetRawPoints() { return new List<Vector3>(rawPoints); }

    // --- Internal Catmull-Rom spline (self-contained) ---
    static List<Vector3> CatmullRomSpline(List<Vector3> controlPoints, int samplesPerSegment, bool closed)
    {
        List<Vector3> outPts = new List<Vector3>();
        int n = controlPoints.Count;
        if (n < 2)
        {
            outPts.AddRange(controlPoints);
            return outPts;
        }

        int segCount = closed ? n : n - 1;
        for (int i = 0; i < segCount; i++)
        {
            Vector3 p0 = GetWrapped(controlPoints, i - 1, closed);
            Vector3 p1 = GetWrapped(controlPoints, i + 0, closed);
            Vector3 p2 = GetWrapped(controlPoints, i + 1, closed);
            Vector3 p3 = GetWrapped(controlPoints, i + 2, closed);

            for (int s = 0; s < samplesPerSegment; s++)
            {
                float t = (float)s / (float)samplesPerSegment;
                Vector3 pt = CatmullRom(p0, p1, p2, p3, t);
                outPts.Add(pt);
            }
        }

        if (!closed)
        {
            outPts.Add(controlPoints[n - 1]); // ensure last point included
        }
        else if (outPts.Count > 0)
        {
            outPts.Add(outPts[0]); // close loop
        }

        return outPts;
    }

    static Vector3 GetWrapped(List<Vector3> list, int index, bool closed)
    {
        if (closed)
        {
            int n = list.Count;
            index = (index % n + n) % n;
            return list[index];
        }
        else
        {
            index = Mathf.Clamp(index, 0, list.Count - 1);
            return list[index];
        }
    }

    static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * ((2f * p1) +
                       (-p0 + p2) * t +
                       (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                       (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }
}

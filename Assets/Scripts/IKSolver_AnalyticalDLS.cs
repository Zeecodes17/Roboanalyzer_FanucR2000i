// IKSolver_AnalyticalDLS.cs
using UnityEngine;

/// <summary>
/// Position-only Damped Least Squares IK solver (analytical Jacobian columns).
/// Extended with optional per-joint limits and smoothing (non-invasive).
/// </summary>
public class IKSolver_AnalyticalDLS : MonoBehaviour
{
    [Header("References")]
    public Transform target;
    public Transform[] joints; // ordered base -> ... -> last joint (parent of end effector)
    public Transform endEffector; // if null, will use last joint

    [Header("IK Parameters")]
    [Tooltip("Damping factor (lambda). Try 0.01 - 1.0")]
    public float damping = 0.1f;
    [Tooltip("Max solver iterations per Solve call")]
    public int maxIterations = 10;
    [Tooltip("Multiplier applied to computed delta angles")]
    public float step = 1.0f;
    [Tooltip("Stop when end-effector is within this distance (m)")]
    public float positionThreshold = 0.01f;
    public bool solveEveryFrame = true;

    [Header("Joint axes (local space)")]
    [Tooltip("Local axis for each joint (e.g. (0,1,0) for Y axis). Length must match joints.")]
    public Vector3[] localAxes;

    [Header("Limit & smoothing (optional)")]
    [Tooltip("Enable per-joint angle limits (degrees).")]
    public bool useJointLimits = false;
    [Tooltip("Per-joint [min,max] limits in degrees relative to initial pose. Length must match joints.")]
    public Vector2[] jointLimitsDeg;
    [Tooltip("Enable smoothing of applied delta (0 = no smoothing). Higher => slower smoothing.")]
    public float smoothFactor = 12f; // units: larger = faster/stronger smoothing (used as lerp speed)
    [Tooltip("Clamp maximum delta applied per iteration (degrees). Helps prevent frame snaps.")]
    public float maxStepDegrees = 10f;

    // internal accumulators
    Quaternion[] initialLocalRot;
    float[] accumulatedAngleDeg; // angle around local axis relative to initial pose

    void Reset()
    {
        if (joints != null && (localAxes == null || localAxes.Length != joints.Length))
        {
            localAxes = new Vector3[joints.Length];
            for (int i = 0; i < localAxes.Length; i++) localAxes[i] = Vector3.up;
        }
    }

    void Start()
    {
        if (endEffector == null && joints != null && joints.Length > 0) endEffector = joints[joints.Length - 1];

        if (localAxes == null || localAxes.Length != (joints != null ? joints.Length : 0))
        {
            if (joints != null)
            {
                localAxes = new Vector3[joints.Length];
                for (int i = 0; i < localAxes.Length; i++) localAxes[i] = Vector3.up;
            }
        }

        // init accumulators for limits/smoothing
        if (joints != null)
        {
            int n = joints.Length;
            initialLocalRot = new Quaternion[n];
            accumulatedAngleDeg = new float[n];
            for (int i = 0; i < n; i++)
            {
                initialLocalRot[i] = joints[i] != null ? joints[i].localRotation : Quaternion.identity;
                accumulatedAngleDeg[i] = 0f;
            }

            // ensure jointLimitsDeg length matches if enabled
            if (useJointLimits && (jointLimitsDeg == null || jointLimitsDeg.Length != n))
            {
                jointLimitsDeg = new Vector2[n];
                for (int i = 0; i < n; i++) jointLimitsDeg[i] = new Vector2(-180f, 180f);
            }
        }

        // run initial solve after one frame (lets all transforms settle)
        StartCoroutine(DelayedInitialSolve());
    }

    System.Collections.IEnumerator DelayedInitialSolve()
    {
        yield return null; // wait 1 frame

        // save old settings
        float oldStep = step;
        int oldMax = maxIterations;
        float oldDamping = damping;

        // soft initial solve
        step = Mathf.Max(0.05f, step * 0.2f);
        maxIterations = Mathf.Clamp(2, 1, 10);
        damping = Mathf.Max(0.05f, damping * 3f);
        SolveIK();

        // restore
        step = oldStep;
        maxIterations = oldMax;
        damping = oldDamping;
    }

    void Update()
    {
        if (solveEveryFrame) SolveIK();
    }

    [ContextMenu("Solve Once")]
    public void SolveOnce()
    {
        bool old = solveEveryFrame;
        solveEveryFrame = false;
        SolveIK();
        solveEveryFrame = old;
    }

    public void SolveIK()
    {
        if (target == null || joints == null || joints.Length == 0) return;
        Transform eff = endEffector != null ? endEffector : joints[joints.Length - 1];

        for (int iter = 0; iter < maxIterations; iter++)
        {
            Vector3 pEnd = eff.position;
            Vector3 e = target.position - pEnd;
            if (e.magnitude < positionThreshold) break;

            int n = joints.Length;
            // J is 3 x n
            float[,] J = new float[3, n];
            for (int i = 0; i < n; i++)
            {
                Transform joint = joints[i];
                if (joint == null) continue;
                Vector3 axisLocal = (localAxes != null && i < localAxes.Length && localAxes[i] != Vector3.zero) ? localAxes[i].normalized : Vector3.up;
                Vector3 axisWorld = joint.TransformDirection(axisLocal); // axis in world
                Vector3 r = pEnd - joint.position;
                Vector3 col = Vector3.Cross(axisWorld, r); // linear Jacobian column
                J[0, i] = col.x;
                J[1, i] = col.y;
                J[2, i] = col.z;
            }

            // J * J^T (3x3)
            float[,] JJT = Multiply(J, Transpose(J));

            // add damping (lambda^2 * I)
            float lambda2 = damping * damping;
            JJT[0, 0] += lambda2;
            JJT[1, 1] += lambda2;
            JJT[2, 2] += lambda2;

            float[,] invJJT = Inverse3x3(JJT);
            if (invJJT == null)
            {
                Debug.LogWarning("IK: JJT singular (unable to invert). Increasing damping might help.");
                break;
            }

            float[] eVec = new float[3] { e.x, e.y, e.z };
            float[] K = Multiply(invJJT, eVec); // 3x1

            // deltaTheta = J^T * K (n x 1)
            float[,] JT = Transpose(J); // n x 3
            float[] deltaTheta = new float[n];
            for (int i = 0; i < n; i++)
            {
                deltaTheta[i] = JT[i, 0] * K[0] + JT[i, 1] * K[1] + JT[i, 2] * K[2];
            }

            // apply delta angles (rotate joint around its local axis) with smoothing & limits
            for (int i = 0; i < n; i++)
            {
                if (joints[i] == null) continue;
                float angleRad = deltaTheta[i] * step;
                float angleDeg = angleRad * Mathf.Rad2Deg;

                // clamp the per-iteration delta to avoid huge jumps
                angleDeg = Mathf.Clamp(angleDeg, -Mathf.Abs(maxStepDegrees), Mathf.Abs(maxStepDegrees));

                // compute how much we should actually apply this frame (smoothing)
                float applyDeg = angleDeg;
                if (smoothFactor > 0f)
                {
                    // convert smoothFactor to interpolation factor per-frame (frame-rate independent)
                    float t = 1f - Mathf.Exp(-smoothFactor * Time.deltaTime); // critically damped style lerp factor
                    applyDeg = Mathf.Lerp(0f, angleDeg, t);
                }

                // compute tentative new accumulated angle
                float newAccum = accumulatedAngleDeg[i] + applyDeg;

                // if limits enabled, clamp the accumulated angle and adjust applyDeg accordingly
                if (useJointLimits && jointLimitsDeg != null && i < jointLimitsDeg.Length)
                {
                    float minA = jointLimitsDeg[i].x;
                    float maxA = jointLimitsDeg[i].y;
                    float clampedAccum = Mathf.Clamp(newAccum, minA, maxA);
                    float allowedApply = clampedAccum - accumulatedAngleDeg[i];
                    applyDeg = allowedApply;
                    newAccum = accumulatedAngleDeg[i] + applyDeg; // equals clampedAccum
                }

                // finally apply rotation around the joint's local axis in local space
                Vector3 axisLocal = (localAxes != null && i < localAxes.Length && localAxes[i] != Vector3.zero) ? localAxes[i].normalized : Vector3.up;
                joints[i].Rotate(axisLocal, applyDeg, Space.Self);

                // update accumulator
                accumulatedAngleDeg[i] = newAccum;
            }
        }
    }

    #region Matrix helpers
    static float[,] Transpose(float[,] A)
    {
        int r = A.GetLength(0), c = A.GetLength(1);
        float[,] T = new float[c, r];
        for (int i = 0; i < r; i++) for (int j = 0; j < c; j++) T[j, i] = A[i, j];
        return T;
    }

    static float[,] Multiply(float[,] A, float[,] B)
    {
        int rA = A.GetLength(0), cA = A.GetLength(1);
        int rB = B.GetLength(0), cB = B.GetLength(1);
        if (cA != rB) throw new System.Exception("Matrix dim mismatch");
        float[,] C = new float[rA, cB];
        for (int i = 0; i < rA; i++) for (int j = 0; j < cB; j++)
            {
                float s = 0f;
                for (int k = 0; k < cA; k++) s += A[i, k] * B[k, j];
                C[i, j] = s;
            }
        return C;
    }

    static float[] Multiply(float[,] A, float[] v)
    {
        int r = A.GetLength(0), c = A.GetLength(1);
        if (c != v.Length) throw new System.Exception("Matrix-vector dim mismatch");
        float[] outv = new float[r];
        for (int i = 0; i < r; i++)
        {
            float s = 0f;
            for (int j = 0; j < c; j++) s += A[i, j] * v[j];
            outv[i] = s;
        }
        return outv;
    }

    static float[,] Inverse3x3(float[,] m)
    {
        float a = m[0, 0], b = m[0, 1], c = m[0, 2];
        float d = m[1, 0], e = m[1, 1], f = m[1, 2];
        float g = m[2, 0], h = m[2, 1], i = m[2, 2];
        float det = a * (e * i - f * h) - b * (d * i - f * g) + c * (d * h - e * g);
        if (Mathf.Abs(det) < 1e-9f) return null;
        float invDet = 1.0f / det;
        float[,] inv = new float[3, 3];
        inv[0, 0] = (e * i - f * h) * invDet;
        inv[0, 1] = (c * h - b * i) * invDet;
        inv[0, 2] = (b * f - c * e) * invDet;
        inv[1, 0] = (f * g - d * i) * invDet;
        inv[1, 1] = (a * i - c * g) * invDet;
        inv[1, 2] = (c * d - a * f) * invDet;
        inv[2, 0] = (d * h - e * g) * invDet;
        inv[2, 1] = (b * g - a * h) * invDet;
        inv[2, 2] = (a * e - b * d) * invDet;
        return inv;
    }
    #endregion

    void OnDrawGizmos()
    {
        if (target != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(target.position, 0.05f);
        }

        Transform eff = endEffector != null ? endEffector : (joints != null && joints.Length > 0 ? joints[joints.Length - 1] : null);
        if (eff != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(eff.position, 0.03f);
        }

        if (joints != null)
        {
            for (int i = 0; i < joints.Length; i++)
            {
                if (joints[i] == null) continue;
                Gizmos.color = Color.yellow;
                Vector3 axis = (localAxes != null && i < localAxes.Length) ? localAxes[i].normalized : Vector3.up;
                Gizmos.DrawLine(joints[i].position, joints[i].position + joints[i].TransformDirection(axis) * 0.1f);
            }
        }
    }
}

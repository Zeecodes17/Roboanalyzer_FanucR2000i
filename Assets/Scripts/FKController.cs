using UnityEngine;
using System;
using System.Text;

namespace FanucR2000iA
{
    public enum Axis { X, Y, Z }

    [Serializable]
    public class RevoluteJoint
    {
        public string name = "J";
        public Transform transform;

        public Axis axis = Axis.Z;
        public bool invert = false;

  
        [Range(-360f, 360f)] public float thetaDeg = 0f;

        public bool useLimits = true;
        public float minDeg = -180f;
        public float maxDeg = 180f;

        [HideInInspector] public Quaternion initialLocalRotation;

        public float GetClampedAngle()
        {
            if (!useLimits) return thetaDeg;
            if (minDeg > maxDeg) { var t = minDeg; minDeg = maxDeg; maxDeg = t; } 
            return Mathf.Clamp(thetaDeg, minDeg, maxDeg);
        }
    }


    
    [ExecuteAlways]
    public class FKController : MonoBehaviour
    {
        [Header("Chain")]
        public Transform baseLink;           
        public Transform endEffector;        
        public RevoluteJoint[] joints = new RevoluteJoint[6];

        [Header("Behaviour")]
        public bool printHTMOnChange = true; 
        public bool applyInEditMode = true;  

        float[] _lastAngles;

        void Reset()
        {
            if (joints == null || joints.Length == 0) joints = new RevoluteJoint[6];
        }

        void OnEnable()
        {
            CaptureInitialPose();
            EnsureLastAngles();
            ApplyAll();
        }

        void OnValidate()
        {
            if (!applyInEditMode) return;
            ApplyAll();
            MaybePrintHTM();
        }

        void Update()
        {
            ApplyAll();
            MaybePrintHTM();
        }

        [ContextMenu("Capture Initial Pose")]
        public void CaptureInitialPose()
        {
            if (joints == null) return;
            foreach (var j in joints)
                if (j != null && j.transform != null)
                    j.initialLocalRotation = j.transform.localRotation;
        }

        void EnsureLastAngles()
        {
            if (joints == null) return;
            if (_lastAngles == null || _lastAngles.Length != joints.Length)
                _lastAngles = new float[joints.Length];
            for (int i = 0; i < joints.Length; i++)
                _lastAngles[i] = joints[i]?.thetaDeg ?? 0f;
        }

        void ApplyAll()
        {
            if (joints == null) return;
            for (int i = 0; i < joints.Length; i++)
            {
                var j = joints[i];
                if (j == null || j.transform == null) continue;

                Vector3 localAxis = AxisToVector(j.axis);
                float clamped = j.GetClampedAngle();
                float angle = j.invert ? -clamped : clamped;

                if (j.initialLocalRotation == Quaternion.identity)
                    j.initialLocalRotation = j.transform.localRotation;

                j.transform.localRotation = j.initialLocalRotation * Quaternion.AngleAxis(angle, localAxis);
            }
        }

        void MaybePrintHTM()
        {
            if (!printHTMOnChange) return;
            if (joints == null) { return; }

            bool changed = false;
            if (_lastAngles == null || _lastAngles.Length != joints.Length) EnsureLastAngles();
            for (int i = 0; i < joints.Length; i++)
            {
                float a = joints[i]?.thetaDeg ?? 0f;
                if (!Mathf.Approximately(a, _lastAngles[i])) { changed = true; break; }
            }
            if (!changed) return;

            for (int i = 0; i < joints.Length; i++) _lastAngles[i] = joints[i]?.thetaDeg ?? 0f;

            var T = GetBaseToEETransform();
            for (int i = 0; i < joints.Length; i++)
            {
                var j = joints[i];
                if (j == null || !j.useLimits) continue;

                float clamped = j.GetClampedAngle();
                if (!Mathf.Approximately(clamped, j.thetaDeg))
                {
                    string edge = Mathf.Approximately(clamped, j.minDeg) ? "MIN" :
                                  Mathf.Approximately(clamped, j.maxDeg) ? "MAX" : "RANGE";
                    Debug.LogWarning($"[Joint Limit] {j.name}: {j.thetaDeg:F1}° → {clamped:F1}° ({edge} {j.minDeg:F1}..{j.maxDeg:F1})");
                }
            }
            Debug.Log(FormatMatrixReport(T, baseLink, endEffector));
        }

        public Matrix4x4 GetBaseToEETransform()
        {
            if (baseLink == null || endEffector == null)
            {
                Debug.LogWarning("Assign baseLink and endEffector.");
                return Matrix4x4.identity;
            }
            return baseLink.worldToLocalMatrix * endEffector.localToWorldMatrix;
        }

        static Vector3 AxisToVector(Axis a)
        {
            switch (a)
            {
                case Axis.X: return Vector3.right;
                case Axis.Y: return Vector3.up;
                default: return Vector3.forward; 
            }
        }

        static string FormatMatrixReport(Matrix4x4 T, Transform baseLink, Transform ee)
        {
            Vector3 p = new Vector3(T.m03, T.m13, T.m23);
            var fwd = new Vector3(T.m02, T.m12, T.m22);
            var up = new Vector3(T.m01, T.m11, T.m21);
            Quaternion q = Quaternion.LookRotation(fwd, up);
            Vector3 euler = q.eulerAngles;

            var sb = new StringBuilder();
            sb.AppendLine($"HTM  T_base^{ee.name}  (base='{baseLink.name}', ee='{ee.name}')");
            sb.AppendLine(Row(T, 0));
            sb.AppendLine(Row(T, 1));
            sb.AppendLine(Row(T, 2));
            sb.AppendLine(Row(T, 3));
            sb.AppendLine($"Position (m): [{p.x:F4}, {p.y:F4}, {p.z:F4}]");
            sb.AppendLine($"Euler ZYX (deg, approx): [{euler.z:F3}, {euler.y:F3}, {euler.x:F3}]");
            sb.AppendLine($"Quaternion (x,y,z,w): [{q.x:F5}, {q.y:F5}, {q.z:F5}, {q.w:F5}]");
            return sb.ToString();
        }
        static string Row(Matrix4x4 M, int r)
        {
            return $"[{M[r, 0],8:F5} {M[r, 1],8:F5} {M[r, 2],8:F5} {M[r, 3],8:F5}]";
        }

        // ---- New helper property ----
        public float[] JointAngles
        {
            get
            {
                if (joints == null) return new float[0];
                float[] arr = new float[joints.Length];
                for (int i = 0; i < joints.Length; i++)
                    arr[i] = joints[i] != null ? joints[i].thetaDeg : 0f;
                return arr;
            }
        }
    }
}

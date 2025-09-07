using UnityEngine;
using TMPro;
using System;
using System.IO;
using System.Linq;

namespace FanucR2000iA
{
    [ExecuteAlways]
    public class FKUIPanel : MonoBehaviour
    {
        [Header("FK source")]
        public FKController fk; // assign the FKController component here

        [Header("HUD text fields (TextMeshPro)")]
        public TextMeshProUGUI xText;
        public TextMeshProUGUI yText;
        public TextMeshProUGUI zText;
        public TextMeshProUGUI aText;
        public TextMeshProUGUI bText;
        public TextMeshProUGUI cText;

        [Header("Matrix text (TextMeshPro)")]
        public TextMeshProUGUI matrixText; // the multi-line matrix field

        [Header("Optional panels (for enabling/disabling)")]
        public GameObject hudPanel;    // optional reference to the HUD panel GameObject
        public GameObject matrixPanel; // optional reference to the Matrix panel GameObject

        [Header("Display settings")]
        public int posDecimals = 0;   // decimals for position (mm)
        public int rotDecimals = 3;   // decimals for A/B/C (deg)
        public bool showUnits = true; // append " mm" / "°" (toggle if you prefer clean labels)

        void Update()
        {
            if (fk == null || fk.baseLink == null || fk.endEffector == null) return;

            Matrix4x4 T = fk.GetBaseToEETransform();

            // Position in meters -> mm
            float px = T.m03 * 1000f;
            float py = T.m13 * 1000f;
            float pz = T.m23 * 1000f;

            Vector3 abc = EulerXYZ_FromRotation(T);

            // format strings
            string posFmt = "F" + Mathf.Max(0, posDecimals).ToString();
            string rotFmt = "F" + Mathf.Max(0, rotDecimals).ToString();

            string xStr = px.ToString(posFmt);
            string yStr = py.ToString(posFmt);
            string zStr = pz.ToString(posFmt);

            string aStr = abc.x.ToString(rotFmt);
            string bStr = abc.y.ToString(rotFmt);
            string cStr = abc.z.ToString(rotFmt);

            if (showUnits)
            {
                xStr += " mm";
                yStr += " mm";
                zStr += " mm";
                aStr += "°";
                bStr += "°";
                cStr += "°";
            }

            if (xText != null) xText.text = $"X: {xStr}";
            if (yText != null) yText.text = $"Y: {yStr}";
            if (zText != null) zText.text = $"Z: {zStr}";
            if (aText != null) aText.text = $"A: {aStr}";
            if (bText != null) bText.text = $"B: {bStr}";
            if (cText != null) cText.text = $"C: {cStr}";

            if (matrixText != null)
            {
                // build fixed-width rows for monospace font
                string r0 = Row(T.m00, T.m01, T.m02, T.m03);
                string r1 = Row(T.m10, T.m11, T.m12, T.m13);
                string r2 = Row(T.m20, T.m21, T.m22, T.m23);
                string r3 = Row(T.m30, T.m31, T.m32, T.m33); // use actual bottom row
                matrixText.text = r0 + "\n" + r1 + "\n" + r2 + "\n" + r3;
            }
        }

        // format helper: each number occupies the same width (works best with a monospace font)
        string Row(float a, float b, float c, float d)
        {
            // width 9 with 4 decimals tends to be safe; increase width if values become large.
            return $"[{a,9:F4} {b,9:F4} {c,9:F4} {d,9:F4}]";
        }

        // Recover extrinsic XYZ euler (A around X, then B around Y, then C around Z)
        static Vector3 EulerXYZ_FromRotation(Matrix4x4 R)
        {
            float r00 = R.m00, r01 = R.m01, r02 = R.m02;
            float r10 = R.m10, r11 = R.m11, r12 = R.m12;
            float r20 = R.m20, r21 = R.m21, r22 = R.m22;

            float B = Mathf.Asin(Mathf.Clamp(r02, -1f, 1f));
            float cosB = Mathf.Cos(B);
            float A, C;
            if (Mathf.Abs(cosB) > 1e-6f)
            {
                A = Mathf.Atan2(-r12, r22);
                C = Mathf.Atan2(-r01, r00);
            }
            else
            {
                A = 0f;
                C = Mathf.Atan2(r10, r11);
            }
            return new Vector3(A * Mathf.Rad2Deg, B * Mathf.Rad2Deg, C * Mathf.Rad2Deg);
        }

        // convenience methods to toggle panels from code or other UI
        public void ShowHUD(bool on)
        {
            if (hudPanel != null) hudPanel.SetActive(on);
        }

        public void ShowMatrix(bool on)
        {
            if (matrixPanel != null) matrixPanel.SetActive(on);
        }

        // ------- JSON dump helper (safe) -------
        [Serializable]
        public class JudgePose
        {
            public string timestamp;
            public float[] position_mm; // [x,y,z]
            public float[] abc_deg;     // [A,B,C]
            public float[] jointAngles; // copy from fk
        }

        // Call this from a UI Button to save a JSON snapshot of the current pose.
        [ContextMenu("Dump Pose As JSON")]
        public void DumpPoseAsJson()
        {
            if (fk == null || fk.baseLink == null || fk.endEffector == null)
            {
                Debug.LogWarning("FKUIPanel.DumpPoseAsJson: fk or its links are null, can't dump pose.");
                return;
            }

            Matrix4x4 T = fk.GetBaseToEETransform();
            Vector3 posmm = new Vector3(T.m03, T.m13, T.m23) * 1000f;
            Vector3 abc = EulerXYZ_FromRotation(T);

            JudgePose p = new JudgePose();
            p.timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            p.position_mm = new float[] { posmm.x, posmm.y, posmm.z };
            p.abc_deg = new float[] { abc.x, abc.y, abc.z };
            p.jointAngles = fk.JointAngles;

            string json = JsonUtility.ToJson(p, true);
            string filename = $"JUDGE_POSE_{p.timestamp}.json";
            string path = Path.Combine(Application.persistentDataPath, filename);

            try
            {
                File.WriteAllText(path, json);
                Debug.Log($"Saved pose JSON to: {path}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save pose JSON: {ex.Message}");
            }
        }
    }
}
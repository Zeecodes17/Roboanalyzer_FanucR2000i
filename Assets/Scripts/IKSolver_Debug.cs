// IKSolver_Debug.cs
using UnityEngine;

public class IKSolver_Debug : MonoBehaviour
{
    public IKSolver_AnalyticalDLS ik;

    [ContextMenu("Print IK Debug")]
    public void PrintDebug()
    {
        if (ik == null)
        {
            Debug.LogError("IKSolver_Debug: assign the IKSolver_AnalyticalDLS component to 'ik' field.");
            return;
        }

        if (ik.target == null || ik.joints == null || ik.joints.Length == 0)
        {
            Debug.LogError("IKSolver_Debug: missing target or joints.");
            return;
        }

        Transform eff = ik.endEffector != null ? ik.endEffector : ik.joints[ik.joints.Length - 1];
        Vector3 pEnd = eff.position;
        Vector3 pTarget = ik.target.position;
        Vector3 e = pTarget - pEnd;
        Debug.Log($"IK DEBUG: target={pTarget:F4}, effector={pEnd:F4}, errorMag={e.magnitude:F6}");

        for (int i = 0; i < ik.joints.Length; i++)
        {
            Transform joint = ik.joints[i];
            Vector3 localAxis = (ik.localAxes != null && i < ik.localAxes.Length && ik.localAxes[i] != Vector3.zero)
                ? ik.localAxes[i].normalized : Vector3.up;
            Vector3 axisWorld = joint.TransformDirection(localAxis);
            Vector3 r = pEnd - joint.position;
            Vector3 col = Vector3.Cross(axisWorld, r);
            Debug.Log($"J{i} '{joint.name}': jointPos={joint.position:F4}, axisLocal={localAxis}, axisWorld={axisWorld:F6}, r={r:F6}, Jcol={col:F6}");
        }
    }
}
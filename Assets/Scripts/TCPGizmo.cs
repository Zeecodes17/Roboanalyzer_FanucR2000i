using UnityEngine;

[ExecuteAlways]
public class TCPGizmo : MonoBehaviour
{
    public Transform tcp;            // assign TCP (end effector)
    public float axisLength = 0.15f; // meters
    public float headSize = 0.02f;

    void OnDrawGizmos()
    {
        if (tcp == null) return;

        // Save and use local-to-world transform of TCP
        Vector3 p = tcp.position;
        Vector3 x = tcp.right.normalized * axisLength;
        Vector3 y = tcp.up.normalized * axisLength;
        Vector3 z = tcp.forward.normalized * axisLength;

        // X - red
        Gizmos.color = Color.red;
        Gizmos.DrawLine(p, p + x);
        DrawArrowHead(p + x, x.normalized, headSize);

        // Y - green
        Gizmos.color = Color.green;
        Gizmos.DrawLine(p, p + y);
        DrawArrowHead(p + y, y.normalized, headSize);

        // Z - blue
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(p, p + z);
        DrawArrowHead(p + z, z.normalized, headSize);

        // small sphere at TCP
        Gizmos.color = Color.white;
        Gizmos.DrawSphere(p, headSize * 0.5f);
    }

    void DrawArrowHead(Vector3 pos, Vector3 dir, float size)
    {
        // two short lines for head
        Vector3 a = Quaternion.AngleAxis(20, Vector3.up) * -dir * size * 0.6f;
        Vector3 b = Quaternion.AngleAxis(-20, Vector3.up) * -dir * size * 0.6f;
        Gizmos.DrawLine(pos, pos + a);
        Gizmos.DrawLine(pos, pos + b);
    }
}
using UnityEngine;

public class RobotFollowController : MonoBehaviour
{
    [Header("References")]
    public Transform solverTarget;       // assign IK_Target
    public Transform restPoseTransform;  // assign IK_RestPose
    public Transform effectorTransform;  // end-effector (for debug line)

    [Header("Motion")]
    public float followSpeed = 8f;       // higher = snappier
    public float returnSpeed = 5f;
    public bool debugLine = true;

    public float maxReach = 0f; // <=0 disables clamp

    Transform followingBall;
    LineRenderer lr;

    void Start()
    {
        if (debugLine)
        {
            lr = gameObject.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = 0.01f;
            lr.endWidth = 0.01f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        if (solverTarget == null) Debug.LogError("RobotFollowController: assign solverTarget (IK target Transform).");
        if (restPoseTransform == null) Debug.LogWarning("RobotFollowController: restPoseTransform not assigned.");
    }

    void LateUpdate()
    {
        if (solverTarget == null) return;

        if (followingBall != null)
        {
            Vector3 desired = followingBall.position;
            Quaternion desiredRot = followingBall.rotation;

            if (maxReach > 0f && effectorTransform != null)
            {
                float dist = Vector3.Distance(effectorTransform.position, desired);
                if (dist > maxReach)
                    desired = effectorTransform.position + (desired - effectorTransform.position).normalized * maxReach;
            }

            solverTarget.position = Vector3.Lerp(solverTarget.position, desired, 1f - Mathf.Exp(-followSpeed * Time.deltaTime));
            solverTarget.rotation = Quaternion.Slerp(solverTarget.rotation, desiredRot, 1f - Mathf.Exp(-followSpeed * Time.deltaTime));
        }
        else if (restPoseTransform != null)
        {
            solverTarget.position = Vector3.Lerp(solverTarget.position, restPoseTransform.position, 1f - Mathf.Exp(-returnSpeed * Time.deltaTime));
            solverTarget.rotation = Quaternion.Slerp(solverTarget.rotation, restPoseTransform.rotation, 1f - Mathf.Exp(-returnSpeed * Time.deltaTime));
        }

        if (debugLine && lr != null && effectorTransform != null)
        {
            lr.SetPosition(0, effectorTransform.position);
            lr.SetPosition(1, solverTarget.position);
        }
    }

    public void StartFollowing(Transform ball)
    {
        followingBall = ball;
        Debug.Log($"RobotFollowController: StartFollowing {ball.name}");
    }

    public void StopFollowing()
    {
        Debug.Log("RobotFollowController: StopFollowing");
        followingBall = null;
    }
}

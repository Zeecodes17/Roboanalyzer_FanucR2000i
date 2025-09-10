using UnityEngine;

public class FollowController : MonoBehaviour
{
    [Header("References")]
    public Transform solverTarget;       // IK_Target
    public Transform restPoseTransform;  // IK_RestPose
    public Transform effectorTransform;  // Effector_Marker

    [Header("Motion")]
    public float followSpeed = 8f;
    public float returnSpeed = 5f;
    [Range(0f, 20f)] public float weightChangeRate = 5f; // smooth blend

    [Header("Debug Visuals")]
    public bool debugLine = true;
    public Color lineColor = Color.cyan;
    public float lineWidth = 0.01f;
    public GameObject unreachableMarkerPrefab; // small red sphere prefab
    public float maxReach = 0f; // <=0 disables clamp

    private Transform followingBall;
    private LineRenderer lr;
    private float solverWeight = 0f;
    private GameObject unreachableMarker;

    void Start()
    {
        if (debugLine)
        {
            lr = gameObject.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.material = new Material(Shader.Find("Unlit/Color"));
            lr.material.SetColor("_Color", lineColor);
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.enabled = false; // start hidden
        }
        if (unreachableMarkerPrefab != null)
        {
            unreachableMarker = Instantiate(unreachableMarkerPrefab);
            unreachableMarker.SetActive(false);
        }
    }

    void LateUpdate()
    {
        if (solverTarget == null) return;

        // blend weight
        float targetWeight = (followingBall != null) ? 1f : 0f;
        solverWeight = Mathf.MoveTowards(solverWeight, targetWeight, weightChangeRate * Time.deltaTime);

        Vector3 desiredPos;
        Quaternion desiredRot;

        if (followingBall != null)
        {
            desiredPos = followingBall.position;
            desiredRot = followingBall.rotation;
        }
        else if (restPoseTransform != null)
        {
            desiredPos = restPoseTransform.position;
            desiredRot = restPoseTransform.rotation;
        }
        else
        {
            desiredPos = solverTarget.position;
            desiredRot = solverTarget.rotation;
        }

        // clamp reach
        bool clamped = false;
        Vector3 unclamped = desiredPos;
        if (maxReach > 0f && effectorTransform != null)
        {
            float dist = Vector3.Distance(effectorTransform.position, desiredPos);
            if (dist > maxReach)
            {
                desiredPos = effectorTransform.position +
                             (desiredPos - effectorTransform.position).normalized * maxReach;
                clamped = true;
            }
        }

        // smooth position/rotation update
        float spd = (followingBall != null) ? followSpeed : returnSpeed;
        solverTarget.position = Vector3.Lerp(solverTarget.position, desiredPos, 1f - Mathf.Exp(-spd * solverWeight * Time.deltaTime));
        solverTarget.rotation = Quaternion.Slerp(solverTarget.rotation, desiredRot, 1f - Mathf.Exp(-spd * solverWeight * Time.deltaTime));

        // visuals
        if (debugLine && lr != null && effectorTransform != null)
        {
            lr.enabled = (solverWeight > 0.01f);
            if (lr.enabled)
            {
                lr.SetPosition(0, effectorTransform.position);
                lr.SetPosition(1, solverTarget.position);
            }
        }

        if (unreachableMarker != null)
        {
            unreachableMarker.SetActive(clamped);
            if (clamped) unreachableMarker.transform.position = unclamped;
        }
    }

    public void StartFollowing(Transform ball)
    {
        followingBall = ball;
        if (lr != null) lr.enabled = true;
    }

    public void StopFollowing()
    {
        followingBall = null;
        if (lr != null) lr.enabled = false;
    }
}

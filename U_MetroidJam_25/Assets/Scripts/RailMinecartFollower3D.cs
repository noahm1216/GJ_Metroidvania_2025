using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

public class RailMinecartFollower3D : MonoBehaviour
{
    [Header("Forward Movement")]
    public float speed = 5f;
    public float slowFactor = .75f;
    private float currentSpeed;
    public Vector3 direction = Vector3.right;

    public GameObject sparks;

    [Header("Rail Follow (Y only while grounded)")]
    public SplineContainer rail;
    public float yOffset = 0f;
    public float yFollowLerp = 20f;
    public float zLock = 0f;

    [Header("Jump / Gravity")]
    public float jumpVelocity = 8f;
    public float gravity = 25f;
    public float reconnectDistance = 0.5f;
    public float minFallSpeedToReconnect = -0.1f;

    [Header("Landing Smoothing")]
    [Tooltip("Extra smoothing when reconnecting after a jump. Higher = snappier.")]
    public float landingLerp = 30f;

    [Header("Rotation (match spline angle)")]
    public Transform rotateTarget;   // drag your cart mesh / visuals here
    public float rotationLerp = 20f; // 0 = instant, higher = smoother

    [Header("Gap Info")]
    [Tooltip("Reference to the runtime rail generator so we know where the gaps are.")]
    public RuntimeRailSplineGenerator railGenerator;

    bool grounded = true;
    float verticalVelocity = 0f;
    bool justReconnected = false;

    void Start()
    {
        currentSpeed = speed;
    }

    void Update()
    {

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            currentSpeed = speed * slowFactor;
            sparks.SetActive(true);
        }

        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            currentSpeed = speed;
            sparks.SetActive(false);
        }
        // 1) Always move forward in X
        transform.position += direction.normalized * currentSpeed * Time.deltaTime;

        if (rail == null || rail.Splines.Count == 0)
            return;

        // 2) Nearest point on ANY spline in the container (smooth chunk transitions)
        float3 localPos = (float3)rail.transform.InverseTransformPoint(transform.position);

        float3 nearestWorld = (float3)transform.position;
        float nearestT = 0f;
        int nearestSplineIndex = -1;
        float nearestDistSq = float.MaxValue;

        var splines = rail.Splines;
        for (int i = 0; i < splines.Count; i++)
        {
            var spline = splines[i];

            SplineUtility.GetNearestPoint(
                spline,
                localPos,
                out float3 nearestLocal,
                out float t,
                resolution: 8,
                iterations: 3
            );

            float3 world = (float3)rail.transform.TransformPoint((Vector3)nearestLocal);
            float distSq = math.lengthsq(world - (float3)transform.position);

            if (distSq < nearestDistSq)
            {
                nearestDistSq = distSq;
                nearestWorld = world;
                nearestT = t;
                nearestSplineIndex = i;
            }
        }

        // 3) Check if our X is currently inside a gap (rail-independent of slope)
        bool inGap = false;
        if (railGenerator != null)
        {
            float x = transform.position.x;
            var gaps = railGenerator.Gaps;
            for (int i = 0; i < gaps.Count; i++)
            {
                Vector2 g = gaps[i];
                if (x >= g.x && x <= g.y)
                {
                    inGap = true;
                    break;
                }
            }
        }

        // ---------- Rotation (only while grounded) ----------
        if (rotateTarget != null && grounded)
        {
            Vector3 tangent = direction; // fallback

            if (nearestSplineIndex >= 0)
            {
                var spline = splines[nearestSplineIndex];

                // small delta to approximate derivative
                float dt = 0.001f;
                float t0 = Mathf.Clamp01(nearestT - dt);
                float t1 = Mathf.Clamp01(nearestT + dt);

                float3 p0Local = SplineUtility.EvaluatePosition(spline, t0);
                float3 p1Local = SplineUtility.EvaluatePosition(spline, t1);

                float3 p0World = (float3)rail.transform.TransformPoint((Vector3)p0Local);
                float3 p1World = (float3)rail.transform.TransformPoint((Vector3)p1Local);

                float3 tanW = p1World - p0World;
                tangent = ((Vector3)tanW).normalized;
            }

            float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
            Quaternion targetRot = Quaternion.Euler(0f, 0f, angle);

            if (rotationLerp <= 0f)
            {
                rotateTarget.rotation = targetRot;
            }
            else
            {
                rotateTarget.rotation = Quaternion.Lerp(
                    rotateTarget.rotation,
                    targetRot,
                    1f - Mathf.Exp(-rotationLerp * Time.deltaTime)
                );
            }
        }
        // -------------------------------------------------------

        // 4) Jump input (only while grounded)
        if (grounded && Input.GetButtonDown("Jump"))
        {
            grounded = false;
            verticalVelocity = jumpVelocity;
            justReconnected = false;
        }

        Vector3 pos = transform.position;
        float railY = nearestWorld.y + yOffset;

        if (grounded)
        {
            if (inGap)
            {
                // We just stepped into a gap: start falling IMMEDIATELY this frame
                grounded = false;
                justReconnected = false;

                // Start the fall (first gravity step)
                // If you want a tiny "coyote time", you could skip this first step.
                verticalVelocity -= gravity * Time.deltaTime;
                pos.y += verticalVelocity * Time.deltaTime;
            }
            else
            {
                // Grounded and on rail: follow spline Y (with landing smoothing if we just reconnected)
                float targetY = railY;
                float effectiveLerp = justReconnected ? landingLerp : yFollowLerp;

                if (effectiveLerp <= 0f)
                {
                    pos.y = targetY;
                    justReconnected = false;
                }
                else
                {
                    pos.y = Mathf.Lerp(
                        pos.y,
                        targetY,
                        1f - Mathf.Exp(-effectiveLerp * Time.deltaTime)
                    );

                    if (Mathf.Abs(pos.y - targetY) < 0.01f)
                    {
                        pos.y = targetY;
                        justReconnected = false;
                    }
                }
            }
        }

        // Airborne motion (either jumped, fell off a gap, or are mid-fall)
        if (!grounded)
        {
            verticalVelocity -= gravity * Time.deltaTime;
            pos.y += verticalVelocity * Time.deltaTime;

            // Try to reconnect if we're falling and close to rail (outside of gap)
            if (!inGap)
            {
                float distToRail = Mathf.Abs(pos.y - railY);
                if (verticalVelocity <= minFallSpeedToReconnect && distToRail <= reconnectDistance)
                {
                    grounded = true;
                    verticalVelocity = 0f;
                    justReconnected = true; // grounded branch will smooth us onto the rail
                }
            }
        }

        // keep 2.5D plane stable
        pos.z = zLock;

        transform.position = pos;
    }
}

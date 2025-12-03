using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

public class RailMinecartFollower3D : MonoBehaviour
{
    [Header("Forward Movement")]
    public float speed = 5f;
    public Vector3 direction = Vector3.right;

    [Header("Rail Follow (Y only while grounded)")]
    public SplineContainer rail;
    public float yOffset = 0f;
    public float yFollowLerp = 20f;
    public float zLock = 0f;

    [Header("Jump")]
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

    bool grounded = true;
    float verticalVelocity = 0f;

    // track when we just reconnected so we can soften that landing a bit
    bool justReconnected = false;

    void Update()
    {
        // 1) Always move forward
        transform.position += direction.normalized * speed * Time.deltaTime;

        if (rail == null || rail.Splines.Count == 0)
            return;

        // 2) Find nearest point on ANY spline in the container (smooth chunk transitions)
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

        // ---------- Rotation (still only while grounded) ----------
        if (rotateTarget != null && grounded)
        {
            // Approximate tangent along the spline near nearestT on the chosen spline
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

            // Slope angle in the X/Y plane
            float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;

            // Tilt in 2D plane (around Z)
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

        // 3) Handle jump input
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
            // 4a) Grounded: follow spline Y (with extra smoothing right after landing)
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

                // once very close, snap and clear the landing flag
                if (Mathf.Abs(pos.y - targetY) < 0.01f)
                {
                    pos.y = targetY;
                    justReconnected = false;
                }
            }
        }
        else
        {
            // 4b) Airborne: ballistic motion
            verticalVelocity -= gravity * Time.deltaTime;
            pos.y += verticalVelocity * Time.deltaTime;

            // 5) Reconnect when falling & close to rail
            float distToRail = Mathf.Abs(pos.y - railY);

            if (verticalVelocity <= minFallSpeedToReconnect && distToRail <= reconnectDistance)
            {
                grounded = true;
                verticalVelocity = 0f;

                // Don't hard snap; let grounded logic blend us down to the rail
                justReconnected = true;
            }
        }

        // keep 2.5D plane stable
        pos.z = zLock;

        transform.position = pos;
    }
}

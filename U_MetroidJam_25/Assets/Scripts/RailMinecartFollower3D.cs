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

    // ---------- NEW: Rotation fields ----------
    [Header("Rotation (match spline angle)")]
    public Transform rotateTarget;   // drag your cart mesh / visuals here
    public float rotationLerp = 20f; // 0 = instant, higher = smoother
    // ------------------------------------------

    bool grounded = true;
    float verticalVelocity = 0f;

    void Update()
    {
        // 1) Always move forward
        transform.position += direction.normalized * speed * Time.deltaTime;

        if (rail == null) return;

        // 2) Find nearest point on spline
        float3 localPos = (float3)rail.transform.InverseTransformPoint(transform.position);

        SplineUtility.GetNearestPoint(
            rail.Spline,
            localPos,
            out float3 nearestLocal,
            out float nearestT,
            resolution: 8,
            iterations: 3
        );

        float3 nearestWorld = rail.EvaluatePosition(nearestT);

        // ---------- NEW: Rotation code goes RIGHT HERE ----------
        if (rotateTarget != null && grounded)
        {
            // Tangent gives rail direction at nearestT
            float3 tangentW = rail.EvaluateTangent(nearestT);
            Vector3 tangent = ((Vector3)tangentW).normalized;

            // Slope angle in the X/Y plane
            float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;

            // Tilt in 2D plane (around Z)
            Quaternion targetRot = Quaternion.Euler(0f, 0f, angle);

            if (rotationLerp <= 0f)
                rotateTarget.rotation = targetRot;
            else
                rotateTarget.rotation = Quaternion.Lerp(
                    rotateTarget.rotation,
                    targetRot,
                    1f - Mathf.Exp(-rotationLerp * Time.deltaTime)
                );
        }
        // -------------------------------------------------------

        // 3) Handle jump input
        if (grounded && Input.GetButtonDown("Jump"))
        {
            grounded = false;
            verticalVelocity = jumpVelocity;
        }

        Vector3 pos = transform.position;

        if (grounded)
        {
            // 4a) Grounded: follow spline Y
            float targetY = nearestWorld.y + yOffset;

            if (yFollowLerp <= 0f)
                pos.y = targetY;
            else
                pos.y = Mathf.Lerp(pos.y, targetY, 1f - Mathf.Exp(-yFollowLerp * Time.deltaTime));
        }
        else
        {
            // Airborne: ballistic motion
            verticalVelocity -= gravity * Time.deltaTime;
            pos.y += verticalVelocity * Time.deltaTime;

            float railY = nearestWorld.y + yOffset;

            // ---- HARD FLOOR: never allow going below rail ----
            // If we've fallen to or through the rail, snap and re-ground.
            if (pos.y <= railY && verticalVelocity <= 0f)
            {
                grounded = true;
                verticalVelocity = 0f;
                pos.y = railY;
            }
            else
            {
                // (optional) your old soft reconnect can stay for "magnet" feel
                float distToRail = Mathf.Abs(pos.y - railY);
                if (verticalVelocity <= minFallSpeedToReconnect && distToRail <= reconnectDistance)
                {
                    grounded = true;
                    verticalVelocity = 0f;
                    pos.y = railY;
                }
            }
        }

        // keep 2.5D plane stable
        pos.z = zLock;

        transform.position = pos;
    }
}
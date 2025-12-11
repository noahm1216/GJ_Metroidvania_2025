using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowObjectPosition : MonoBehaviour
{

    public Transform target; // The object the camera will follow
    public Vector3 offset = new Vector3(0f, 2f, 0f); // Offset from the target
    public float smoothTime = 0.25f; // How quickly the camera reaches the target position

    private Vector3 currentVelocity; // Used by SmoothDamp to track velocity

    private void Start()
    {
        offset.z = transform.position.z;
    }


    private void LateUpdate()
    {
        if (target == null)
        {
            Debug.LogWarning("Camera follow target not assigned!");
            return;
        }

        // Calculate the desired position based on the target's position and the offset
        Vector3 desiredPosition = target.position + offset;

        // Smoothly move the camera towards the desired position
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref currentVelocity,
            smoothTime
        );
    }
}
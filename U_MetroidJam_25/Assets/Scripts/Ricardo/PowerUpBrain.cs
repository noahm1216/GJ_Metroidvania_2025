using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerUpBrain : MonoBehaviour
{
    private float timer_D = 0f;   // counter
    public float amplitude = 0.2f; // how far it moves up/down
    public float frequency = 0.05f; // speed of oscillation

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position; // remember starting position
    }

    void Update()
    {
        // increment timer
        timer_D++;

        // calculate offset
        float offsetY = -Mathf.Sin(timer_D * frequency) * amplitude;

        // apply to position
        transform.position = new Vector3(
            startPos.x,
            startPos.y + offsetY,
            startPos.z
        );
    }

}

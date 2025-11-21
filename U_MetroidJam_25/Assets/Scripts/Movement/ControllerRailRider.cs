using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;


[RequireComponent(typeof(Rigidbody))]
public class ControllerRailRider : MonoBehaviour
{
    public enum MOTIONSTATE { NotRiding, Riding }

    // designer controlled numbers
    public float speedHorizontal = 350;
    public float speedSprintMultiplier = 2;

    public int numberOfJump = 1;
    public float speedJumpPower = 500;
    public LayerMask jumpRestoreLayers;
    private bool facingRight;
    

    [Header("Rail")]
    public LayerMask railWayLayers;
    [SerializeField] Rail currentRailScript;
    public float railAcceleration = 2f;  // Optional acceleration along the rail
    [SerializeField] float grindSpeed;
    [SerializeField] float heightOffset;
    float timeForFullSpline;
    float elapsedTime;
    [SerializeField] float lerpSpeed = 10f;
    public MOTIONSTATE motionState;
    public Spline splineRiding;
    private Quaternion storedRotationBeforeRail;


    // input mappings
    private string inputAxisHori = "Horizontal",
        inputAxisVert = "Vertical",
        inputAxisSprint = "Sprint";
        //inputAxisAbility = "Jump";

    // movement calculation data
    private Vector3 movementInput;
    private Rigidbody rb3d;
    private float deadzone = 0.05f;

    private float sprintDecaySpeed = 1.0f;
    private float sprintAmount = 0;
    private bool burstSprint = false;

    private int jumpsRemaining = 1;
    private float jumpTimeStamp, jumpTimeWait = 0.475f;
    private bool canJump;
    private bool jumpNow;

    


    // Start is called before the first frame update
    void Start()
    {
        rb3d = GetComponent<Rigidbody>();
        jumpsRemaining = numberOfJump;
    }

    // Update is called once per frame
    void Update()
    {
        // movement input while not riding
        movementInput = new Vector3(Input.GetAxis(inputAxisHori) * (1 + sprintAmount) // X
            , Input.GetAxis(inputAxisVert), // Y
            0); // Z


        switch (motionState)
        {
            case MOTIONSTATE.NotRiding:
                // direction facing
                if (movementInput.x < 0 && !facingRight) { transform.Rotate(0, 180, 0); facingRight = true; }
                if(movementInput.x > 0 && facingRight) { transform.Rotate(0, -180, 0); facingRight = false; }
                // Sprint logic
                if (Mathf.Abs(Input.GetAxis(inputAxisSprint)) > deadzone)
                {
                    if (!burstSprint) { sprintAmount = speedSprintMultiplier; }

                    sprintAmount -= (Time.deltaTime * sprintDecaySpeed);

                    if (sprintAmount < speedSprintMultiplier * 0.25f) sprintAmount = speedSprintMultiplier * 0.5f;

                    burstSprint = true;
                }
                else
                {
                    burstSprint = false;
                    sprintAmount = 0;
                }
                break;

            case MOTIONSTATE.Riding:
                // Move along the rail
                if (splineRiding != null)
                {
                }
                break;

            default:
                break;
        }

        // Jump logic
        canJump = jumpsRemaining > 0 && Time.time > jumpTimeStamp + jumpTimeWait;
        if (movementInput.y > 0 && canJump)
        {
            jumpNow = true;
            jumpTimeStamp = Time.time;
            jumpsRemaining--;
        }
    }

    private void FixedUpdate()
    {
        movementInput *= Time.deltaTime; // Normalize input against framerate

        if (motionState == MOTIONSTATE.NotRiding)
        {
            rb3d.velocity = new Vector3(movementInput.x * speedHorizontal,  // X velocity
                rb3d.velocity.y,  // Keep Y velocity the same
                0);  // Z velocity
        }
        else
        {
            MovePlayerAlongRail();
        }

        if (jumpNow)
        {
            jumpNow = false;

            if (motionState == MOTIONSTATE.Riding)
                JumpOffRail();
            else
                JumpNormally();
        }

    }

    void MovePlayerAlongRail()
    {
        if (currentRailScript != null && motionState == MOTIONSTATE.Riding) //This is just some additional error checking.
        {
            //Calculate a 0 to 1 normalised time value which is the progress along the rail.
            //Elapsed time divided by the full time needed to traverse the spline will give you that value.
            float progress = elapsedTime / timeForFullSpline;

            //If progress is less than 0, the player's position is before the start of the rail.
            //If greater than 1, their position is after the end of the rail.
            //In either case, the player has finished their grind.
            if (progress < 0 || progress > 1)
            {
                ThrowOffRail();
                return;
            }
            //The rest of this code will not execute if the player is thrown off.

            //Next Time Normalised is the player's progress value for the next update.
            //This is used for calculating the player's rotation.
            //Depending on the direction of the player on the spline, it will either add or subtract time from the
            //current elapsed time.
            float nextTimeNormalised;
            if (currentRailScript.normalDir)
                nextTimeNormalised = (elapsedTime + Time.deltaTime) / timeForFullSpline;
            else
                nextTimeNormalised = (elapsedTime - Time.deltaTime) / timeForFullSpline;

            //Calculating the local positions of the player's current position and next position
            //using current progress and the progress for the next update.
            float3 pos, tangent, up;
            float3 nextPosfloat, nextTan, nextUp;
            SplineUtility.Evaluate(currentRailScript.railSpline.Spline, progress, out pos, out tangent, out up);
            SplineUtility.Evaluate(currentRailScript.railSpline.Spline, nextTimeNormalised, out nextPosfloat, out nextTan, out nextUp);

            //Converting the local positions into world positions.
            Vector3 worldPos = currentRailScript.LocalToWorldConversion(pos);
            Vector3 nextPos = currentRailScript.LocalToWorldConversion(nextPosfloat);

            //Setting the player's position and adding a height offset so that they're sitting on top of the rail
            //instead of being in the middle of it.
            transform.position = worldPos + (transform.up * heightOffset);
            #region old spline rotate with rail
            ////Lerping the player's current rotation to the direction of where they are to where they're going.
            //transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(nextPos - worldPos), lerpSpeed * Time.deltaTime);
            ////Lerping the player's up direction to match that of the rail, in relation to the player's current rotation.
            //transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.FromToRotation(transform.up, up) * transform.rotation, lerpSpeed * Time.deltaTime);
            #endregion
            // NEW – clean, stable, upright, no sideways tilt
            Vector3 forwardDir = (nextPos - worldPos).normalized;
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                Quaternion.LookRotation(forwardDir, Vector3.up),
                lerpSpeed * Time.deltaTime
            );


            //Finally incrementing or decrementing elapsed time for the next update based on direction.
            if (currentRailScript.normalDir)
                elapsedTime += Time.deltaTime;
            else
                elapsedTime -= Time.deltaTime;
        }
    }

    private void OnCollisionEnter(Collision col)
    {
        // Reset jumps if collision with appropriate layer
        if (((1 << col.gameObject.layer) & jumpRestoreLayers) != 0 && col.transform.position.y < transform.position.y)
        { jumpsRemaining = numberOfJump; }

        // Check if collided with rail
        if (((1 << col.gameObject.layer) & railWayLayers) != 0 && col.transform.position.y < transform.position.y)
        {
            print("hit RAIL");            
            motionState = MOTIONSTATE.Riding;
            storedRotationBeforeRail = transform.rotation;

            // Get SplineContainer and retrieve the spline
            SplineContainer splineContainer = col.transform.GetComponent<SplineContainer>();
            if (splineContainer)
            {
                currentRailScript = col.transform.GetComponent<Rail>();
                splineRiding = splineContainer.Spline;
                CalculateAndSetRailPosition();
                rb3d.isKinematic = true;
            }
        }
    }

    void CalculateAndSetRailPosition()
    {
        //Figure out the amount of time it would take for the player to cover the rail.
        timeForFullSpline = currentRailScript.totalSplineLength / grindSpeed;

        //This is going to be the world position of where the player is going to start on the rail.
        Vector3 splinePoint;

        //The 0 to 1 value of the player's position on the spline. We also get the world position of where that
        //point is.
        float normalisedTime = currentRailScript.CalculateTargetRailPoint(transform.position, out splinePoint);
        elapsedTime = timeForFullSpline * normalisedTime;
        //Multiply the full time for the spline by the normalised time to get elapsed time. This will be used in
        //the movement code.

        //Spline evaluate takes the 0 to 1 normalised time above, 
        //and uses it to give you a local position, a tangent (forward), and up
        float3 pos, forward, up;
        SplineUtility.Evaluate(currentRailScript.railSpline.Spline, normalisedTime, out pos, out forward, out up);
        //Calculate the direction the player is going down the rail
        currentRailScript.CalculateDirection(forward, transform.forward);
        //Set player's initial position on the rail before starting the movement code.
        transform.position = splinePoint + (transform.up * heightOffset);
    }


    private void ThrowOffRail()
    {
        //Set onRail to false, clear the rail script, and push the player off the rail.
        //It's a little sudden, there might be a better way of doing using coroutines and looping, but this will work.
        motionState = MOTIONSTATE.NotRiding;
        transform.rotation = storedRotationBeforeRail;
        currentRailScript = null;
        transform.position += transform.forward * 1;
        rb3d.isKinematic = false;
    }

    private void JumpOffRail()
    {
        // Immediately stop following spline
        motionState = MOTIONSTATE.NotRiding;
        currentRailScript = null;
        rb3d.isKinematic = false;

        // Restore rotation BEFORE moving
        transform.rotation = storedRotationBeforeRail;

        // Give a tiny lift to avoid re-colliding with the rail this frame
        transform.position += Vector3.up * 0.15f;

        // Apply jump impulse
        rb3d.AddForce(((Vector3.up) * Time.deltaTime * speedJumpPower - rb3d.velocity), ForceMode.VelocityChange);
    }

    private void JumpNormally()
    {
        rb3d.AddForce(((Vector3.up) * Time.deltaTime * speedJumpPower - rb3d.velocity), ForceMode.VelocityChange);
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ControllerRailRider : MonoBehaviour
{
    public enum MOTIONSTATE {NotRiding, Riding }

    // designer controlled numbers
    public float speedHorizontal = 350;
    public float speedSprintMultiplier = 2;

    public int numberOfJump = 1;
    public float speedJumpPower = 500;
    public LayerMask jumpRestoreLayers;
    public LayerMask railWayLayers;

    public float speedRailRide = 500;

    // input mappings
    private string inputAxisHori = "Horizontal",
        inputAxisVert = "Vertical",
        inputAxisSprint = "Sprint",
        inputAxisAbility = "Jump";

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

    private MOTIONSTATE motionState;
    public Vector3 dirEnteredRail;

    // Start is called before the first frame update
    void Start()
    {
        rb3d = GetComponent<Rigidbody>();
        jumpsRemaining = numberOfJump;
    }

    // Update is called once per frame
    void Update()
    {
        // move
        movementInput = new Vector3(Input.GetAxis(inputAxisHori) * (1 + sprintAmount) // X
            , Input.GetAxis(inputAxisVert), // Y
            0); // Z


        switch (motionState)
        {
            case MOTIONSTATE.NotRiding:           
                // sprint
                if (Mathf.Abs(Input.GetAxis(inputAxisSprint)) > deadzone)
                {
                    print($"pressing {inputAxisSprint}");
                    if (!burstSprint) { sprintAmount = speedSprintMultiplier; } // first sprint from not sprinting

                    sprintAmount -= (Time.deltaTime * sprintDecaySpeed);

                    if (sprintAmount < speedSprintMultiplier * 0.25f) sprintAmount = speedSprintMultiplier * 0.5f;

                    burstSprint = true;
                }
                else
                { burstSprint = false; sprintAmount = 0; }               
                break;
            case MOTIONSTATE.Riding:
                movementInput = dirEnteredRail;
                break;
            default:
                break;
        }

        // jump        
        canJump = jumpsRemaining > 0 & Time.time > jumpTimeStamp + jumpTimeWait;
        if (movementInput.y > 0 && canJump) { jumpNow = true; jumpTimeStamp = Time.time; jumpsRemaining--; }

    }


    private void FixedUpdate()
    {
        movementInput *= Time.deltaTime; // normalize against framerate
        if (motionState == MOTIONSTATE.NotRiding)
        {
            rb3d.velocity = new Vector3(movementInput.x * speedHorizontal, // X
                rb3d.velocity.y, // Y
                0); // Z
        }
        else
        {
            rb3d.velocity = new Vector3(movementInput.x * speedRailRide, // X
                rb3d.velocity.y, // Y
                0); // Z
        }

        if (jumpNow)
        {
            jumpNow = false;
            rb3d.AddForce(((Vector3.up) * Time.deltaTime * speedJumpPower - rb3d.velocity), ForceMode.VelocityChange);
            if (motionState == MOTIONSTATE.Riding) motionState = MOTIONSTATE.NotRiding;
        }
    }

    private void OnCollisionEnter(Collision col)
    {
        // Check if the otherLayer
        if (((1 << col.gameObject.layer) & jumpRestoreLayers) != 0 && col.transform.position.y < transform.position.y)
        { jumpsRemaining = numberOfJump; } // print("reset jumps"); 

        if (((1 << col.gameObject.layer) & railWayLayers) != 0 && col.transform.position.y < transform.position.y)
        { print("hit RAIL"); dirEnteredRail = col.transform.position - transform.position; motionState = MOTIONSTATE.Riding; }
    }
}



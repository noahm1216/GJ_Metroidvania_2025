using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ControllerTwoPointFiveD : MonoBehaviour
{
    // designer controlled numbers
    public float speedHorizontal = 350;    
    public float speedSprintMultiplier = 2;

    public int numberOfJump = 1;
    public float speedJumpPower = 500;
    public LayerMask jumpRestoreLayers;

    public SummonData[] summonRb3ds;

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

        // jump        
        canJump = jumpsRemaining > 0 & Time.time > jumpTimeStamp + jumpTimeWait;
        if (movementInput.y > 0 && canJump) { jumpNow = true; jumpTimeStamp = Time.time; jumpsRemaining--; }

        // summon abilities
        if (Input.GetAxis(inputAxisAbility) > deadzone)
        {
            if (summonRb3ds.Length > 0)
            {
                for (int i = 0; i < summonRb3ds.Length; i++)
                {
                    if (summonRb3ds[i].gameObject.activeSelf == true)
                        summonRb3ds[i].ActivateAbility();
                }
            }
        }
    }


    private void FixedUpdate()
    {
        movementInput *= Time.deltaTime; // normalize against framerate
        rb3d.velocity = new Vector3(movementInput.x * speedHorizontal, // X
            rb3d.velocity.y, // Y
            0); // Z

        // SUMMONS
        if (summonRb3ds.Length > 0)
        {
            for (int i = 0; i < summonRb3ds.Length; i++)
            {
                if (summonRb3ds[i].rb3d)
                {
                    if (summonRb3ds[i].rb3d.isKinematic)
                        summonRb3ds[i].transform.Translate(movementInput * speedHorizontal * Time.deltaTime);
                    else
                        summonRb3ds[i].rb3d.velocity = new Vector3(movementInput.x * speedHorizontal, // X
                    summonRb3ds[i].rb3d.velocity.y, // Y
                    0); // Z
                }
            }
        }

        if (jumpNow)
        {
            jumpNow = false;
            rb3d.AddForce(((Vector3.up) * Time.deltaTime * speedJumpPower - rb3d.velocity), ForceMode.VelocityChange);

            // SUMMONS
            if(summonRb3ds.Length > 0)
                for(int i =0; i < summonRb3ds.Length; i++)
                    if (summonRb3ds[i].rb3d)
                        summonRb3ds[i].rb3d.AddForce(((summonRb3ds[i].transform.up) * Time.deltaTime * speedJumpPower - summonRb3ds[i].rb3d.velocity), ForceMode.VelocityChange);
        }        
    }

    private void OnCollisionEnter(Collision col)
    {
        // Check if the otherLayer
        if (((1 << col.gameObject.layer) & jumpRestoreLayers) != 0 && col.transform.position.y < transform.position.y)
        { print("reset jumps"); jumpsRemaining = numberOfJump; }
    }
}

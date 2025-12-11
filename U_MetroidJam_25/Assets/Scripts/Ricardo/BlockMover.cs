using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BlockMover : MonoBehaviour
{

    public InputActionAsset InputActions;
    private InputAction moveAction;
    private Vector2 moveAmount;
    

    private Rigidbody rb;
    public float speed;
    public float acceleration;

    private void Awake()
    {
        moveAction = InputSystem.actions.FindAction("Move");
    }

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();//Gets reference to the rigidbody
    }

    private void OnEnable()
    {
        //Enable action map for cart
        InputActions.FindActionMap("Cart").Enable();
    }

    private void OnDisable()
    {
        //Disable action map for cart
        InputActions.FindActionMap("Cart").Disable();
    }

    void Move(float input = 0)
    {
        Vector3 _move = new Vector3(speed *input, 0, 0);
        rb.AddForce(_move, ForceMode.Force);
    }

    public void OnMove()
    {

        moveAmount = moveAction.ReadValue<Vector2>();
        Vector2 mvmt = new Vector2(moveAmount.x * speed, 0);
        Vector3 currentVel = rb.velocity;
        rb.velocity = Vector2.Lerp(currentVel, mvmt, acceleration * Time.deltaTime);
    }
}

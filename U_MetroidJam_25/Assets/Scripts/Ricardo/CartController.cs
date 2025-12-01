using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;


public class CartController : MonoBehaviour
{
    //Simple enum based state machine
    public enum CartState
    {
        idle, controlled
    }

    public CartState state;
    public SplineContainer cartSpline;
    public float maxSpeed = 10f;
    public float accel = 5f;

    private float currentPos;
    private float currentSpeed = 0f;

    public InputActionAsset InputActions;
    private InputAction moveAction;
    private Vector2 moveAmount;

    private void Awake()
    {
        moveAction = InputActions.FindActionMap("Cart").FindAction("Move");
    }

    private void Start()
    {
        
        cartSpline.Spline.Evaluate(0, out var localPos, out var localDir, out var localUp);
        Debug.Log("Up vector: " + localUp + "\ndirection vector:  " + localDir);
    }

    private void OnEnable()
    {
        InputActions.FindActionMap("Cart").Enable();
    }

    private void OnDisable()
    {
        InputActions.FindActionMap("Cart").Disable();
    }

    void Update()
    {
        switch (state)
        {
            case CartState.idle: break; //Does nothing
            case CartState.controlled: UpdateSplinePos(); break; //Player Controlled state
            default: state = CartState.idle; break; //Default state is idle
        }
    }

    void UpdateSplinePos()
    {
        float splineLength = cartSpline.Spline.GetLength();
        float speed = GetMoveSpeed();

        currentPos = Mathf.Clamp(currentPos + speed * Time.deltaTime, 0f, splineLength);
        float normalizedPos = currentPos / splineLength;

        cartSpline.Spline.Evaluate(normalizedPos, out var localPos, out var localDir, out var localUp);

        Vector3 worldPos = cartSpline.transform.TransformPoint(localPos);
        Vector3 worldDir = cartSpline.transform.TransformDirection(localDir);
        Vector3 worldUp = cartSpline.transform.TransformDirection(localUp);

        Quaternion splineRot = Quaternion.LookRotation(worldDir, Vector3.up);

        // If your cart’s forward axis is +X instead of +Z:
        Quaternion offset = Quaternion.Euler(0f, -90f, 0f);

        transform.SetPositionAndRotation(worldPos, splineRot * offset);
    }

    float GetMoveSpeed()
    {
        moveAmount = moveAction.ReadValue<Vector2>();
        float targetSpeed = moveAmount.x * maxSpeed;

        // Smooth acceleration toward target speed
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accel * Time.deltaTime);

        return currentSpeed;
    }


    public void ChangeState(CartState _state)
    {
        state = _state;
        Debug.Log("Current State: " + state.ToString());
    }
}


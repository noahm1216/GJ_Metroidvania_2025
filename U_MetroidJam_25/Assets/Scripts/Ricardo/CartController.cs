using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;


public class CartController : MonoBehaviour
{
    // Simple enum-based state machine
    public enum CartState { Idle, Controlled }
    public CartState state = CartState.Idle;

    [Header("Spline Settings")]
    public SplineContainer cartSpline;
    public float maxSpeed = 10f;
    public float accel = 5f;

    [Header("Input Settings")]
    public InputActionAsset InputActions;
    private InputAction moveAction;
    private Vector2 moveAmount;

    private float currentPos;       // distance along spline
    private float currentSpeed = 0f;

    private void Awake()
    {
        moveAction = InputActions.FindActionMap("Cart").FindAction("Move");
    }

    private void Start()
    {
        // Initialize cart position at closest spline point to its starting transform
        currentPos = FindClosestSplinePos(transform.position, 200);
        Debug.Log($"Cart initialized at spline distance {currentPos}");
    }

    private void OnEnable()
    {
        InputActions.FindActionMap("Cart").Enable();
    }

    private void OnDisable()
    {
        InputActions.FindActionMap("Cart").Disable();
    }

    private void Update()
    {
        switch (state)
        {
            case CartState.Idle:
                break; // Does nothing
            case CartState.Controlled:
                UpdateSplinePos();
                break;
            default:
                state = CartState.Idle;
                if (moveAmount.x > 0.15f) moveAmount -= (new Vector2(1, 1) * Time.deltaTime);
                break;
        }
    }

    void UpdateSplinePos()
    {
        float splineLength = cartSpline.Spline.GetLength(); //Get spline length
        float speed = GetMoveSpeed(); //Gets speed of cart

        currentPos = Mathf.Clamp(currentPos + speed * Time.deltaTime, 0f, splineLength); //Gets current position of cart along a length equal to the splne length
        float normalizedPos = currentPos / splineLength; //Finds the position on the actual spline

        cartSpline.Spline.Evaluate(normalizedPos, out var localPos, out var localDir, out var localUp);//Gives us data of spline at desired position

        ///Find the world position of the spline points as well as directions
        Vector3 worldPos = cartSpline.transform.TransformPoint(localPos);
        Vector3 worldDir = cartSpline.transform.TransformDirection(localDir);
        Vector3 worldUp = cartSpline.transform.TransformDirection(localUp);

        Quaternion splineRot = Quaternion.LookRotation(worldDir, Vector3.up);
        Quaternion offset = Quaternion.Euler(0f, -90f, 0f); //Because the carts forward axis is not X, it is Z

        //Set cart to position along the spline in the world space
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

    /// <summary>
    /// Finds the closest spline position (distance along spline) to a given world position.
    /// Got help from COPILOT for this
    /// </summary>
    float FindClosestSplinePos(Vector3 worldPos, int samples = 100)
    {
        float splineLength = cartSpline.Spline.GetLength();
        float closestDistance = float.MaxValue;
        float closestPos = 0f;

        for (int i = 0; i <= samples; i++)
        {
            float normalizedPos = i / (float)samples;
            cartSpline.Spline.Evaluate(normalizedPos, out var localPos, out _, out _);

            Vector3 sampleWorldPos = cartSpline.transform.TransformPoint(localPos);
            float dist = Vector3.Distance(worldPos, sampleWorldPos);

            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestPos = normalizedPos * splineLength;
            }
        }

        return closestPos;
    }
}



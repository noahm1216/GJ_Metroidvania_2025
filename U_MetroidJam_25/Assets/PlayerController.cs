using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerState
{
    Normal, Cart
}
public class PlayerController : MonoBehaviour
{
    public PlayerState currentState = PlayerState.Normal;
    public float moveSpeed = 5f;       // Movement speed
    public float jumpForce = 5f;       // Jump strength
    private Rigidbody rb;
    private FixedJoint joint;
    private bool isGrounded;
    public GameObject cart;


    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        switch (currentState)
        {
            case PlayerState.Normal:
                HandleNormalState();
                break;

            case PlayerState.Cart:
                
                break;
        }

        // Example: toggle states with key press
        if (Input.GetKeyDown(KeyCode.C))
        {
            currentState = (currentState == PlayerState.Normal) ? PlayerState.Cart : PlayerState.Normal;
            Debug.Log("Switched to state: " + currentState);
        }

    }
    void OnCollisionEnter(Collision collision)
    {
        // Simple ground check
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }

        if (collision.gameObject.CompareTag("Cart") && currentState == PlayerState.Normal)
        {
            EnterCart(collision.rigidbody);
        }

    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }
    
    void HandleNormalState()
    {
        float moveX = Input.GetAxis("Horizontal"); // left/right input
        rb.velocity = new Vector3(moveX * moveSpeed, rb.velocity.y, 0);

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }
    void EnterCart(Rigidbody cartBody)
    {
        ChangeState(PlayerState.Cart);

        // Attach player to cart with FixedJoint
        joint = rb.gameObject.AddComponent<FixedJoint>();
        joint.connectedBody = cartBody;

        // Optional: disable player physics so cart drives everything
        //rb.isKinematic = true;

        cartBody.gameObject.GetComponent<CartBrain>().ChangeState(CartState.normal);
    }


    public void ChangeState(PlayerState state)
    {
        currentState = state;
        Debug.Log("Player state changed to " + state.ToString());
    }

}

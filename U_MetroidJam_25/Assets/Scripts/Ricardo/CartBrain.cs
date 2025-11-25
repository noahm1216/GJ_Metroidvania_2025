using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public enum CartState
{
    idle,normal,drill
}
public class CartBrain : MonoBehaviour
{
   public CartState currentState = CartState.idle;
    public GameObject drill;
    public BlockMover blockMover;
    private bool canDrive = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        switch (currentState)
        {
            case CartState.idle: break;
            case CartState.normal:
                canDrive = true;
                break;
            case CartState.drill:
                if(drill != null)
                {
                    Vector3 pos = drill.transform.localPosition;
                    if (drill.gameObject.activeSelf == false)
                    {
                        drill.gameObject.SetActive(true);
                    }
                    
                    drill.transform.localPosition = pos;
                }
                break;
                default: break;
        }
    }

    private void FixedUpdate()
    {
        if (canDrive)
        {
            blockMover.Controls();
        }
    }

    public void ChangeState(CartState state)
    {
        currentState = state;
        Debug.Log("Current State: " + currentState.ToString());
    }
    
    

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "PowerUps")
        {
            Debug.Log("Collided with PowerUp");
            ChangeState(CartState.drill);
            collision.gameObject.SetActive(false);  
        }
    }
}


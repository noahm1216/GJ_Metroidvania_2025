using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TriggerAction : MonoBehaviour
{
    public CartController cartController;
    private bool playerDetected;

    // Start is called before the first frame update
    void Start()
    {
        cartController = GetComponentInParent<CartController>();
        if (cartController == null)
        {
            Debug.LogWarning("No cart controller detected. Please attach it in the inspector");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        ControllerRailRider rider = other.gameObject.GetComponentInParent<ControllerRailRider>();

        if(rider != null && playerDetected == false)
        {
            playerDetected = true;
            cartController.ChangeState(CartController.CartState.Controlled);
            rider.ChangeMotion(ControllerRailRider.MOTIONSTATE.RidingCart);
            Debug.Log("Player detected");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (playerDetected)
        {
            playerDetected = false;
        }
    }
}

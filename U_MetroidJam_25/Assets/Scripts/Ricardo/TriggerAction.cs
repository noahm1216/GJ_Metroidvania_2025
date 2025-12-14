using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TriggerAction : MonoBehaviour
{
    public CartController cartController;
    private bool playerDetected;
    private ControllerRailRider playerRailRiderRef;
    private ControllerRailRider rider;

    // Start is called before the first frame update
    void Start()
    {
        cartController = GetComponentInParent<CartController>();
        if (cartController == null)
        {
            Debug.LogWarning("No cart controller detected. Please attach it in the inspector");
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        rider = other.gameObject.GetComponentInParent<ControllerRailRider>();

        if (rider != null && playerDetected == false)
        {
            playerDetected = true;
            if (cartController) cartController.ChangeState(CartController.CartState.Controlled);
            if (rider) rider.ChangeMotion(ControllerRailRider.MOTIONSTATE.RidingCart);
            other.TryGetComponent(out playerRailRiderRef);
            if (playerRailRiderRef) playerRailRiderRef.CartData(transform, true, transform);
            Debug.Log("Player detected");
        }
    }

    public void RemovePlayer()
    {
        playerDetected = false;
        if (cartController) { cartController.ChangeState(CartController.CartState.Idle); }
        if (rider) { rider.ChangeMotion(ControllerRailRider.MOTIONSTATE.NotRiding); }
        Debug.Log("Player removed from cart");

    }

    private void OnTriggerExit(Collider other)
    {
        if (playerDetected)
        {
            playerDetected = false;
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MinecartRespawner : MonoBehaviour
{

    public GameObject cart;

    public TMP_Text distanceTraveled;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = new Vector3(cart.transform.position.x, transform.position.y, transform.position.z);

        distanceTraveled.text = Mathf.Round(cart.transform.position.x).ToString() + " m";
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            Debug.Log("respawn");
            SceneManager.LoadScene("CartRunnerMinigame");
        }

        if (other.gameObject.CompareTag("rock"))
        {
            Destroy(other.gameObject);
        }
    }
}

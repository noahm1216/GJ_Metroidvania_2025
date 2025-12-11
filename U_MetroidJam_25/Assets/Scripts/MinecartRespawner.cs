using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MinecartRespawner : MonoBehaviour
{
    public static MinecartRespawner Instance;

    public GameObject cart;

    public TMP_Text distanceTraveled;

    public GameObject distanceTraveledUI;

    public GameObject respawnMenu;

    public TMP_Text loseScreenText;
    
    // Start is called before the first frame update
    void Start()
    {
        Time.timeScale = 1;

        if (Instance == null)
        {
            Instance = this;
        }
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
            RespawnUI();
            // Time.timeScale = 0;
            // respawnMenu.SetActive(true);
            // distanceTraveledUI.SetActive(false);
            // loseScreenText.text = "OH NO, YOU CRASHED\nat " + Mathf.Round(cart.transform.position.x).ToString() + " meters";
            //SceneManager.LoadScene("CartRunnerMinigame");
        }

        if (other.gameObject.CompareTag("rock"))
        {
            Destroy(other.gameObject);
        }
    }

    public void RespawnUI()
    {
        Time.timeScale = 0;
        respawnMenu.SetActive(true);
        distanceTraveledUI.SetActive(false);
        loseScreenText.text = "OH NO, YOU CRASHED\nat " + Mathf.Round(cart.transform.position.x).ToString() + " meters";
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene("CartRunnerMinigame");
    }

    public void MainMenu()
    {
        //SceneManager.LoadScene("MainMenu");
    }
}

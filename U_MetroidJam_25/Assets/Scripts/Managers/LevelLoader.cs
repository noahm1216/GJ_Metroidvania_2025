using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelLoader : MonoBehaviour
{

    public static LevelLoader Instance;

    public Image fadeImage;
    private float fadeTimeMax = 100, currentFadeTime, fadeMultiplier;
    private bool fadeIn, finishedFade;    

    public Transform playerObj, cartObj;
    private List<CustomLoaderData> levelsLoaded = new List<CustomLoaderData>();

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }

    private void Start()
    {
        finishedFade = true;
        if(fadeImage) { fadeImage.gameObject.SetActive(true); fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, 0); }
        fadeIn = true;
    }

    void LateUpdate()
    {
        if (Input.GetKeyDown("space"))
        {
            print($"pressed space: fading {!fadeIn}");
            SetFade(!fadeIn);
        }

        if (!finishedFade)
        {
            float percentFaded = 0;

            if (fadeIn) // show screen (not black)
            {
                currentFadeTime -= Time.time * fadeMultiplier;
                if (currentFadeTime < 0) { currentFadeTime = 0; percentFaded = currentFadeTime / fadeTimeMax; finishedFade = true; }
            }
            else // hide screen (show black)
            {
                currentFadeTime += Time.time * fadeMultiplier;
                if (currentFadeTime > fadeTimeMax) { currentFadeTime = fadeTimeMax; percentFaded = currentFadeTime / fadeTimeMax; finishedFade = true; }
            }
            percentFaded = currentFadeTime / fadeTimeMax;
            if (fadeImage) fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, percentFaded);
        }
    }

    public void LoadLevel(CustomLoaderData _levelToLoad)
    {
        SetFade(true);

        if(_levelToLoad == null) { Debug.LogWarning("Null Level Passed"); return; }

        if (levelsLoaded.Count == 0 || !levelsLoaded.Contains(_levelToLoad))
            levelsLoaded.Add(_levelToLoad);


        for (int i = 0; i < levelsLoaded.Count; i++)
        {
            if (levelsLoaded[i] == _levelToLoad)
            {
                //levelsLoaded[i].objectToLoad.SetActive(true);
                if (playerObj) playerObj.position = levelsLoaded[i].playerSpawn.position;
                if (cartObj) cartObj.position = levelsLoaded[i].cartSpawn.position;
            }
            else
            {
                levelsLoaded[i].level.LoadData(true);
            }
        }
    }

    public void SetFade(bool _fadeIn)
    {
        if (!finishedFade) return;

        fadeIn = _fadeIn;
        finishedFade = false;
        if (fadeIn) { currentFadeTime = fadeTimeMax; fadeMultiplier = 0.05f; }
        else {currentFadeTime = 0; fadeMultiplier = 0.1f; }
    }

    
}

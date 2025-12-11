using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelData : MonoBehaviour
{
    public CustomLoaderData levelData;
    public List<Collider> tempColliders = new List<Collider>();

    private void Start()
    {
        levelData.level = this;
    }

    public void OnTriggerEnter(Collider trig)
    {
        LoadData(false);
    }

    public void LoadData(bool _unload)
    {
        if (_unload) // handled by levelLoader.instance
        {
            for(int i = 0; i < tempColliders.Count; i++)
            {
                tempColliders[i].enabled = true;
            }
            if (levelData.objectToLoad) levelData.objectToLoad.SetActive(false);
        }
        else
        {
            for (int i = 0; i < tempColliders.Count; i++)
            {
                tempColliders[i].enabled = false;
            }

            if (levelData.objectToLoad) levelData.objectToLoad.SetActive(true);
            if (LevelLoader.Instance) LevelLoader.Instance.LoadLevel(levelData);
        }

    }
}



// the custom data for level chunks
[System.Serializable]
public class CustomLoaderData
{
    public string LevelLoadName;
    public LevelData level;

    [Tooltip("")]
    public GameObject objectToLoad;
    [Tooltip("")]
    public Transform playerSpawn;
    [Tooltip("")]
    public Transform cartSpawn;

    //public CustomLoaderData(string _newName,)
    //{
    //    //abilityNickname = _newName;
    //}

}//end of data for levels

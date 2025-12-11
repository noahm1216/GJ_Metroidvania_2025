using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RockSpawner : MonoBehaviour
{
    //public bool spawning;
    public List<GameObject> rockObjs;
    public float spawnDelayMin, spawnDelayMax, spawnRadius;
    public Transform rockHolder;
    public GameObject rockWarningUI;
    
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(SpawnCoroutine());
    }

    // Update is called once per frame
    void Update()
    {
        //StartCoroutine(SpawnCoroutine());
    }

    // public void SpawnRock()
    // {
    //     if (spawning) {
    //         Instantiate(rockObjs[Random.Range(0, rockObjs.Count)], transform.position + (Random.insideUnitSphere * spawnRadius), Quaternion.identity, transform);
    //     }
    //     Invoke("SpawnRock", Random.Range(spawnDelayMin, spawnDelayMax));
    //     
    //     //Instantiate(rockObjs[Random.Range(0, rockObjs.Count - 1)], this.transform);
    // }
    
    IEnumerator SpawnCoroutine ()
    {
        //float randomTime = Random.Range(spawnDelayMin, spawnDelayMax);
        
        //WaitForSeconds waitTime = new WaitForSeconds(Random.Range(spawnDelayMin, spawnDelayMax)+1);
        while (true) {
            float randomTime = Random.Range(spawnDelayMin, spawnDelayMax);
            
            yield return new WaitForSeconds(randomTime - 2f);
            
            RockFallWarning();
            yield return new WaitForSeconds(2f);
            rockWarningUI.SetActive(false);
            // Generate a random angle for the Y-axis
            float randomAngle = Random.Range(0f, 360f);

            // Create a Quaternion representing only Y-axis rotation
            Quaternion randomRotation = Quaternion.Euler(randomAngle, randomAngle, randomAngle);
            Instantiate (rockObjs[Random.Range(0, rockObjs.Count - 1)], transform.position, randomRotation, rockHolder);
            //make it wait time plus 1 and then during that 1 second play the warning
        }
    }
    
    void RockFallWarning()
    {
        rockWarningUI.SetActive(true);
        Debug.Log("⚠ Warning - spawn in 1 second!");
    }
}

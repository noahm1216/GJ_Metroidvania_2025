using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RockSpawner : MonoBehaviour
{
    //public bool spawning;
    public List<GameObject> rockObjs;
    public float spawnDelayMin, spawnDelayMax, spawnRadius;
    public Transform rockHolder;
    
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
        WaitForSeconds waitTime = new WaitForSeconds(Random.Range(spawnDelayMin, spawnDelayMax));
        while (true) {
            // Vector3 brownSpawnPos = new Vector3 (Random.Range (0, width), 0, Random.Range (0, length));
            // Vector3 yellowSpawnPos = new Vector3 (Random.Range (0, width), 0, Random.Range (0, length));
            Instantiate (rockObjs[Random.Range(0, rockObjs.Count - 1)], transform.position + (Random.insideUnitSphere * spawnRadius), Quaternion.identity, rockHolder);
            Instantiate (rockObjs[Random.Range(0, rockObjs.Count - 1)], transform.position + (Random.insideUnitSphere * spawnRadius), Quaternion.identity, rockHolder);
            Instantiate (rockObjs[Random.Range(0, rockObjs.Count - 1)], transform.position + (Random.insideUnitSphere * spawnRadius), Quaternion.identity, rockHolder);
            //Instantiate (yellowPrefab, brownSpawnPos, Quaternion.identity, laneTransform);
            yield return waitTime;
        }
    }
}

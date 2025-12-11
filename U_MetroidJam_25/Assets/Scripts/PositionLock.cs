using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionLock : MonoBehaviour
{
    private float startYPos;
    private float startZPos;
    // Start is called before the first frame update
    void Start()
    {
        startYPos = transform.position.y;
        startZPos = transform.position.z;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = new Vector3(transform.position.x, 1.52f, startZPos);
    }
}

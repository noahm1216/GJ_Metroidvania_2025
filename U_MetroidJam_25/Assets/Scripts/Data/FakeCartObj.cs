using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FakeCartObj : MonoBehaviour
{
    public LayerMask cartRailsMask;
    public GameObject realCartObj;

    private void OnCollisionEnter(Collision col)
    {
        // if we are touching the cartRails (and hopefully inside 1.8m range)
        if (((1 << col.gameObject.layer) & cartRailsMask) != 0 && realCartObj)
        {
            realCartObj.SetActive(true);
            realCartObj.transform.position = transform.position;
            gameObject.SetActive(false);
        }

    }
}

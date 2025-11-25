using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockMover : MonoBehaviour
{
    private Rigidbody rb;
    public float speed;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();//Gets reference to the rigidbody
    }


    void Move(float input = 0)
    {
        Vector3 _move = new Vector3(speed *input, 0, 0);
        rb.AddForce(_move, ForceMode.Force);
    }

    public void Controls()
    {
        if (Input.GetKey(KeyCode.RightArrow))
        {
            Move(1);
        }

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            Move(-1);
        }
    }
}

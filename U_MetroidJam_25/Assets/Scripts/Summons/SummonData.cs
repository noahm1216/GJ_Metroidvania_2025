using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SummonData : MonoBehaviour
{
    public enum SUMMONTYPE { Hulk, Bat, Spirit }

    public SUMMONTYPE summonType;
    public ControllerTwoPointFiveD ref_PlayerController; // we should inject/pass this when we spawn the zombie
    public Rigidbody rb3d;

    public float abilityWaitTime = 1.5f;
    private float abilityUseStamp;

    // Start is called before the first frame update
    void Start()
    {
        TryGetComponent(out rb3d);
        gameObject.SetActive(false);
    }

    public void OnEnable()
    {
        transform.position = ref_PlayerController.transform.position + new Vector3(2, 2, 0);
    }

    public void ActivateAbility()
    {
        if (Time.time > abilityUseStamp + abilityWaitTime)
        {
            abilityUseStamp = Time.time;

            switch (summonType)
            {
                case SUMMONTYPE.Hulk:
                    print("Hulk Summon Ability");
                    break;
                case SUMMONTYPE.Bat:
                    Vector3 playerCurrentPos = ref_PlayerController.transform.position;
                    ref_PlayerController.transform.position = transform.position;
                    transform.position = playerCurrentPos;
                    break;
                case SUMMONTYPE.Spirit:
                    print("Spirit Summon Ability");
                    break;
                default:
                    break;
            }
        }
    }
}

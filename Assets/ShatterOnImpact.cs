using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShatterOnImpact : MonoBehaviour
{
    public GameObject replacement;

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name != "Player")
        {
            GameObject.Instantiate(replacement, transform.position, transform.rotation);
            Destroy(gameObject);
        }
    }
}

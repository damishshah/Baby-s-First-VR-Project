using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyOnImpact : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        if (shouldTriggerShatter(collision))
        {
            Destroy(gameObject);
        }
    }

    private bool shouldTriggerShatter(Collision collision) {
        return collision.gameObject.tag == "wall" && collision.relativeVelocity.magnitude > 2;
    }
}

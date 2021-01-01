using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyOnAnyImpact : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        if (shouldTriggerDestroy(collision))
        {
            Destroy(gameObject);
        }
    }

    private bool shouldTriggerDestroy(Collision collision) {
        return collision.relativeVelocity.magnitude > 2;
    }
}

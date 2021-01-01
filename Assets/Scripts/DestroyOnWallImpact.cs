using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyOnWallImpact : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        if (shouldTriggerDestroy(collision))
        {
            Destroy(gameObject);
        }
    }

    private bool shouldTriggerDestroy(Collision collision) {
        return collision.gameObject.tag == "wall" && collision.relativeVelocity.magnitude > 2;
    }
}

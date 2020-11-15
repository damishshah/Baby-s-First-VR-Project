using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DamageOnImpact : MonoBehaviour
{        
    void OnCollisionEnter(Collision collision)
    {
        if (shouldTriggerDamage(collision))
        {
            float randomizedX = transform.position.x + Random.Range(0f, 3f);
            float randomizedY = transform.position.y + Random.Range(1.0f, 2.0f);
            DamagePopup.Create(new Vector3(randomizedX, randomizedY, transform.position.z), (int)collision.relativeVelocity.magnitude);
        }
    }

    private bool shouldTriggerDamage(Collision collision) {
        return collision.gameObject.name != "Player" && collision.relativeVelocity.magnitude > 2;
    }

}

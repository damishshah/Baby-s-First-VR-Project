﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShatterOnImpact : MonoBehaviour
{
    public GameObject replacement;

    void OnCollisionEnter(Collision collision)
    {
        if (shouldTriggerShatter(collision))
        {
            GameObject.Instantiate(replacement, transform.position, transform.rotation);
            Destroy(gameObject);
        }
    }

    private bool shouldTriggerShatter(Collision collision) {
        return collision.gameObject.name != "Player" && collision.relativeVelocity.magnitude > 2;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShatterOnImpact : MonoBehaviour
{
    public GameObject replacement;

    public float radius = 5.0F;
    public float power = 500.0F;

    void OnCollisionEnter(Collision collision)
    {
        if (shouldTriggerShatter(collision))
        {
            Destroy(gameObject);
            GameObject.Instantiate(replacement, transform.position, transform.rotation);

            Vector3 collisionLocation = collision.contacts[0].point;
            Collider[] colliders = Physics.OverlapSphere(collisionLocation, radius);
            foreach (Collider hit in colliders)
            {
                Rigidbody rb = hit.GetComponent<Rigidbody>();

                if (hit.tag == "wall" && rb != null) {
                    Debug.Log(hit.tag);
                    rb.AddExplosionForce(power, collisionLocation, radius);
                }
            }
        }
    }

    private bool shouldTriggerShatter(Collision collision) {
        return collision.gameObject.name != "Player" && collision.relativeVelocity.magnitude > 2;
    }
}

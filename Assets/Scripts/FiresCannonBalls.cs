using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FiresCannonBalls : MonoBehaviour
{
    public GameObject projectile;
    public float fireRate;
    public float speed;

    Transform mouth;
    private float timer;

    // Start is called before the first frame update
    void Start()
    {
        mouth = gameObject.transform.Find("Mouth");
        timer = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        if (timer < fireRate) {
            timer += Time.deltaTime;
        } else {
            timer = 0f;
            fireProjectile(projectile, mouth, speed);
        }
    }

    void fireProjectile(GameObject projectile, Transform origin, float speed) {
        GameObject projectileInstance = Instantiate(projectile, origin.position, Quaternion.identity);
        projectileInstance.GetComponent<Rigidbody>().AddForce(transform.up * speed);
    }
}

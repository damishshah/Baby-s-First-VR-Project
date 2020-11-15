using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeOut : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(fadeOutAfterDelay(gameObject.GetComponent<MeshRenderer>().material, 2f, 3f));
    }

    IEnumerator fadeOutAfterDelay(Material mat, float duration, float delay) {
        float counter = 0;

        while (counter < delay) {
            counter += Time.deltaTime;
            yield return null;
        }

        StartCoroutine(fadeOut(mat, duration));
    }

    IEnumerator fadeOut(Material mat, float duration)
    {
        float counter = 0;
        Color spriteColor = mat.color;

        while (counter < duration)
        {
            counter += Time.deltaTime;

            float alpha = Mathf.Lerp(1, 0, counter / duration);

            mat.color = new Color(spriteColor.r, spriteColor.g, spriteColor.b, alpha);
            gameObject.GetComponent<MeshRenderer>().material = mat;

            yield return null;
        }

        Destroy(gameObject);
    }
}

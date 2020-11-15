using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{    
    public static DamagePopup Create(Vector3 pos, int damageAmount) {
        GameObject damagePopupTransform = Instantiate(GameAssets.assets.DamagePopupPrefab, pos, Quaternion.identity);
        DamagePopup damagePopup = damagePopupTransform.GetComponent<DamagePopup>();
        damagePopup.Setup(damageAmount);
        return damagePopup;
    }

    private TextMeshPro textMesh;
    private float disappearTimer = 1f;
    private float disappearSpeed = 1f;
    private float moveSpeed = 1f;
    private Color textColor;

    private void Awake() {
        textMesh = gameObject.GetComponent<TextMeshPro>();
        textColor = textMesh.color;
    }

    public void Setup(int damageAmount) {
        textMesh.SetText(damageAmount.ToString());
    }

    public void Update() {
        transform.position += new Vector3(0, moveSpeed) * Time.deltaTime;

        disappearTimer -= Time.deltaTime;
        if (disappearTimer < 0) {
            textColor.a -= disappearSpeed * Time.deltaTime;
            textMesh.color = textColor;
            if (textColor.a < 0) {
                Destroy(gameObject);
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trigger : MonoBehaviour


{
    bool hasPackage;
    //[SerializeField] float destroytime = 0.1f;
    [SerializeField] Color32 hasPackageColor = new Color32(1, 1, 1, 1);
    [SerializeField] Color32 noPackageColor = new Color32(0, 0, 0, 0);

    SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Package" && !hasPackage)
        {
            Debug.Log("Packet has taken");
            hasPackage = true;
            Destroy(other.gameObject);
            spriteRenderer.color = hasPackageColor;
        }

        if (other.tag == "Package_Delivery" && hasPackage)
        {
            Debug.Log("Paket Teslim Edildi.");
            hasPackage = false;
            spriteRenderer.color = noPackageColor;
        }
    }
}

using UnityEngine;

public class BackgroundFollow : MonoBehaviour
{
    private Transform cam;
    private float startX;

    void Start()
    {
        cam = Camera.main.transform;
        startX = transform.position.x;
    }

    void LateUpdate()
    {
        // Solo sigue en Y, mantiene X fija
        transform.position = new Vector3(startX, cam.position.y, transform.position.z);
    }
}
using UnityEngine;

public class BackgroundFollow : MonoBehaviour
{
    private Transform cam;
    private float alturaSegmento;
    private float startX;

    void Start()
    {
        cam = Camera.main.transform;
        startX = transform.position.x;

        // Toma la altura del SpriteRenderer tileado
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        alturaSegmento = sr != null ? sr.size.y : 75f;
    }

    void LateUpdate()
    {
        // Sigue en X fija
        float nuevoY = transform.position.y;

        // Si la cámara superó la mitad superior del fondo, lo sube un segmento
        if (cam.position.y > transform.position.y + alturaSegmento * 0.5f)
        {
            nuevoY += alturaSegmento;
        }

        transform.position = new Vector3(startX, nuevoY, transform.position.z);
    }
}
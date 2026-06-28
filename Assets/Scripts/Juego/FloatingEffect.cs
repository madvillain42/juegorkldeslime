using UnityEngine;

public class FloatingEffect : MonoBehaviour
{
    [Header("Ajustes de Flotación")]
    [SerializeField] private float speed = 3f;      // Qué tan rápido sube y baja
    [SerializeField] private float amplitude = 0.15f; // Qué tan arriba/abajo llega (0.15 unidades de Unity)

    private float startY;

    void Start()
    {
        // Guardamos la posición Y inicial del objeto para que flote alrededor de ella
        startY = transform.position.y;
    }

    void Update()
    {
        // Calculamos el nuevo desplazamiento usando la función Seno
        float newY = startY + Mathf.Sin(Time.time * speed) * amplitude;
        
        // Aplicamos la posición manteniendo la X y Z originales
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}
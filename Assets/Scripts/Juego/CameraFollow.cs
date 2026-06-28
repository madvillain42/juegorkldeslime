using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Configuración de Seguimiento")]
    [SerializeField] private Transform target; // Arrastra aquí a tu Slime
    [SerializeField] private float smoothSpeed = 5f; // Qué tan suave sigue al personaje
    [SerializeField] private float offsetY = 2f; // Para que la cámara mire un poco más arriba del Slime

    [Header("Límites de la Torre")]
    [SerializeField] private bool usarLimiteY = true; // Actívalo o desactívalo desde el Inspector
    [SerializeField] private float limiteMaximoY = 700f; // Cámbialo a mano según la altura de tu nivel

    private float targetY;

    void Start()
    {
        if (target == null)
        {
            // Intentar buscar al Slime automáticamente si se nos olvidó arrastrarlo
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Solo seguimos al personaje si sube más allá de la posición actual de la cámara menos el offset
        // Esto evita que la cámara baje bruscamente si el Slime se cae al suelo inicial
        if (target.position.y + offsetY > transform.position.y)
        {
            targetY = target.position.y + offsetY;
        }
        else
        {
            targetY = transform.position.y;
        }

        // BLOQUEO DE CÁMARA EN LA CIMA:
        // Si el límite está activado, evitamos que targetY supere la altura máxima que definas.
        if (usarLimiteY && targetY > limiteMaximoY)
        {
            targetY = limiteMaximoY;
        }

        // Calculamos la posición destino manteniendo las posiciones X e Z actuales de la cámara fijas
        Vector3 desiredPosition = new Vector3(transform.position.x, targetY, transform.position.z);
        
        // Pasamos posiciones completas (Vector3) a la función Lerp para suavizar el movimiento
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        
        // Aplicamos la posición final corregida a la cámara
        transform.position = smoothedPosition;
    }
}
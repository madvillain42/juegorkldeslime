using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Configuración de Seguimiento")]
    [SerializeField] private Transform target; 
    [SerializeField] private float smoothSpeed = 5f; 
    [SerializeField] private float offsetY = 2f; 

    [Header("Límites de la Torre")]
    [SerializeField] private bool usarLimiteY = true; 
    [SerializeField] private float limiteMaximoY = 700f; 

    private float targetY;

    void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        if (target.position.y + offsetY > transform.position.y)
        {
            targetY = target.position.y + offsetY;
        }
        else
        {
            targetY = transform.position.y;
        }

        if (usarLimiteY && targetY > limiteMaximoY)
        {
            targetY = limiteMaximoY;
        }

        Vector3 desiredPosition = new Vector3(transform.position.x, targetY, transform.position.z);
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;
    }
}
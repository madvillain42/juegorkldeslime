using UnityEngine;

public class FallingProjectile : MonoBehaviour
{
    [Header("Configuración de Caída")]
    [SerializeField] private float fallSpeed = 18f; // Caída MUY rápida
    [SerializeField] private float autoDestroyTime = 4f; 

    void Start()
    {
        // Se destruye automáticamente para no llenar la RAM de proyectiles infinitos cayendo
        Destroy(gameObject, autoDestroyTime);
    }

    void Update()
    {
        // Movimiento constante hacia abajo
        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Tu slime ya debería morir si toca algo con Tag "Lethal", 
        // pero podemos poner este Debug para verificar que choca.
        if (other.CompareTag("Player"))
        {
            Debug.Log("[Meteoro] ¡Impacto directo con el jugador!");
        }
    }
}
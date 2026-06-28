using UnityEngine;

public class AutoDestruirEfecto : MonoBehaviour
{
    // Tiempo que tarda en destruirse. Ajusta esto a lo que dure tu animación (ej: 0.5 segundos)
    [SerializeField] private float tiempoDeVida = 0.5f;

    void Start()
    {
        // Se destruye automáticamente al pasar el tiempo de vida
        Destroy(gameObject, tiempoDeVida);
    }
}
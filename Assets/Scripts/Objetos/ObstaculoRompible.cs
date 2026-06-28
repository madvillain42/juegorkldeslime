using UnityEngine;
using System.Collections;

public class ObstaculoRompible : MonoBehaviour
{
    [Header("Configuración de Ruptura")]
    [SerializeField] private float tiempoTemblando = 0.3f; // Cuánto dura el temblor antes de romperse
    [SerializeField] private float intensidadTemblor = 0.05f; // Qué tan fuerte se mueve hacia los lados

    private Vector3 posicionOriginal;
    private bool yaImpactado = false;

    // Detecta cuando el Slime choca con el Collider (que veo que NO es Trigger en tu imagen)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Comprueba si lo que chocó es el jugador y si no se está rompiendo ya
        if (collision.gameObject.CompareTag("Player") && !yaImpactado)
        {
            yaImpactado = true;
            posicionOriginal = transform.position;
            StartCoroutine(TrembleAndBreak());
        }
    }

    private IEnumerator TrembleAndBreak()
    {
        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < tiempoTemblando)
        {
            // Calcula un desfase aleatorio solo en el eje X para que tiemble hacia los lados
            float offsetX = Random.Range(-intensidadTemblor, intensidadTemblor);
            transform.position = new Vector3(posicionOriginal.x + offsetX, transform.position.y, posicionOriginal.z);

            tiempoTranscurrido += Time.deltaTime;
            yield return null; // Espera al siguiente frame
        }

        // Asegura que si el obstáculo se mueve solo (cae o avanza), no altere drásticamente otra lógica antes de morir
        Destroy(gameObject);
    }
}
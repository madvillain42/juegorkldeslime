using UnityEngine;
using UnityEngine.UI; // Necesario para controlar el Canvas
using System.Collections;

public class FogTransition : MonoBehaviour
{
    [Header("Configuración de Transición")]
    [Tooltip("Arrastra aquí tu FadeScreen del Canvas")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1.5f; // Segundos que tarda en ponerse negro

    private bool isTransitioning = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Si el Slime toca la niebla y aún no estamos en transición
        if (other.CompareTag("Player") && !isTransitioning)
        {
            isTransitioning = true;
            
            // 1. Congelamos al Slime en el aire
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic; // Evita que la gravedad lo jale
                rb.linearVelocity = Vector2.zero;        // Frena cualquier salto en seco
            }

            // 2. Iniciamos el efecto de oscurecimiento suave
            StartCoroutine(FadeToBlack());
        }
    }

    private IEnumerator FadeToBlack()
    {
        float elapsed = 0f;
        Color colorActual = fadeImage.color;
        
        // Nos aseguramos de que empiece en 0
        colorActual.a = 0f;
        fadeImage.color = colorActual;

        // Bucle que aumenta el Alfa poco a poco según el tiempo
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            // Mathf.Clamp01 asegura que el valor nunca se pase de 1
            colorActual.a = Mathf.Clamp01(elapsed / fadeDuration); 
            fadeImage.color = colorActual;
            
            // Espera al siguiente frame para continuar el bucle
            yield return null;
        }

        // Aseguramos que quede 100% negro al final
        colorActual.a = 1f;
        fadeImage.color = colorActual;

        // Una pequeña pausa dramática de medio segundo en pantalla negra
        yield return new WaitForSeconds(0.5f);

        // 3. Avisamos al GameManager que empiece la pelea del Jefe
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.BossBattle);
            Debug.Log("[Transición] ¡Fundido a negro terminado! Iniciando BossFight...");
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class FogTransition : MonoBehaviour
{
    [Header("Configuración Visual")]
    [Tooltip("El script buscará automáticamente un objeto llamado 'FadeScreen' en tu Canvas.")]
    [SerializeField] private float fadeDuration = 1.5f;

    [Header("Cambio de Escena")]
    [SerializeField] private string nombreEscenaJefe;

    private Image fadeImage; // Ya no se asigna en el inspector, se busca en código
    private bool isTransitioning = false;

    private void Start()
    {
        // Busca en toda la escena el objeto llamado "FadeScreen"
        GameObject fadeObj = GameObject.Find("FadeScreen");
        
        if (fadeObj != null)
        {
            fadeImage = fadeObj.GetComponent<Image>();
        }
        else
        {
            Debug.LogError("[FogTransition] ¡No se encontró un objeto llamado 'FadeScreen' en la escena! Revisa tu Canvas.");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isTransitioning)
        {
            isTransitioning = true;
            
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.linearVelocity = Vector2.zero;
            }

            if (fadeImage != null)
            {
                StartCoroutine(FadeOutAndLoadScene());
            }
            else
            {
                Debug.LogError("No se puede hacer el Fade porque falta la imagen. Saltando directo a la escena.");
                SceneManager.LoadScene(nombreEscenaJefe);
            }
        }
    }

    private IEnumerator FadeOutAndLoadScene()
    {
        float elapsed = 0f;
        Color colorActual = fadeImage.color;
        colorActual.a = 0f; 
        fadeImage.color = colorActual;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            colorActual.a = Mathf.Clamp01(elapsed / fadeDuration); 
            fadeImage.color = colorActual;
            yield return null;
        }
        
        colorActual.a = 1f;
        fadeImage.color = colorActual;

        yield return new WaitForSeconds(0.5f); 

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.BossBattle);
        }

        if (!string.IsNullOrEmpty(nombreEscenaJefe))
        {
            SceneManager.LoadScene(nombreEscenaJefe);
        }
    }
}
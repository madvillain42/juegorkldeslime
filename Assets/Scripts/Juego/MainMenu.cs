using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // "public void" es obligatorio para que Unity lo detecte en el botón
    public void IniciarJuego()
    {
        // Cambia "Nivel1" por el nombre exacto de tu escena de juego si es diferente
        SceneManager.LoadScene("Nivel1");
    }
}
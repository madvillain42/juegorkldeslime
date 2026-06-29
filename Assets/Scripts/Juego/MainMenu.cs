using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject panelMenuPrincipal;
    public GameObject panelNiveles;

    // ─── Menú Principal ───────────────────────────────────────────────────────

    public void IniciarJuego()
    {
        SceneManager.LoadScene("Nivel1");
    }

    public void AbrirNiveles()
    {
        panelMenuPrincipal.SetActive(false);
        panelNiveles.SetActive(true);
    }

    public void Salir()
    {
        Application.Quit();
        Debug.Log("[Menu] Salir");
    }

    // ─── Panel Niveles ────────────────────────────────────────────────────────

    public void CargarNivel1()
    {
        SceneManager.LoadScene("Nivel1");
    }

    public void CargarNivel2()
    {
        SceneManager.LoadScene("Nivel2");
    }

    public void Volver()
    {
        panelNiveles.SetActive(false);
        panelMenuPrincipal.SetActive(true);
    }
}
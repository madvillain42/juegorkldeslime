using UnityEngine;
using System.Collections;

public class EfectoIconoFlotante : MonoBehaviour
{
    [SerializeField] private float velocidadSubida = 1.5f;
    [SerializeField] private float tiempoVida = 1.2f;
    
    private SpriteRenderer sr;

    public void IniciarEfecto(Sprite spriteIcono)
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.sprite = spriteIcono;
        
        StartCoroutine(AnimarYDestruir());
    }

    private IEnumerator AnimarYDestruir()
    {
        float tiempo = 0f;
        Color colorOriginal = sr.color;

        while (tiempo < tiempoVida)
        {
            // Sube lentamente
            transform.position += Vector3.up * velocidadSubida * Time.deltaTime;

            // Se va volviendo transparente en la segunda mitad de su vida
            if (tiempo > tiempoVida / 2f)
            {
                float alfa = Mathf.Lerp(1f, 0f, (tiempo - (tiempoVida / 2f)) / (tiempoVida / 2f));
                sr.color = new Color(colorOriginal.r, colorOriginal.g, colorOriginal.b, alfa);
            }

            tiempo += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}
using UnityEngine;
using System.Collections;

public class MeteorWarning : MonoBehaviour
{
    [Header("Ajuste en Pantalla")]
    [Tooltip("Distancia desde el centro de la cámara hacia arriba. Ajusta esto para que quede pegado al borde superior.")]
    [SerializeField] private float offsetYDesdeCentro = 4.5f;

    private GameObject projectilePrefab;
    private float warningDuration;
    private float targetX;
    
    private AudioSource audioSource;
    private SpriteRenderer spriteRenderer;
    private Camera mainCam;

    [Header("Audio")]
    [SerializeField] private AudioClip sonidoCaida;
    [SerializeField] private float pitchCaida = 1.5f;

    public void Inicializar(GameObject fireball, float duration, float xPos, AudioClip warningSound)
    {
        projectilePrefab = fireball;
        warningDuration = duration;
        targetX = xPos;
        mainCam = Camera.main;
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        
        if (warningSound != null)
        {
            audioSource.clip = warningSound;
            audioSource.volume = 0.8f;
            audioSource.Play();
        }

        StartCoroutine(RoutineAdvertencia());
    }

    void Update()
    {
        if (mainCam != null)
        {
            transform.position = new Vector3(targetX, mainCam.transform.position.y + offsetYDesdeCentro, 0f);
        }
    }

    IEnumerator RoutineAdvertencia()
    {
        float elapsed = 0f;
        float blinkInterval = 0.15f;

        while (elapsed < warningDuration)
        {
            if (spriteRenderer != null) spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
        }

        if (spriteRenderer != null) spriteRenderer.enabled = false;

        if (projectilePrefab != null)
        {
            Vector3 spawnPos = new Vector3(targetX, transform.position.y + 6f, 0f);
            GameObject meteoro = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

            if (sonidoCaida != null)
            {
                AudioSource tempAS = meteoro.AddComponent<AudioSource>();
                tempAS.clip = sonidoCaida;
                tempAS.pitch = pitchCaida;
                tempAS.Play();
            }
        }

        Destroy(gameObject);
    }
}
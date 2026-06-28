using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    public float speed = 6f;

    private float despawnY;
    private bool ready = false;

    public void Init(float destroyBelowY)
    {
        despawnY = destroyBelowY;
        ready    = true;
    }

    void Update()
    {
        if (!ready) return;
        transform.position += Vector3.down * speed * Time.deltaTime;
        if (transform.position.y < despawnY) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Buscar el SlimeBossController para manejar vidas
        SlimeBossController slimeBoss = other.GetComponent<SlimeBossController>();
        if (slimeBoss != null)
        {
            slimeBoss.RecibirDaño();
            Destroy(gameObject);
            return;
        }

        // Fallback — si no tiene SlimeBossController, game over directo
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetStats();
            GameManager.Instance.ChangeState(GameState.GameOver);
        }

        Destroy(gameObject);
    }
}
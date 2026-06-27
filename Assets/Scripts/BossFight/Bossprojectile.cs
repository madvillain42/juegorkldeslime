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
        if (other.CompareTag("Player"))
        {
            Debug.Log("Proyectil impactó al slime. Conectar a HP aquí.");
            Destroy(gameObject);
        }
    }
}
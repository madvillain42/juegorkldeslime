using UnityEngine;

public class ObstacleAutoDestroy : MonoBehaviour
{
    private Transform player;
    [SerializeField] private float destroyOffsetBelow = 15f;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        if (player == null) return;
        if (transform.position.y < player.position.y - destroyOffsetBelow)
            Destroy(gameObject);
    }
}
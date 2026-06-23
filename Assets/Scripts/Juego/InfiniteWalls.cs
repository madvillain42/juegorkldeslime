using UnityEngine;

public class InfiniteWall : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private Transform player;
    [SerializeField] private float segmentHeight = 20f;
    [SerializeField] private float recycleOffsetBelow = 10f;

    private Transform[] siblings;
    private float wallX;

    void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player").transform;

        wallX = transform.position.x;

        // Ajustar escala al tamaño de segmento
        transform.localScale = new Vector3(transform.localScale.x, segmentHeight, 1f);

        // Buscar hermanos desde el padre directo
        Transform padre = transform.parent;
        siblings = new Transform[padre.childCount];
        for (int i = 0; i < padre.childCount; i++)
            siblings[i] = padre.GetChild(i);
    }

    void LateUpdate()
    {
        if (player == null) return;

        // Si este segmento quedó muy abajo del jugador, lo reciclamos arriba
        if (transform.position.y < player.position.y - recycleOffsetBelow)
        {
            float highestY = GetHighestSiblingY();
            transform.position = new Vector3(wallX, highestY + segmentHeight, 0f);
        }
    }

    float GetHighestSiblingY()
    {
        float highest = float.MinValue;
        foreach (var sibling in siblings)
        {
            if (sibling.position.y > highest)
                highest = sibling.position.y;
        }
        return highest;
    }
}
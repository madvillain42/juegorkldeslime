using UnityEngine;

public class InfiniteBackground : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private Transform player;
    [SerializeField] private float segmentHeight = 75f;
    [SerializeField] private float recycleOffsetBelow = 10f;

    private Transform[] siblings;
    private float startX;

    void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player").transform;

        startX = transform.position.x;

        // Buscar hermanos desde el padre directo
        Transform padre = transform.parent;
        siblings = new Transform[padre.childCount];
        for (int i = 0; i < padre.childCount; i++)
            siblings[i] = padre.GetChild(i);
    }

    void LateUpdate()
    {
        if (player == null) return;

        if (transform.position.y < player.position.y - recycleOffsetBelow)
        {
            float highestY = GetHighestSiblingY();
            transform.position = new Vector3(startX, highestY + segmentHeight, 0f);
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
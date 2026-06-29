using UnityEngine;

public class RecycleObject : MonoBehaviour
{
    [SerializeField] private float alturaReciclaje = 50f;
    [SerializeField] private float offsetReciclaje = 100f;

    private Transform cam;

    void Start()
    {
        cam = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (transform.position.y < cam.position.y - alturaReciclaje)
        {
            transform.position += new Vector3(0, offsetReciclaje, 0);
        }
    }
}
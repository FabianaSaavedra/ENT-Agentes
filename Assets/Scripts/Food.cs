using UnityEngine;

public class Food : MonoBehaviour
{
    public int nutrition = 5;
    public float lifetimeSeconds = 5f; // X tiempo de la comida antes de desaparecer (EN SEGUNDOS)

    void Start()
    {
        Destroy(gameObject, lifetimeSeconds);
    }
}

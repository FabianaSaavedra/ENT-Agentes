using UnityEngine;

public class FrictionZone : MonoBehaviour
{
    [Header("Settings")]
    public float speedMultiplier = 0.5f; // 0.4f para agua, como acordamos

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Intentamos obtener el componente Bunny O el componente Predator
        Bunny bunny = other.GetComponent<Bunny>();
        Predator predator = other.GetComponent<Predator>();

        if (bunny != null)
        {
            bunny.speed *= speedMultiplier;
            other.GetComponent<SpriteRenderer>().color = new Color(0.7f, 0.7f, 0.7f); // Oscurecer
        }
        else if (predator != null)
        {
            predator.speed *= speedMultiplier;
            other.GetComponent<SpriteRenderer>().color = new Color(0.7f, 0.7f, 0.7f);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Bunny bunny = other.GetComponent<Bunny>();
        Predator predator = other.GetComponent<Predator>();

        if (bunny != null)
        {
            bunny.speed /= speedMultiplier;
            other.GetComponent<SpriteRenderer>().color = Color.white;
        }
        else if (predator != null)
        {
            predator.speed /= speedMultiplier;
            other.GetComponent<SpriteRenderer>().color = Color.white;
        }
    }
}
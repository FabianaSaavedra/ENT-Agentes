using UnityEngine;

public class Predator : MonoBehaviour
{
    [Header("Predator Settings")]
    public float energy = 10;
    public float age = 0;
    public float maxAge = 20;
    public float speed = 1f;
    public float visionRange = 5f;

    // FEATURE: Envejecimiento de depredadores.
    // El depredador pierde velocidad a medida que envejece (ver AgingCalculator).
    [Header("Aging (Envejecimiento)")]
    [Tooltip("Porcentaje de la vida (0 a 1) en el que empieza a perder velocidad. 0.5 = a mitad de su vida.")]
    [Range(0f, 1f)] public float maturityRatio = 0.5f;

    [Tooltip("Porcentaje minimo de velocidad que conserva al llegar a su edad maxima. 0.3 = 30%.")]
    [Range(0.05f, 1f)] public float minSpeedFactor = 0.3f;

    [Tooltip("Velocidad real actual (solo lectura, para ver el efecto en el Inspector).")]
    public float currentSpeed;

    // FEATURE Eventos climáticos: visión real según el clima (ver WeatherSystem).
    [Header("Weather (Clima)")]
    [Tooltip("Vision real actual segun el clima (solo lectura, para ver el efecto en el Inspector).")]
    public float currentVision;

    [Header("Predator States")]
    public bool isAlive = true;
    public PredatorState currentState = PredatorState.Exploring;

    private Vector3 destination;
    private float h;

    private void Start()
    {
        destination = transform.position;
    }

    public void Simulate(float h)
    {
        if (!isAlive) return;

        this.h = h;

        switch (currentState)
        {
            case PredatorState.Exploring:
                Explore();
                break;
            case PredatorState.SearchingFood:
                SearchFood();
                break;
            case PredatorState.Eating:
                Eat();
                break;
        }

        Move();
        Age();
        CheckState();
    }

    void Explore()
    {
        // Si hay comida a la vista, cambiar de estado
        Bunny nearestBunny = FindNearestBunny();
        if (nearestBunny != null)
        {
            currentState = PredatorState.SearchingFood;
            destination = nearestBunny.transform.position;
            return;
        }

        // Si ya llegó al destino, elegir uno nuevo
        if (Vector3.Distance(transform.position, destination) < 0.1f)
        {
            SelectNewDestination();
        }
    }

    void SearchFood()
    {
        Bunny nearestBunny = FindNearestBunny();
        if (nearestBunny == null)
        {
            // Si no hay comida, volver a explorar
            currentState = PredatorState.Exploring;
            return;
        }

        destination = nearestBunny.transform.position;

        // Si está suficientemente cerca, pasar a comer
        if (Vector3.Distance(transform.position, nearestBunny.transform.position) < 0.2f)
        {
            currentState = PredatorState.Eating;
        }
    }

    void Eat()
    {
        Collider2D foodHit = Physics2D.OverlapCircle(transform.position, 0.2f, LayerMask.GetMask("Bunnies"));
        if (foodHit != null)
        {
            Bunny food = foodHit.GetComponent<Bunny>();
            if (food != null)
            {
                energy += food.age;
                Destroy(food.gameObject);
            }
        }

        // Después de comer vuelve a explorar
        currentState = PredatorState.Exploring;
    }

    void Flee()
    {
        SelectNewDestination();
        currentState = PredatorState.Exploring;
    }

    void SelectNewDestination()
    {
        Vector3 direction = new Vector3(
            Random.Range(-visionRange, visionRange),
            Random.Range(-visionRange, visionRange),
            0
        );

        Vector3 targetPoint = transform.position + direction;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction.normalized, visionRange, LayerMask.GetMask("Obstacles"));

        if (hit.collider != null)
        {
            float offset = transform.localScale.magnitude * 0.5f;
            destination = hit.point - (Vector2)direction.normalized * offset;
        }
        else
        {
            destination = targetPoint;
        }
    }

    void Move()
    {
        // FEATURE Envejecimiento: la velocidad real depende de la edad.
        // AgingCalculator devuelve un factor entre minSpeedFactor y 1
        // que se multiplica por la velocidad base.
        currentSpeed = speed * AgingCalculator.GetSpeedFactor(age, maxAge, maturityRatio, minSpeedFactor);

        transform.position = Vector3.MoveTowards(
            transform.position,
            destination,
            currentSpeed * h
        );

        // El gasto de energía usa la velocidad real:
        // un depredador viejo y lento gasta menos energía al moverse.
        energy -= currentSpeed * h;
    }

    void Age()
    {
        age += h;
    }

    void CheckState()
    {
        if (energy <= 0 || age > maxAge)
        {
            isAlive = false;
            Destroy(gameObject);
        }
    }

    // FEATURE Eventos climáticos:
    // Devuelve el rango de visión real = visión base x multiplicador del clima.
    // Despejado = x1, Lluvia = x0.6, Tormenta = x0.3 (configurable en WeatherSystem).
    // Si no existe WeatherSystem, el multiplicador es 1 y todo funciona como antes.
    float GetEffectiveVision()
    {
        currentVision = visionRange * WeatherSystem.VisionMultiplier;
        return currentVision;
    }

    private void OnDrawGizmosSelected()
    {
        // FEATURE Eventos climáticos: el círculo verde muestra la visión real (afectada por el clima)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, Application.isPlaying ? GetEffectiveVision() : visionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(destination, 0.2f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, destination);
    }

    Bunny FindNearestBunny()
    {
        // FEATURE Eventos climáticos: busca conejos solo dentro de la visión afectada por el clima
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, GetEffectiveVision(), LayerMask.GetMask("Bunnies"));
        Debug.Log($"Predator {name} encontró {hits.Length} colliders en su rango");
        Bunny nearest = null;
        float minDist = Mathf.Infinity;

        foreach (Collider2D hit in hits)
        {
            Bunny food = hit.GetComponent<Bunny>();
            if (food != null)
            {
                float dist = Vector2.Distance(transform.position, food.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = food;
                }
            }
        }

        return nearest;
    }
}
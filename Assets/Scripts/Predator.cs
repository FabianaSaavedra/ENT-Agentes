using UnityEngine;

public class Predator : MonoBehaviour
{
    [Header("Predator Settings")]
    public float energy = 10;
    public float age = 0;
    public float maxAge = 20;
    public float speed = 1f;
    public float visionRange = 5f;
    public float baseSpeed;

    [Header("Water Settings")]
    [Range(0f, 100f)]
    public float waterCrossChance = 50f;
    //CONDICIÓN FÍSICA DEL DEPREDADOR EN CASO DE CANSANCIO
    [Header("Physical condition settings")]
    public float maxResistancePursue = 10f; // Resistencia máxima para perseguir a su presa
    public float resistanceLostSecondPursue = 2f; // Resistencia perdida por segundo de persecución
    public float resistanceRecoveredWithResting = 2f; // Resistencia recuperada por descansar
    public float PursueDuration = 3f; //Tiempo máximo de persecución
    public float restingDuration = 3f; //Tiempo de descanso después de abandonar

    // FEATURE: Envejecimiento de depredadores.
    // El depredador pierde velocidad a medida que envejece (ver AgingCalculator).
    [Header("Aging (Envejecimiento)")]
    [Tooltip("Porcentaje de la vida (0 a 1) en el que empieza a perder velocidad. 0.5 = a mitad de su vida.")]
    [Range(0f, 1f)] public float maturityRatio = 0.5f;

    [Tooltip("Porcentaje minimo de velocidad que conserva al llegar a su edad maxima. 0.3 = 30%.")]
    [Range(0.05f, 1f)] public float minSpeedFactor = 0.3f;

    [Tooltip("Velocidad real actual (solo lectura, para ver el efecto en el Inspector).")]
    public float currentSpeed;

    // FEATURE Eventos climaticos: vision real segun el clima (ver WeatherSystem).
    [Header("Weather (Clima)")]
    [Tooltip("Vision real actual segun el clima (solo lectura, para ver el efecto en el Inspector).")]
    public float currentVision;

    [Header("Predator States")]
    public bool isAlive = true;
    public PredatorState currentState = PredatorState.Exploring;

    [Header("Hunger (Progressive increase)")]
    public float timeWithoutFood = 0f;
    public float timeToIncrease = 5f; // Cantidad de segundos para que ocurra un aumento de velocidad
    public float speedBonus = 0.5f; // Cantidad que se le agregar a public float speed

    private Vector3 destination;
    private float h;
    // h es el tiempo que dura cada paso de la simulación, esto viene de SimulationManager.secondsPerIteration =1f;

    // VARIABLES PARA EL CANSANCIO DEL DEPREDADOR
    private float currentResistance; // Resistencia actual del depredador
    private float pursueTimer = 0f; // Tiempo que lleva la persecución (valor inicial)
    private float restingTimer = 0f; // Tiempo que lleva descansando el depredador (valor inicial)
    private bool isResting = false;
    private Bunny currentPrey; // Conejo que esgtá persiguiendo

    private Bunny targetBunny;
    private bool waterDecisionMade = false;
    private bool canCrossWaterThisSearch = false;

    // Mientras este valor sea true, el zorro explora sin volver a detectar comida.
    private bool ignoreFoodUntilExplorePoint = false;

    private void Start()
    {
        destination = transform.position;
        baseSpeed = speed;

        //Al iniciar la simuación, el depredador estará con la resistencia al máximo
        currentResistance = maxResistancePursue;
    }

    public void Simulate(float h) // h es el tiempo de cada paso (1 segundo)
    {
        timeWithoutFood += h;

        // Aumento de velocidad cada cierto tiempo
        if (timeWithoutFood >= timeToIncrease)
        {
            speed += speedBonus;
            timeWithoutFood -= timeToIncrease; // Si es mayor que 0
        }

        if (!isAlive) return;

        this.h = h;

        if (isResting) // Si el depredador está descansando,...
        {
            Rest();
            return;
        }

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
        if (ignoreFoodUntilExplorePoint)
        {
            if (Vector3.Distance(transform.position, destination) < 0.1f)
            {
                ignoreFoodUntilExplorePoint = false;
                SelectNewDestination();
            }
            return;
        }

        Bunny nearestBunny = FindNearestBunny();

        if (nearestBunny != null)
        {
            targetBunny = nearestBunny;
            currentState = PredatorState.SearchingFood;
            waterDecisionMade = false;
            canCrossWaterThisSearch = false;
        }
        // Si hay comida a la vista, cambiar de estado a persecución (caza)
        if (nearestBunny != null && isResting == false) // Si detecta un conejo cerca y no está descansando...
        {
            startPersecution(nearestBunny); // Inicia la persecución
            return;
        }

        if (Vector3.Distance(transform.position, destination) < 0.2f)
        {
            SelectNewDestination();
        }
    }

    // INICIO DE LA PERSECUCIÓN
    void startPersecution(Bunny prey)
    {
        currentPrey = prey; // Se guarda el conejo que va a perseguir el depredador

        pursueTimer = 0f; // Se reinicia el contador de la persecución 
        currentState = PredatorState.SearchingFood; // Se dirigue al estado de buscar comida y ...
        destination = prey.transform.position; // Diriguirse hacia su presa
    }

    void SearchFood()
    {

        Bunny nearestBunny = FindNearestBunny();
        if (nearestBunny == null)
            if (targetBunny == null)
            {
                AbortHuntAndExplore();
                return;
            }

        // verificar que siga dentro del rango de visión
        float dist = Vector3.Distance(transform.position, nearestBunny.transform.position);

        // FEATURE Eventos climaticos: se compara con la vision afectada por el clima
        if (dist > GetEffectiveVision())
        {
            currentState = PredatorState.Exploring;

            SelectNewDestination();
            return;
        }

        destination = nearestBunny.transform.position;

        // Si está suficientemente cerca, pasar a comer
        if (dist < 0.2f)
        {
            currentState = PredatorState.Eating;
            return;
        }

        Vector2 dirNorm = destination / dist;

        RaycastHit2D obstacleHit = Physics2D.Raycast(
            transform.position,
            dirNorm,
            dist,
            LayerMask.GetMask("Obstacles")
        );

        if (obstacleHit.collider != null)
        {
            AbortHuntAndExplore();
            return;
        }

        RaycastHit2D waterHit = Physics2D.Raycast(
            transform.position,
            dirNorm,
            dist,
            LayerMask.GetMask("Water")
        );

        if (waterHit.collider != null)
        {
            if (!waterDecisionMade)
            {
                canCrossWaterThisSearch = Random.Range(0f, 100f) < waterCrossChance;
                waterDecisionMade = true;
            }

            if (!canCrossWaterThisSearch)
            {
                AbortHuntAndExplore();
                return;
            }
        }

        destination = targetBunny.transform.position;

        if (Vector3.Distance(transform.position, targetBunny.transform.position) < 0.2f)
            //Actualizar destino hacia la presa más cercana
            destination = targetBunny.transform.position;

        // PERSECUCIÓN EN PROCESO
        currentResistance -= resistanceLostSecondPursue * h;  // h es el tiempo de cada paso (1 segundo)
        //Cada segundo que persigue, pierde resistencia
        //Esto también se puede escribir sin la h

        pursueTimer += h; // h es el tiempo de cada paso (1 segundo)
        //Cada segundo que persigue, aumenta el contador de tiempo
        //Esto también se puede escribir como: pursueTimer++

        // VERIFICAR SI DEBE ABANDONAR POR CANSANCIO
        if (currentResistance <= 0 || pursueTimer >= PursueDuration) // Si el depredador se quedó sin resistencia (energía) o persiguió al conejo demasiado tiempo, entonces ...
        {
            AbandonPursue(); // Se rinde y abandona esa cacería
            return;
        }

        // Si alcanzó el conejo, pasará al método para comerselo
        if (Vector3.Distance(transform.position, targetBunny.transform.position) < 0.2f)
        {
            currentState = PredatorState.Eating;
        }
    }

    // ABANDONAR LA PERSECUCIÓN (CACERÍA)
    void AbandonPursue()
    {
        isResting = true; // Pasa a modo descanso
        restingTimer = 0f; // Inicialmente el contador de descanso está en 0
        currentPrey = null; // Olvida completamente al conejo por ahora
        //currentState = PredatorState.Exploring
        Debug.Log("El depredador abandonó la persecución por cansancio");
    }

    //MÉTODO DESCANSO O TIEMPO FUERA DEL DEPREDADOR
    void Rest()
    {
        restingTimer += h; // h es el tiempo de cada paso (1 segundo)
        //El contador del descanso va a aumentar
        //Esto también se puede escribir como: restingTimer++

        currentResistance += resistanceRecoveredWithResting * h; // h es el tiempo de cada paso (1 segundo)
        // Cada segundo que pasa, recupera resistencia
        //Esto también se puede escribir sin la h

        currentResistance = Mathf.Min(currentResistance, maxResistancePursue); // Para evitar que la resistencia supere el máximo

        if (restingTimer >= restingDuration) // Si el depredador ya descansó lo suficiente,...
        {
            isResting = false; // Volverá a estar activo
            Debug.Log("El depredador ha descansado y está listo para cazar");
        }
    }
    void Eat()
    {
        Collider2D foodHit = Physics2D.OverlapCircle(
            transform.position,
            0.2f,
            LayerMask.GetMask("Bunnies")
        );

        if (foodHit != null)
        {
            Bunny food = foodHit.GetComponent<Bunny>();
            if (food != null)
            {
                energy += food.age;

                // Reseteo del las variables de hambre progresiva
                speed = baseSpeed;
                timeWithoutFood = 0f;

                Destroy(food.gameObject);
            }
        }

        ResetSearch();
        currentState = PredatorState.Exploring;
    }

    void AbortHuntAndExplore()
    {
        ResetSearch();
        currentState = PredatorState.Exploring;
        ignoreFoodUntilExplorePoint = true;
        SelectNewDestination();
    }

    void ResetSearch()
    {
        targetBunny = null;
        waterDecisionMade = false;
        canCrossWaterThisSearch = false;
    }

    void SelectNewDestination()
    {
        Vector3 direction = new Vector3(
            Random.Range(-visionRange, visionRange),
            Random.Range(-visionRange, visionRange),
            0
        );

        Vector3 targetPoint = transform.position + direction;

        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            direction.normalized,
            visionRange,
            LayerMask.GetMask("Obstacles", "Water")
        );

        if (hit.collider != null)
        {
            float offset = transform.localScale.magnitude * 0.9f;
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
        // que se multiplica por la velocidad actual (que ya incluye el bonus de hambre progresiva).
        currentSpeed = speed * AgingCalculator.GetSpeedFactor(age, maxAge, maturityRatio, minSpeedFactor);

        Vector3 nextPosition = Vector3.MoveTowards(
            transform.position,
            destination,
            currentSpeed * h
        );

        Vector3 moveDir = nextPosition - transform.position;
        float moveDist = moveDir.magnitude;

        if (moveDist > 0.0001f)
        {
            int mask = LayerMask.GetMask("Obstacles");

            bool allowWater =
                currentState == PredatorState.SearchingFood &&
                canCrossWaterThisSearch;

            if (!allowWater)
            {
                mask |= LayerMask.GetMask("Water");
            }

            RaycastHit2D hit = Physics2D.Raycast(
                transform.position,
                moveDir.normalized,
                moveDist,
                mask
            );

            if (hit.collider != null)
            {
                float offset = 0.05f;
                nextPosition = (Vector3)hit.point - moveDir.normalized * offset;
            }
        }

        transform.position = nextPosition;

        // FEATURE Envejecimiento: el gasto de energia usa la velocidad real:
        // un depredador viejo y lento gasta menos energia al moverse.
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

    // FEATURE Eventos climaticos:
    // Devuelve el rango de vision real = vision base x multiplicador del clima.
    // Despejado = x1, Lluvia = x0.6, Tormenta = x0.3 (configurable en WeatherSystem).
    // Si no existe WeatherSystem, el multiplicador es 1 y todo funciona como antes.
    float GetEffectiveVision()
    {
        currentVision = visionRange * WeatherSystem.VisionMultiplier;
        return currentVision;
    }

    private void OnDrawGizmosSelected()
    {
        // FEATURE Eventos climaticos: el circulo verde muestra la vision real (afectada por el clima)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, Application.isPlaying ? GetEffectiveVision() : visionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(destination, 0.2f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, destination);
    }

    Bunny FindNearestBunny() // Busca al conejo mas cercano dentro del rango de vision, considerando obstaculos
    {
        // FEATURE Eventos climaticos: busca conejos solo dentro de la vision afectada por el clima
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, GetEffectiveVision(), LayerMask.GetMask("Bunnies"));
        Debug.Log($"Predator {name} encontró {hits.Length} colliders en su rango");
        Bunny nearest = null;
        float minDist = Mathf.Infinity;

        foreach (Collider2D hit in hits)// Se ejecuta para cada collider encontrado
        {
            Bunny bunny = hit.GetComponent<Bunny>();
            if (bunny == null) continue;

            Vector3 direction = bunny.transform.position - transform.position;
            float dist = direction.magnitude;

            RaycastHit2D obstacleHit = Physics2D.Raycast(
                transform.position,
                direction.normalized,
                dist,
                LayerMask.GetMask("Obstacles", "Water")
            );

            if (obstacleHit.collider != null)
                continue;

            if (dist < minDist)
            {
                minDist = dist;
                nearest = bunny;
            }
        }
        return nearest; //Retornando al conejo mas cercano que el depredador puede ver, si no hay niguno entonces manda un null
    }
}
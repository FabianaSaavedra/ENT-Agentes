using UnityEngine;
using System.Collections.Generic;

//Hacemos lo mismo de crear estados como en los conejos y los zorros, pero acá es con respecto a las estaciones del año
public enum Season
{
    Spring, //Primavera
    Summer, //Verano
    Autumn, //Otoño
    Winter //Invierno
}

public class FoodSpawner : MonoBehaviour
{

    //Configuración de apareción de la comida en el escenario
    [Header("Spawner Settings")]
    public GameObject foodPrefab; //El sprite de la comida que va a aparecer en el escenario
    public float spawnInterval = 0.5f; //Cada cuántos segundos en el tiempo va a aparecer comida en el escenario
    public int maxFood = 50; //Cantidad máxima de comida que puede haber en el escenario

    //Configuración de las estaciones
    [Header("Season Settings")]
    public Season currentSeason = Season.Spring; //Iniciamos en primavera
    public float secondsPerSeason = 10f; //Cada una dura 15 segundos
    private float seasonTimer = 0f; //Iniciamos en ceros
    private float currentInterval; //Intervalo que se usara con el tiempo real


    //Área en el que puede aparecer la comida
    [Header("Spawn Area (Rectangular)")]
    public Vector2 areaSize = new Vector2(20, 20); //Tamaño del área
    private float time = 0f;

    [Header("Zone status")] // Probabilidad para que el terreno sea fertil, árido o normal
    public float fertileZoneChance = 0.7f;
    public float aridZoneChance = 0.2f;
    public float normalZoneChance = 0.1f;

    // Lista en donde se guardan todas las posibles zonas
    private List<Zone> allZones = new List<Zone>();

    //Variable en la que se guarda la zona encontrada
    private Zone foundZone;

    private void Start()
    {
        currentInterval = spawnInterval; //Se inicia el intervalo de las estaciones con el que viene la simulacion
        // Cuando la simulación empieze, revisará toda el área para saber que terreno tiene y lo guardará
        allZones.AddRange(FindObjectsByType<Zone>(FindObjectsSortMode.InstanceID));
    }

    public void Simulate(float h)
    {
        //Esta parte sería de los cambios de estación
        seasonTimer += h;
        if (seasonTimer >= secondsPerSeason) //Si se cumple que ya paso el tiempo de cada estacion
        {
            seasonTimer = 0; //Se reinicia el contador para que cuente de nuevo en la próxima estación
            currentSeason = (Season)(((int)currentSeason + 1) % 4); //Como en el enum cada estación es un número (desde 0 hasta 3), entonces lo que le hace es sumarle 1 para pasar a la siguiente, y el módulo 4 hace que vuelva a empezar en primavera después de terminar en la estación invierno
            Debug.Log($"Estación actual: " + currentSeason); //Imprime en consola la estación actual
            UpdateInterval(); //Actualiza el intervalo de aparición de comida dependiendo de la estación actual
        }

        //Esta parte sería de la aparición de comida en el escenario
        time += h;
        if (time >= currentInterval)
        {
            time = 0f;
            if (CountFood() < maxFood)

            if (CountFood() < maxFood) // Si hay menos de 50 comidas...
            {
                SpawnFoodDependZone(); // Te dirigue al método de generador de comida segun la zona
                //SpawnFood();
            }
        }
    }

    //Acá configuramos qué tanto dura cada una de las estaciones
    void UpdateInterval()
    {
        switch (currentSeason)
        {
            case Season.Spring: //Si es primavera, dura un tiempo normal
                currentInterval = spawnInterval;
                break;
            case Season.Summer:
                currentInterval = spawnInterval * 2f; //Si es verano, se demora 2 veces más de lo normal en salir comida
                break;
            case Season.Autumn:
                currentInterval = spawnInterval * 3f; //Si es otoño, se demora 3 veces más de lo normal en salir comida
                break;
            case Season.Winter:
                currentInterval = spawnInterval * 4f; //Si es invierno, se demora 4 veces más de lo normal en salir comida
                break;
        }
    }
    void SpawnFoodDependZone() // GENERADOR DE COMIDA SEGÚN LA ZONA
    {
        Vector2 spawnPos = new Vector2( // Crear una variable para guardar la posición
            // Elige un número al azar para la posición de la comida
            Random.Range(-areaSize.x / 2f, areaSize.x / 2f),
            Random.Range(-areaSize.y / 2f, areaSize.y / 2f)
        );

        spawnPos += (Vector2)transform.position; // Suma la posición del generador de comida a la posición aleatoria

        Instantiate(foodPrefab, spawnPos, Quaternion.identity); 
        GetZoneAtPosition(spawnPos); // Se dirigue al método para detectar la zona

        float probability = normalZoneChance; // Mientras tanto, por defecto será determinada como zona normal (Está en el 10%)

        // Luego, al tener el dato de spawnpos, se corroboran los siguientes aspectos:
        // Primero, si la posición está en alguna zona especial
        if (foundZone != null)
        {
            // Segundo, Si la zona encontrada está señalada como fertil...
            if (foundZone.zonetype == Zone.ZoneType.Fertile)
            {
                probability = fertileZoneChance; // Entonces es señalada como probabilidad del 70%
            } 
            else if (foundZone.zonetype == Zone.ZoneType.Arid)// Si no, entonces Si la zona encontrada está señalada como arida...
            {
                probability = aridZoneChance; // Entonces es señalada como probabilidad del 20%
            }
        }

        if (Random.value <= probability) // Para saber si esa probabilidad se va a cumplir, se realiza este método para determinar si en esa zona aparecerá comida o no
        { 
            Instantiate(foodPrefab, spawnPos, Quaternion.identity); // Si el valor da dentro del porcentaje, entonces aparecerá comida
        } //Si no, no aparece nada en este intento
    }

    void GetZoneAtPosition(Vector2 position) // Verifica si la posición está dentro de alguna zona especial (arida o fertil)
    {
        foundZone = null; // Inicialmente esta zona no se encuentra registrada

        foreach (Zone zone in allZones) // Revisa cada zona guardada en la lista
        {
            Collider2D collider = zone.GetComponent<Collider2D>(); //Obtienes el collider de la zona

            if (collider != null && collider.OverlapPoint(position)) // Verifica, si tiene collider e indica que tipo de zona es, entonces...
            {
                foundZone = zone; // Guarda la zona encontrada
                return; // Salir del método ya que se tiene la información que necesitamos
            }
        }
    }

    int CountFood()
    {
        return FindObjectsByType<Food>(FindObjectsSortMode.InstanceID).Length;
    }

    private void OnDrawGizmosSelected() //Acá hace lo mismo que tiene el zorro y el conejo para saber el área en el que puede aparecer la comida
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(areaSize.x, areaSize.y, 1));
    }
}

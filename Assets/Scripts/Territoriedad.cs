using UnityEngine;

[RequireComponent(typeof(Predator))] //hace que el gameobgect tenga la clase pregator
public class Territoriedad : MonoBehaviour
{
    public Vector3 territoryCenter;
    public float territoryRadius = 5f;

    private Predator predator;// para controlar el script predator

    private void Start()
    {
        predator = GetComponent<Predator>(); //obtiene el componente predator
        territoryCenter = transform.position; //la posicion inicial se vuelve centro del territorio
    }

    private void Update()
    {
        if (!predator.isAlive) return; // Se ejecuta cada frame, Si está muerto no hace nada, Si está vivo revisa su territorio

        CheckTerritory();
    }

    void CheckTerritory()
    {
        float dist = Vector3.Distance(transform.position, territoryCenter); //Calcula qué tan lejos está del centro

        //  Si está cerca del límite o fuera
        if (dist > territoryRadius * 0.9f)
        {
            // Fuerza cambio de estado para que el Predator elija nuevo destino
            predator.currentState = PredatorState.Exploring;

            // Rota ligeramente al depredador hacia el centro
            Vector3 dir = (territoryCenter - transform.position).normalized;

            // Le da un empujon
            transform.position += dir * predator.speed * Time.deltaTime;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(territoryCenter, territoryRadius);
    }
}
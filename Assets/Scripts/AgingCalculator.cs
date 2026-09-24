using UnityEngine;

/// <summary>
/// FEATURE: Envejecimiento de depredadores.
/// Calcula qué porcentaje de su velocidad conserva un agente según su edad.
///
/// Lógica:
///  - Mientras el agente es joven (edad menor a la "edad de madurez")
///    conserva el 100% de su velocidad.
///  - Después de la madurez, la velocidad baja poco a poco (de forma lineal)
///    hasta llegar a un mínimo (minSpeedFactor) cuando alcanza su edad máxima.
///
/// Es una clase estática (no es un componente) para no tener que modificar
/// los prefabs; cualquier agente puede usarla.
/// </summary>
public static class AgingCalculator
{
    /// <param name="age">Edad actual del agente.</param>
    /// <param name="maxAge">Edad a la que el agente muere.</param>
    /// <param name="maturityRatio">Porcentaje de la vida (0 a 1) a partir del cual empieza a envejecer. Ej: 0.5 = a la mitad de su vida.</param>
    /// <param name="minSpeedFactor">Porcentaje mínimo de velocidad al llegar a maxAge. Ej: 0.3 = 30% de su velocidad.</param>
    /// <returns>Un número entre minSpeedFactor y 1 que se multiplica por la velocidad base.</returns>
    public static float GetSpeedFactor(float age, float maxAge, float maturityRatio, float minSpeedFactor)
    {
        // Evita dividir entre cero si maxAge está mal configurado
        if (maxAge <= 0f) return 1f;

        // Edad en la que empieza el envejecimiento
        float maturityAge = maxAge * maturityRatio;

        // Todavía es joven: velocidad completa
        if (age <= maturityAge) return 1f;

        // t va de 0 (recién madura) a 1 (edad máxima)
        float t = Mathf.InverseLerp(maturityAge, maxAge, age);

        // Interpolamos de 100% de velocidad hasta el mínimo
        return Mathf.Lerp(1f, minSpeedFactor, t);
    }
}
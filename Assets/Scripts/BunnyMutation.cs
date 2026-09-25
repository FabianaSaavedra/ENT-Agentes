using UnityEngine;

public class BunnyMutation : MonoBehaviour
{
    public static float Mutate(float value, float mutationRate)
    {
        float randomFactor = Random.Range(-mutationRate, mutationRate);
        return value + randomFactor;
    }

    public static void ApplyMutation(Bunny child, Bunny parent1, Bunny parent2)
    {
        float baseSpeed = (parent1.speed + parent2.speed) / 2f;
        float baseEnergy = (parent1.energy + parent2.energy) / 2f;
        float baseVision = (parent1.visionRange + parent2.visionRange) / 2f;

        child.speed = Mathf.Clamp(Mutate(baseSpeed, 0.3f), 0.5f, 5f);
        child.energy = Mathf.Clamp(Mutate(baseEnergy, 1f), 1f, 20f);
        child.visionRange = Mathf.Clamp(Mutate(baseVision, 0.5f), 1f, 10f);
    }
}
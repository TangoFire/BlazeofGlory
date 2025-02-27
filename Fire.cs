using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fire : MonoBehaviour
{
    // 🔥 Fire Properties
    public float intensity = 1.0f;  // Controls the strength/intensity of the fire
    public float spreadDelay = 1.0f;  // Delay before the fire attempts to spread again
    public float spreadRange = 1.0f;  // Fire spread distance (how far the fire can spread)
    public float extinguishThreshold = 0.1f;  // Threshold below which the fire is considered extinguished

    // 🌡️ Temperature Mechanics
    public float temperatureIncrease = 2f;  // Temperature increase per fire spawned
    public static float currentTemperature = 20f;  // Global room temperature, starts at room temp
    public static float maxTemperature = 100f;  // Temperature threshold for evacuation

    // 🔥 Fire Control
    private bool isSpreading = false;  // Prevents spreading while it's already in progress
    private bool isExtinguished = false;  // Tracks if the fire has been extinguished

    // 🔥 Visuals & Object References
    public GameObject fireParticlePrefab;  // The fire particle prefab to spawn
    private SpriteRenderer fireSpriteRenderer;  // The SpriteRenderer for fire visuals

    // 🔥 Room & Fire Limits
    public Collider2D roomCollider;  // Collider for the room's boundaries (used to limit spread area)
    public float maxFires = 10;  // The maximum number of fires allowed at once
    private static int currentFireCount = 0;  // Keeps track of how many fires are currently active
    private List<Vector2> previousFirePositions = new List<Vector2>();  // Tracks previously spawned fire positions

    void Start()
    {
        // Ensure the fire particle prefab is assigned and retrieve the sprite renderer
        if (fireParticlePrefab != null)
            fireSpriteRenderer = fireParticlePrefab.GetComponent<SpriteRenderer>();
        else
            Debug.LogError("🔥 Fire Particle Prefab is missing!");

        // Start fire spread and temperature increase routines
        StartCoroutine(SpreadFireRoutine());  // Begin the fire spread process
        StartCoroutine(IncreaseTemperatureOverTime());  // Begin increasing temperature over time
    }
// 🔥 Fire Spreading Routine with Variable Spread Chance
IEnumerator SpreadFireRoutine()
{
    while (!isExtinguished)  // Continue spreading until the fire is extinguished
    {
        if (!isSpreading)  // Check if a spread action is already in progress
        {
            isSpreading = true;

            // Introduce a random delay before attempting to spread again (0.1s delay as per your change)
            float randomDelay = 0.1f;
            yield return new WaitForSeconds(randomDelay);

            // Only spread fire if we haven't reached the max fire count
            if (currentFireCount < maxFires)
            {
                // Determine if this fire should attempt to spread (e.g., 15% chance)
                float spreadChance = Random.Range(0f, 1f);
                if (spreadChance <= 0.15f)  // 15% chance to spread each time (lower chance)
                {
                    Vector2 spawnPosition = GetNextFirePosition();  // Get the next valid position for fire to spawn
                    SpreadFireInRoom(spawnPosition);  // Spread fire at the calculated position
                }
                else
                {
                    // If fire doesn't spread, delay before the next check
                    yield return new WaitForSeconds(0.5f);  // Adjust delay if you want slower spreading
                }
            }
            else
            {
                // Wait for a longer time before attempting to spread again when max fires are reached
                yield return new WaitForSeconds(2f);  
            }

            isSpreading = false;  // Allow next spread attempt after delay
        }

        yield return null;  // Wait until the next frame
    }
}



    // 🔥 Calculate Next Fire Position
    Vector2 GetNextFirePosition()
    {
        // Get a random point within the room collider, slightly adjusting to simulate fire growing
        Vector2 randomPosition = new Vector2(
            Random.Range(roomCollider.bounds.min.x, roomCollider.bounds.max.x),
            Random.Range(roomCollider.bounds.min.y, roomCollider.bounds.max.y)
        );

        // Fire will tend to spread around areas where previous fires occurred, so we adjust here if needed
        if (previousFirePositions.Count > 0)
        {
            randomPosition = Vector2.Lerp(randomPosition, previousFirePositions[Random.Range(0, previousFirePositions.Count)], 0.5f);
        }

        return randomPosition;
    }

    // 🔥 Spread Fire in Room
    public void SpreadFireInRoom(Vector2 position)
    {
        // Ensure necessary references are set before attempting to spawn fire
        if (fireParticlePrefab == null || roomCollider == null)
        {
            Debug.LogError("🔥 Missing Fire Prefab or Room Collider!");
            return;
        }

        // Validate the fire position, ensuring it’s not too close to another fire
        if (!IsValidFirePosition(position))
        {
            Debug.Log($"🔥 Fire cannot spread to {position}, invalid position.");
            return;
        }

        // Spawn new fire at the valid position
        GameObject newFire = Instantiate(fireParticlePrefab, position, Quaternion.identity);
        Fire fireComponent = newFire.GetComponent<Fire>();

        if (fireComponent != null)
        {
            // Increase intensity slightly for each new spread to simulate fire growing
            fireComponent.StartFire(intensity * 1.1f);  
            IncreaseTemperature();  // Increase temperature due to new fire spreading
            currentFireCount++;  // Increment the fire count
            previousFirePositions.Add(position);  // Store the position of the new fire

            Debug.Log($"🔥 Fire spread to {position}. Total Fires: {currentFireCount}");
        }

        // Warn when the max fire count is reached
        if (currentFireCount >= maxFires)
            Debug.LogWarning("🔥 Maximum fire limit reached.");
    }

    // ✅ Check if Fire Can Spread Here (Validates if fire can spawn at this position)
    private bool IsValidFirePosition(Vector2 position)
    {
        // Check all colliders within the spread range of the position to prevent overlap
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(position, spreadRange);

        foreach (var hitCollider in hitColliders)
        {
            // If the position already has a fire, check how close it is
            if (hitCollider.gameObject.CompareTag("Fire"))
            {
                // Calculate the distance to the other fire
                float distance = Vector2.Distance(position, hitCollider.transform.position);

                // Relaxed threshold for fire proximity, allowing fires to spawn closer
                if (distance < spreadRange * 0.9f)  // Fires can spawn closer, but not too close
                    return false;  // Reject spawn if too close to another fire
            }
        }

        return true;  // Fire can spawn at this position
    }

    // 🔥 Start Fire & Update Visuals
    public void StartFire(float initialIntensity)
    {
        if (isExtinguished) return;  // Prevent fire start if it’s already extinguished

        intensity = initialIntensity;  // Set the fire intensity based on the spread logic
        UpdateFireVisuals();  // Update the visual appearance of the fire based on its intensity
        Debug.Log($"🔥 Fire started with intensity: {intensity}");
    }

    // 🔥 Update Fire Visuals Based on Intensity
    private void UpdateFireVisuals()
    {
        if (fireSpriteRenderer != null)
        {
            // Adjust the fire’s transparency and scale based on intensity
            fireSpriteRenderer.color = new Color(1f, 1f, 1f, intensity / 3f);
            fireSpriteRenderer.transform.localScale = Vector3.one * (0.5f + intensity * 0.2f);
        }
    }

    // 🌡️ Increase Temperature Over Time
    IEnumerator IncreaseTemperatureOverTime()
    {
        while (!isExtinguished)  // Continue until the fire is extinguished
        {
            // Increase temperature as long as there are fires
            if (currentFireCount > 0)
                IncreaseTemperature();

            // Increase temperature more quickly if there are more fires
            float delay = (currentFireCount > 5) ? 10f : 5f;  // Delay longer with more fires
            yield return new WaitForSeconds(delay);
        }
    }

    // 🌡️ Increase Temperature
    private void IncreaseTemperature()
    {
        currentTemperature += temperatureIncrease;  // Increase the temperature due to fire presence
        if (currentTemperature > maxTemperature)
            currentTemperature = maxTemperature;  // Prevent temperature from exceeding max limit

        Debug.Log($"🔥 Room temperature: {currentTemperature}°C");

        // Trigger evacuation if temperature exceeds the max threshold
        if (currentTemperature >= maxTemperature)
            GameManager.TriggerEvacuation();
    }

    // 🔥 Extinguish Fire and Remove it from the Game
   public void ExtinguishFire(float amount)
{
    // Only reduce intensity if not already extinguished
    if (isExtinguished) return; 

    // Reduce the fire intensity gradually
    intensity -= amount;
    
    // Prevent intensity from going below zero
    if (intensity < 0) intensity = 0f;

    // Check if the fire is completely extinguished
    if (intensity <= extinguishThreshold)
    {
        isExtinguished = true;  // Mark the fire as extinguished
        currentFireCount--;  // Decrease the total fire count

        // Optionally reduce room temperature over time
        Fire.currentTemperature -= amount;
        if (Fire.currentTemperature < 20f) Fire.currentTemperature = 20f;  // Prevent going below room temperature

        Debug.Log($"💦 Fire extinguished by {amount}!");

        Destroy(gameObject);  // Destroy the fire object
    }
    else
    {
        // If the fire is not yet extinguished, update visuals (you could change the visual appearance or emit smoke, etc.)
        UpdateFireVisuals();
    }
}

}

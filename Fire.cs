using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fire : MonoBehaviour
{
    // 🔥 Fire Properties
    public float intensity = 1.0f;  // Controls fire strength
    public float spreadDelay = 1.0f;  // Delay before spreading
    public float spreadRange = 2.0f;  // Fire spread distance
    public float extinguishThreshold = 0.1f;  // When fire dies out

    // 🌡️ Temperature Mechanics
    public float temperatureIncrease = 2f;  // Temperature increase per fire
    public static float currentTemperature = 20f;  // Global temperature
    public static float maxTemperature = 100f;  // Evacuation trigger

    // 🔥 Fire Control
    private bool isSpreading = false;
    private bool isExtinguished = false;

    // 🔥 Visuals & Object References
    public GameObject fireParticlePrefab;
    private SpriteRenderer fireSpriteRenderer;

    // 🔥 Room & Fire Limits
    public Collider2D roomCollider;
    public float maxFires = 10;  // Max fires that can exist at once
    private static int currentFireCount = 0;  // Current fire count
    private List<Vector2> previousFirePositions = new List<Vector2>();  // Tracks previous fire positions

    void Start()
    {
        // Ensure the fire particle prefab is assigned and retrieve the sprite renderer
        if (fireParticlePrefab != null)
            fireSpriteRenderer = fireParticlePrefab.GetComponent<SpriteRenderer>();
        else
            Debug.LogError("🔥 Fire Particle Prefab is missing!");

        // Start fire spread and temperature increase routines
        StartCoroutine(SpreadFireRoutine());
        StartCoroutine(IncreaseTemperatureOverTime());
    }

    // 🔥 Fire Spreading Routine
    IEnumerator SpreadFireRoutine()
    {
        while (!isExtinguished)
        {
            if (!isSpreading)
            {
                isSpreading = true;

                // Delay before attempting to spread the fire again
                yield return new WaitForSeconds(spreadDelay);

                // If we haven't reached the max fire count, try spreading fire
                if (currentFireCount < maxFires)
                {
                    Vector2 randomPosition = new Vector2(
                        Random.Range(roomCollider.bounds.min.x, roomCollider.bounds.max.x),
                        Random.Range(roomCollider.bounds.min.y, roomCollider.bounds.max.y)
                    );

                    // Attempt to spread fire at the random position
                    SpreadFireInRoom(randomPosition);
                }
                else
                {
                    // If max fires reached, wait longer before retrying
                    yield return new WaitForSeconds(2f);  // Buffer time to prevent rapid spawning
                }

                isSpreading = false;
            }

            yield return null;
        }
    }

    // 🔥 Spread Fire in Room
    public void SpreadFireInRoom(Vector2 position)
    {
        // Ensure necessary references are set
        if (fireParticlePrefab == null || roomCollider == null)
        {
            Debug.LogError("🔥 Missing Fire Prefab or Room Collider!");
            return;
        }

        // Check if fire position is valid (not too close to another fire)
        if (!IsValidFirePosition(position))
        {
            Debug.Log($"🔥 Fire cannot spread to {position}, invalid position.");
            return;
        }

        // Instantiate new fire at the valid position
        GameObject newFire = Instantiate(fireParticlePrefab, position, Quaternion.identity);
        Fire fireComponent = newFire.GetComponent<Fire>();

        if (fireComponent != null)
        {
            // Increase fire intensity slightly when spreading
            fireComponent.StartFire(intensity * 1.1f);
            IncreaseTemperature();  // Increase temperature due to new fire
            currentFireCount++;  // Increment fire count
            previousFirePositions.Add(position);  // Store fire position
        }

        Debug.Log($"🔥 Fire spread to {position}. Total Fires: {currentFireCount}");

        // Warn when the max fire count is reached
        if (currentFireCount >= maxFires)
            Debug.LogWarning("🔥 Maximum fire limit reached.");
    }

    // ✅ Check if Fire Can Spread Here
    private bool IsValidFirePosition(Vector2 position)
    {
        // Check all colliders within the spread range of the position
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
                    return false;  // Reject spawn if they are too close
            }
        }

        return true;  // Fire can spawn in this position
    }

    // 🔥 Start Fire & Update Visuals
    public void StartFire(float initialIntensity)
    {
        if (isExtinguished) return;  // Don't start fire if it's already extinguished

        intensity = initialIntensity;  // Set fire intensity
        UpdateFireVisuals();  // Update visual appearance of the fire
        Debug.Log($"🔥 Fire started with intensity: {intensity}");
    }

    // 🔥 Update Fire Visuals Based on Intensity
    private void UpdateFireVisuals()
    {
        if (fireSpriteRenderer != null)
        {
            // Adjust fire color and size based on intensity
            fireSpriteRenderer.color = new Color(1f, 1f, 1f, intensity / 3f);
            fireSpriteRenderer.transform.localScale = Vector3.one * (0.5f + intensity * 0.2f);
        }
    }

    // 🌡️ Increase Temperature Over Time
    IEnumerator IncreaseTemperatureOverTime()
    {
        while (!isExtinguished)
        {
            // Increase temperature as long as there are fires
            if (currentFireCount > 0)
                IncreaseTemperature();

            // Increase temperature every few seconds
            float delay = (currentFireCount > 5) ? 10f : 5f;  // Delay longer with more fires
            yield return new WaitForSeconds(delay);
        }
    }

    // 🌡️ Increase Temperature
    private void IncreaseTemperature()
    {
        currentTemperature += temperatureIncrease;
        if (currentTemperature > maxTemperature)
            currentTemperature = maxTemperature;

        Debug.Log($"🔥 Room temperature: {currentTemperature}°C");

        // Trigger evacuation if temperature is too high
        if (currentTemperature >= maxTemperature)
            GameManager.TriggerEvacuation();
    }

    // 🔥 Extinguish Fire and Remove it from the Game
    public void ExtinguishFire(float amount)
    {
        if (isExtinguished) return;  // Don't extinguish if already done

        isExtinguished = true;  // Mark as extinguished
        currentFireCount--;  // Reduce fire count

        // Reduce heat when fire is extinguished
        Fire.currentTemperature -= amount;
        if (Fire.currentTemperature < 20f) Fire.currentTemperature = 20f;  // Prevent temperature from going below room temperature

        Debug.Log($"💦 Fire extinguished by {amount}!");

        // Remove fire from scene if the temperature is sufficiently low
        if (Fire.currentTemperature <= 20f)
        {
            Destroy(gameObject);  // Destroy the fire game object
        }
    }
}

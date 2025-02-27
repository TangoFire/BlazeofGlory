using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fire : MonoBehaviour
{
    // 🔥 Fire Properties
    public float intensity = 1.0f;  // Controls fire strength (affects size, spread rate)
    public float spreadDelay = 1.0f;  // Delay before fire spreads
    public float spreadRange = 2.0f;  // Maximum range fire can spread
    public float extinguishThreshold = 0.1f;  // Minimum intensity before fire is destroyed

    // 🌡️ Temperature Mechanics
    public float temperatureIncrease = 2f;  // Increase in temperature when fire spreads
    public float maxTemperature = 100f;  // Maximum temperature cap
    private float currentTemperature = 20f;  // Starting temperature (room temp)

    // 🔥 Fire Control
    private bool isSpreading = false;  // Prevents multiple spread attempts
    private bool isExtinguished = false;  // Tracks if fire is put out

    // 🔥 Visual & Object References
    public GameObject fireParticlePrefab;  // Fire visual effect
    private SpriteRenderer fireSpriteRenderer;  // Fire's sprite renderer (for opacity/size)

    // 🌍 Room & Fire Limits
    public Collider2D roomCollider;  // Defines the room area for fire spread
    public float maxFires = 10;  // Limit for max fires in a room
    private static int currentFireCount = 0;  // Tracks active fires

    void Start()
    {
        // Validate fire particle prefab assignment
        if (fireParticlePrefab != null)
        {
            fireSpriteRenderer = fireParticlePrefab.GetComponent<SpriteRenderer>();
        }
        else
        {
            Debug.LogError("🔥 Fire Particle Prefab is missing! Assign one in the Inspector.");
        }

        // Start fire spreading mechanic
        StartCoroutine(SpreadFireRoutine());
    }

    // 🔥 Fire Spreading Mechanic
    IEnumerator SpreadFireRoutine()
    {
        while (!isExtinguished)
        {
            if (!isSpreading)
            {
                isSpreading = true;
                yield return new WaitForSeconds(spreadDelay);  // Wait before trying to spread

                if (currentFireCount < maxFires)
                {
                    SpreadFireInRoom();  // Attempt fire spread
                }

                isSpreading = false;
            }

            yield return null;
        }
    }

    // 🔥 Spread Fire in a Random Position Within the Room
    public void SpreadFireInRoom()
    {
        if (fireParticlePrefab == null || roomCollider == null)
        {
            Debug.LogError("🔥 Missing Fire Prefab or Room Collider! Cannot spread fire.");
            return;
        }

        // Pick a random position within the room bounds
        Vector2 randomPoint = new Vector2(
            Random.Range(roomCollider.bounds.min.x, roomCollider.bounds.max.x),
            Random.Range(roomCollider.bounds.min.y, roomCollider.bounds.max.y)
        );

        // Check if the position is valid for fire spread
        if (!IsValidFirePosition(randomPoint))
        {
            Debug.Log($"🔥 Fire cannot spread to {randomPoint}, position is invalid.");
            return;
        }

        // Instantiate a new fire at the position
        GameObject newFire = Instantiate(fireParticlePrefab, randomPoint, Quaternion.identity);
        Fire fireComponent = newFire.GetComponent<Fire>();

        if (fireComponent != null)
        {
            fireComponent.StartFire(intensity);
            IncreaseTemperature();  // Increase temperature as fire spreads
            currentFireCount++;
        }
        else
        {
            Debug.LogError("🔥 Failed to assign Fire component to new fire instance.");
        }

        Debug.Log($"🔥 Fire spread to {randomPoint}. Current Fire Count: {currentFireCount}");

        // Prevent excessive fire spreading
        if (currentFireCount >= maxFires)
        {
            Debug.LogWarning("🔥 Maximum fire limit reached. No more fires will be created.");
        }
    }

    // ✅ Check if Fire Can Spread to a Given Position
    private bool IsValidFirePosition(Vector2 position)
    {
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(position, spreadRange);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.gameObject.CompareTag("Fire"))
            {
                return false;  // Fire already exists in the area
            }
        }
        return true;  // Position is valid for fire spread
    }

    // 🔥 Initialize Fire Properties
    public void StartFire(float initialIntensity)
    {
        if (isExtinguished) return;

        intensity = initialIntensity;
        UpdateFireVisuals();
        Debug.Log($"🔥 Fire started with intensity: {intensity}");
    }

    // 💧 Extinguish Fire with Water
   public void ExtinguishFire(float extinguishAmount)
{
    if (isExtinguished) return;

    intensity -= extinguishAmount;

    if (intensity <= extinguishThreshold)
    {
        isExtinguished = true;
        currentFireCount--;

        if (fireParticlePrefab != null)
        {
            Destroy(fireParticlePrefab, 0.5f);  // Delay destruction slightly
        }

        Destroy(gameObject);  // Remove fire object
        Debug.Log("💧 Fire extinguished!");
    }
    else
    {
        UpdateFireVisuals();
    }
}


    // 🔥 Update Fire Visuals Based on Intensity
    private void UpdateFireVisuals()
    {
        if (fireSpriteRenderer != null)
        {
            fireSpriteRenderer.color = new Color(1f, 1f, 1f, intensity);  // Opacity scales with intensity
            fireSpriteRenderer.transform.localScale = Vector3.one * intensity;  // Fire grows/shrinks
        }
    }

    // 🌡️ Increase Room Temperature Due to Fire
    private void IncreaseTemperature()
    {
        currentTemperature += temperatureIncrease;
        if (currentTemperature > maxTemperature)
        {
            currentTemperature = maxTemperature;
        }
        Debug.Log($"🔥 Room temperature increased to {currentTemperature}°C");
    }

    // 💧 Handle Collision with Water Projectiles
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("WaterProjectile"))
        {
            WaterProjectile waterProjectile = collision.gameObject.GetComponent<WaterProjectile>();
            if (waterProjectile != null)
            {
                ExtinguishFire(waterProjectile.extinguishAmount);
                Destroy(collision.gameObject);  // Remove water projectile on impact
                Debug.Log("💧 Water projectile hit fire! Extinguishing...");
            }
        }
    }
}

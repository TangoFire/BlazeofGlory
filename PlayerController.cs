using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;  // Speed of player movement
    public float waterForce = 10f;  // Force of water stream
    public Transform firePoint;  // The point from which the water is sprayed
    [SerializeField] private GameObject waterStreamPrefab;  // Water stream prefab (assigned in Inspector)

    private ParticleSystem currentWaterStream;  // Reference to the active water stream

    public float dashSpeed = 15f;  // Speed while dashing
    public float dashDuration = 0.2f;  // Duration of dash
    public float dashCooldown = 1f;  // Cooldown between dashes
    private bool isDashing = false;  // Flag to check if player is dashing
   
    private float lastDashTime = -999f;  // Last time dash was performed

    private Vector2 moveDirection;  // The direction the player is moving
    private Rigidbody2D rb;  // Rigidbody2D component for player physics

    public float shootCooldown = 0.05f;  // Cooldown between shooting water streams
    bool isWaterStreamPaused = false;  // Flag to check if water stream is paused

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;  // Disable gravity for top-down movement
    }

    void Update()
    {
        HandleMovement();  // Handle movement input
        HandleWatering();  // Handle shooting and pausing the water stream
        HandleDash();  // Handle dash input

        // If water stream is active, update its behavior
        if (currentWaterStream != null)
        {
            UpdateWaterStream();
        }
    }

    // Handle player movement based on input
    void HandleMovement()
    {
        if (!isDashing)  // Prevent movement during dashing
        {
            moveDirection = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
            rb.linearVelocity = moveDirection * moveSpeed;  // Use velocity to move player
        }
    }

    // Handle the watering logic (shooting and pausing the stream)
    void HandleWatering()
    {
        if (Input.GetButtonDown("Fire1"))  // Mouse click pressed
        {
            ShootWater();  // Start shooting water
        }
        else if (Input.GetButtonUp("Fire1"))  // Mouse click released
        {
            PauseWaterStream();  // Pause the water stream
        }
    }

    // Rotate the firePoint to face the mouse position
    void RotateHoseToMouse()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0;  // Ensure the mouse position is on the same plane

        Vector2 direction = (mousePosition - firePoint.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;  // Calculate angle to rotate firePoint
        firePoint.rotation = Quaternion.Euler(new Vector3(0, 0, angle));  // Rotate firePoint towards the mouse
    }

    // Shoot water from the firePoint
    void ShootWater()
    {
        if (waterStreamPrefab == null)
        {
            Debug.LogError("🚨 Water stream prefab is not assigned!");
            return;  // Exit early if prefab is missing
        }

        // If the current water stream is null, invalid, or paused, reset or create a new particle system
        if (currentWaterStream == null || currentWaterStream.gameObject == null || isWaterStreamPaused)
        {
            Debug.LogWarning("💧 Water stream is invalid or paused, resetting...");

            // If the stream is paused, resume it
            if (isWaterStreamPaused)
            {
                ResumeWaterStream();
            }
            else
            {
                // If the stream doesn't exist, instantiate a new one
                if (currentWaterStream == null)
                {
                    Debug.Log("💧 Instantiating new water stream...");
                    currentWaterStream = Instantiate(waterStreamPrefab, firePoint.position, Quaternion.identity).GetComponent<ParticleSystem>();

                    // Ensure the ParticleSystem was properly retrieved from the prefab
                    if (currentWaterStream == null)
                    {
                        Debug.LogError("🚨 ParticleSystem could not be retrieved from water stream prefab!");
                        return;  // Exit if ParticleSystem is missing
                    }

                    // Attach the water stream to the firePoint
                    currentWaterStream.transform.SetParent(firePoint);
                }
                else
                {
                    // If the stream exists but is invalid, reset it
                    Debug.Log("💧 Resetting existing water stream...");
                    currentWaterStream.Clear();  // Clear the particle system
                    currentWaterStream.Play();  // Restart the particle system
                }
            }
        }

        // Rotate the firePoint to face the mouse direction
        RotateHoseToMouse();

        // Modify particle system properties for realistic water stream behavior
        var mainModule = currentWaterStream.main;

        // Debugging log for MainModule
        Debug.Log("💧 Modifying MainModule settings...");

        // Set speed, lifetime, gravity, and simulation space
        mainModule.startSpeed = waterForce * 3.5f;  // Boost speed to make water spray outward
        mainModule.startLifetime = 0.2f;  // Short lifetime for water particles
        mainModule.gravityModifier = 0.01f;  // Slight gravity effect to simulate water falling
        mainModule.simulationSpace = ParticleSystemSimulationSpace.World;  // Keep particles in world space

        var emissionModule = currentWaterStream.emission;
        emissionModule.rateOverTime = 250f;  // High emission rate for continuous stream

        var shapeModule = currentWaterStream.shape;
        shapeModule.shapeType = ParticleSystemShapeType.Cone;  // Narrow cone for focused stream
        shapeModule.angle = 5;  // Narrow angle for the cone
        shapeModule.radius = 0.02f;  // Small radius to keep the stream tight

        var velocityModule = currentWaterStream.velocityOverLifetime;
        velocityModule.enabled = true;
        velocityModule.space = ParticleSystemSimulationSpace.Local;  // Apply velocity in local space

        // Apply force based on direction
        velocityModule.x = new ParticleSystem.MinMaxCurve(1f * waterForce);
        velocityModule.y = new ParticleSystem.MinMaxCurve(1f * waterForce);

        var noiseModule = currentWaterStream.noise;
        noiseModule.enabled = true;
        noiseModule.strength = 0.15f;  // Add some random movement to the stream
        noiseModule.frequency = 2.5f;  // Control the frequency of noise for realism

        // Ensure the particle system is playing
        if (!currentWaterStream.isPlaying)
        {
            currentWaterStream.Play();  // Start playing the particle system
        }

        // Update the water stream position and behavior
        UpdateWaterStream();

        // Set the stream as not paused after it's been started or resumed
        isWaterStreamPaused = false;  // Mark the stream as not paused
    }

    // Pause the water stream, hiding the effect
    void PauseWaterStream()
    {
        if (currentWaterStream != null)
        {
            Debug.Log("💧 Pausing water stream...");

            // Stop the emission of particles by setting rateOverTime to 0
            var emissionModule = currentWaterStream.emission;
            emissionModule.rateOverTime = 0f;  // Set emission rate to 0 to stop particles

            // Optionally, hide the water stream by disabling its renderer
            var renderer = currentWaterStream.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = false;  // Disable the renderer to make the stream invisible
            }

            // Pause the particle system so it stops updating
            currentWaterStream.Pause();
            isWaterStreamPaused = true;  // Mark the stream as paused
        }
    }

    // Resume the water stream from pause, showing the effect
    void ResumeWaterStream()
    {
        if (currentWaterStream != null && isWaterStreamPaused)
        {
            Debug.Log("💧 Resuming water stream...");

            // Re-enable the renderer to show the stream again
            var renderer = currentWaterStream.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = true;  // Re-enable the renderer
            }

            // Restart the emission of particles
            var emissionModule = currentWaterStream.emission;
            emissionModule.rateOverTime = 250f;  // Set the emission rate to the desired value

            // Play the particle system to resume the water stream
            currentWaterStream.Play();

            // Mark the stream as not paused
            isWaterStreamPaused = false;
        }
    }

    // Update the water stream's position and behavior
    void UpdateWaterStream()
    {
        if (currentWaterStream == null) return;  // Exit if there's no water stream

        // Keep the stream following the firePoint
        currentWaterStream.transform.position = firePoint.position;

        // Get current mouse position
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0;  // Ensure mouse position is on the same plane

        // Calculate direction from firePoint to mouse position
        Vector2 direction = (mousePosition - firePoint.position).normalized;

        // Update stream direction and firePoint rotation
        currentWaterStream.transform.up = direction;
        RotateHoseToMouse();
    }

    // Handle dash input and movement
    void HandleDash()
    {
        if (Input.GetKeyDown(KeyCode.Space) && Time.time - lastDashTime >= dashCooldown)
        {
            StartCoroutine(Dash());  // Start the dash coroutine
            lastDashTime = Time.time;  // Update the last dash time
        }
    }

    // Coroutine to handle dash logic
    IEnumerator Dash()
    {
        isDashing = true;  // Set the dashing flag to true
        float dashStartTime = Time.time;

        // Dash in the direction the player is facing
        Vector2 dashDirection = moveDirection != Vector2.zero ? moveDirection : Vector2.up;

        // Apply dash movement
        while (Time.time < dashStartTime + dashDuration)
        {
            rb.linearVelocity = dashDirection * dashSpeed;  // Set dash velocity
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;  // Stop movement after dash duration
        isDashing = false;  // Reset the dashing flag
    }
}

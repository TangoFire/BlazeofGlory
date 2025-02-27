using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;  // Normal movement speed
    public float dashSpeed = 15f;  // Speed during dash
    public float dashDuration = 0.2f;  // Duration of the dash
    public float dashCooldown = 1f;  // Time between consecutive dashes
    private Vector2 moveDirection;  // Direction of movement
    private Rigidbody2D rb;  // Rigidbody to handle movement physics
    private bool isDashing = false;  // Flag to check if player is dashing
    private float lastDashTime = -999f;  // Tracks the time of the last dash

    [Header("Water Stream Settings")]
    public Transform firePoint;  // The point from which the water is fired
    [SerializeField] private GameObject waterStreamPrefab;  // Prefab of the water stream
    private ParticleSystem currentWaterStream;  // Current active water stream
    private bool isWaterStreamPaused = false;  // Whether the water stream is paused
    public float waterForce = 10f;  // Force of the water stream
    public float shootCooldown = 0.05f;  // Cooldown between water stream shots

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;  // Disable gravity for top-down movement
    }

    void Update()
    {
        HandleMovement();  // Handle player movement
        HandleWatering();  // Handle water stream actions
        HandleDash();  // Handle dash input

        if (currentWaterStream != null)
        {
            UpdateWaterStream();  // Update water stream if it's active
        }
    }

    // Handle player movement, preventing it during the dash
    private void HandleMovement()
    {
        if (!isDashing)  // Prevent movement during dashing
        {
            moveDirection = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
            rb.linearVelocity = moveDirection * moveSpeed;  // Use velocity for smoother movement
        }
    }

    // Handle water stream actions: shooting and pausing
    private void HandleWatering()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            ShootWater();  // Start shooting water
        }
        else if (Input.GetButtonUp("Fire1"))
        {
            PauseWaterStream();  // Pause the water stream
        }
    }

    // Rotate the firePoint to face the mouse position
    private void RotateHoseToMouse()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0;  // Keep z-axis at 0 for 2D view

        Vector2 direction = (mousePosition - firePoint.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        firePoint.rotation = Quaternion.Euler(new Vector3(0, 0, angle));  // Rotate firepoint to face the mouse
    }

    // Start the water stream or resume if paused
    private void ShootWater()
    {
        if (waterStreamPrefab == null)
        {
            Debug.LogError("🚨 Water stream prefab is not assigned!");
            return;
        }

        // If there's no current water stream or it's paused, create or resume the stream
        if (currentWaterStream == null || isWaterStreamPaused)
        {
            if (isWaterStreamPaused)
            {
                ResumeWaterStream();  // Resume if paused
            }
            else
            {
                CreateWaterStream();  // Create a new water stream
            }
        }

        ConfigureWaterStream();  // Configure the water stream's properties
    }

    // Create a new water stream if necessary
    private void CreateWaterStream()
    {
        // Instantiate the water stream and set its parent to the fire point
        currentWaterStream = Instantiate(waterStreamPrefab, firePoint.position, Quaternion.identity).GetComponent<ParticleSystem>();
        if (currentWaterStream == null)
        {
            Debug.LogError("🚨 ParticleSystem could not be retrieved from water stream prefab!");
            return;
        }
        currentWaterStream.transform.SetParent(firePoint);
    }

    // Configure the properties of the water stream (called every time water is shot)
    private void ConfigureWaterStream()
    {
        if (currentWaterStream == null) return;  // Ensure the particle system is valid

        // Configure the particle system's main properties
        var mainModule = currentWaterStream.main;
        mainModule.startSpeed = new ParticleSystem.MinMaxCurve(waterForce * 3.5f);
        mainModule.startLifetime = 0.2f;
        mainModule.gravityModifier = 0.01f;
        mainModule.simulationSpace = ParticleSystemSimulationSpace.World;

        // Set emission rate
        var emissionModule = currentWaterStream.emission;
        emissionModule.rateOverTime = 250f;

        // Define the shape of the water stream
        var shapeModule = currentWaterStream.shape;
        shapeModule.shapeType = ParticleSystemShapeType.Cone;
        shapeModule.angle = 5;
        shapeModule.radius = 0.02f;

        // Set the velocity of the water stream
        var velocityModule = currentWaterStream.velocityOverLifetime;
        velocityModule.enabled = true;
        velocityModule.space = ParticleSystemSimulationSpace.Local;
        velocityModule.x = new ParticleSystem.MinMaxCurve(1f * waterForce);
        velocityModule.y = new ParticleSystem.MinMaxCurve(1f * waterForce);

        // Add noise to the stream for randomness
        var noiseModule = currentWaterStream.noise;
        noiseModule.enabled = true;
        noiseModule.strength = 0.15f;
        noiseModule.frequency = 2.5f;

        // Start playing the water stream if it's not already playing
        if (!currentWaterStream.isPlaying)
        {
            currentWaterStream.Play();
        }
    }

    // Pause the water stream (disables emission and stops the system)
    private void PauseWaterStream()
    {
        if (currentWaterStream != null)
        {
            // Disable the emission of particles
            var emissionModule = currentWaterStream.emission;
            emissionModule.rateOverTime = 0f;

            // Stop rendering the water stream (makes it invisible)
            var renderer = currentWaterStream.GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = false;

            // Stop the particle system
            currentWaterStream.Stop();
            isWaterStreamPaused = true;
        }
    }

    // Resume the water stream (enables emission and makes it visible again)
    private void ResumeWaterStream()
    {
        if (currentWaterStream != null && isWaterStreamPaused)
        {
            // Re-enable rendering the water stream
            var renderer = currentWaterStream.GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = true;

            // Resume the emission of particles
            var emissionModule = currentWaterStream.emission;
            emissionModule.rateOverTime = 250f;

            // Play the particle system
            currentWaterStream.Play();
            isWaterStreamPaused = false;
        }
    }

    // Update the water stream's position and direction to follow the mouse
    private void UpdateWaterStream()
    {
        if (currentWaterStream == null) return;

        // Update the position of the water stream to follow the fire point
        currentWaterStream.transform.position = firePoint.position;

        // Rotate the stream to face the mouse position
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0;  // Ensure the z-axis stays 0 for 2D movement

        Vector2 direction = (mousePosition - firePoint.position).normalized;
        currentWaterStream.transform.up = direction;
        RotateHoseToMouse();  // Update hose direction
    }

    // Handle player dash input and movement
    private void HandleDash()
    {
        // Dash when the player presses the spacebar (if cooldown allows)
        if (Input.GetKeyDown(KeyCode.Space) && Time.time - lastDashTime >= dashCooldown)
        {
            StartCoroutine(Dash());
            lastDashTime = Time.time;
        }
    }

    // Coroutine to perform the dash action
    private IEnumerator Dash()
    {
        isDashing = true;
        float dashStartTime = Time.time;

        Vector2 dashDirection = moveDirection != Vector2.zero ? moveDirection : Vector2.up;

        while (Time.time < dashStartTime + dashDuration)
        {
            // Dash the player in the specified direction
            rb.linearVelocity = dashDirection * dashSpeed;
            yield return null;
        }

        // Stop the dash after the duration
        rb.linearVelocity = Vector2.zero;
        isDashing = false;
    }
}

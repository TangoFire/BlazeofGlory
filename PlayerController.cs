using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float waterForce = 10f;
    public Transform firePoint;
        [SerializeField] private GameObject waterStreamPrefab;  // Ensure this is assigned in the Inspector

    private ParticleSystem currentWaterStream; // Store reference to the active water stream

    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    private bool isDashing = false;
    private float dashTime = 0f;
    private float lastDashTime = -999f;

    private Vector2 moveDirection;
    private Rigidbody2D rb;

    public float shootCooldown = 0.05f; // Faster shooting for a smooth stream
    

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
    }

  void Update()
{
    HandleMovement();
    HandleWatering();
    HandleDash();

    if (currentWaterStream != null) // Ensure the stream follows the firePoint
    {
        UpdateWaterStream();
    }
}


    void HandleMovement()
    {
        if (!isDashing)
        {
            moveDirection = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
            rb.linearVelocity = moveDirection * moveSpeed;
        }
    }

void HandleWatering()
{
    if (Input.GetButtonDown("Fire1")) // Mouse click pressed
    {
        ShootWater();
    }
    else if (Input.GetButtonUp("Fire1")) // Mouse click released
    {
        StopWater();
    }
}


    void RotateHoseToMouse()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0;

        Vector2 direction = (mousePosition - firePoint.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        firePoint.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
    }

void ShootWater()
{
    // 🔍 Check if waterStreamPrefab is assigned
    if (waterStreamPrefab == null)
    {
        Debug.LogError("🚨 Water stream prefab is not assigned!");
        return; // Exit if prefab is missing
    }

    // If the current water stream is null or invalid, create or reset the particle system
    if (currentWaterStream == null || currentWaterStream.gameObject == null)
    {
        Debug.LogWarning("💧 Water stream is invalid, resetting...");
        
        // Instantiate the water stream if it's null (the first time it's created)
        if (currentWaterStream == null)
        {
            currentWaterStream = Instantiate(waterStreamPrefab, transform.position, Quaternion.identity).GetComponent<ParticleSystem>();
        }
        else
        {
            // Reset the particle system if it is invalid (e.g., destroyed)
            currentWaterStream.Clear(); // Clears the particle system
            currentWaterStream.Play(); // Restarts the particle system
        }
    }

    // Rotate the firePoint to face the mouse
    Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    mousePosition.z = 0;  // Ensure 2D vector
    Vector2 direction = (mousePosition - firePoint.position).normalized;
    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    firePoint.rotation = Quaternion.Euler(new Vector3(0, 0, angle));

    // Modify the particle system settings to ensure proper behavior
    var mainModule = currentWaterStream.main;
    mainModule.startSpeed = waterForce * 3.5f;  // Boost speed to make it fly outward
    mainModule.startLifetime = 0.2f; // Short lifetime for realistic water stream
    mainModule.gravityModifier = 0.01f; // Very small gravity to make it fall gradually
    mainModule.simulationSpace = ParticleSystemSimulationSpace.World; // Keeps particles independent of player movement

    var emissionModule = currentWaterStream.emission;
    emissionModule.rateOverTime = 250f; // High rate for continuous water stream

    var shapeModule = currentWaterStream.shape;
    shapeModule.shapeType = ParticleSystemShapeType.Cone; // Direct the stream in one direction
    shapeModule.angle = 5; // Narrow cone for a focused stream
    shapeModule.radius = 0.02f; // Tight radius to prevent spreading

    var velocityModule = currentWaterStream.velocityOverLifetime;
    velocityModule.enabled = true;
    velocityModule.space = ParticleSystemSimulationSpace.Local; // Apply velocity correctly in local space
    velocityModule.x = new ParticleSystem.MinMaxCurve(direction.x * waterForce * 3); // Boost forward velocity
    velocityModule.y = new ParticleSystem.MinMaxCurve(direction.y * waterForce * 3); // Ensure proper travel towards the mouse

    var noiseModule = currentWaterStream.noise;
    noiseModule.enabled = true;
    noiseModule.strength = 0.15f; // Adds randomness to simulate turbulence
    noiseModule.frequency = 2.5f; // Adjust frequency of noise for realism

    currentWaterStream.Play(); // Start playing the water stream particle system
    UpdateWaterStream(); // Any additional updates for the stream (e.g., controlling particle behavior over time)
}


void StopWater()
{
    if (currentWaterStream != null)
    {
        Debug.Log("💧 Stopping water stream...");
        currentWaterStream.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        Destroy(currentWaterStream.gameObject, 0.5f);
        StartCoroutine(ResetWaterStream(0.5f));
    }
}

IEnumerator ResetWaterStream(float delay)
{
    yield return new WaitForSeconds(delay); 
    Debug.Log("💧 Water stream fully destroyed. Resetting reference...");
    currentWaterStream = null;
}


void UpdateWaterStream()
{
    if (currentWaterStream == null) return;

    // Keep the stream moving with the player
    currentWaterStream.transform.position = firePoint.position;

    // Get current mouse position
    Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    mousePosition.z = 0;

    // Calculate direction
    Vector2 direction = (mousePosition - firePoint.position).normalized;

    // **Apply rotation directly to the water stream instead of firePoint**
   // float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
   // currentWaterStream.transform.rotation = Quaternion.Euler(0, 0, angle);

     // **Fix: Apply rotation to BOTH firePoint and water stream**
    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    firePoint.rotation = Quaternion.Euler(0, 0, angle);
    currentWaterStream.transform.rotation = Quaternion.Euler(0, 0, angle);

    // Adjust velocity over lifetime to match the new direction
    var velocityModule = currentWaterStream.velocityOverLifetime;
    velocityModule.x = new ParticleSystem.MinMaxCurve(direction.x * waterForce * 3);
    velocityModule.y = new ParticleSystem.MinMaxCurve(direction.y * waterForce * 3);
}


    void HandleDash()
    {
        if (Time.time - lastDashTime >= dashCooldown && Input.GetKeyDown(KeyCode.Space))
        {
            StartDash();
        }

        if (isDashing)
        {
            dashTime -= Time.deltaTime;
            if (dashTime <= 0f)
            {
                EndDash();
            }
        }
    }

    void StartDash()
    {
        if (moveDirection.magnitude > 0)
        {
            isDashing = true;
            dashTime = dashDuration;
            lastDashTime = Time.time;
            rb.linearVelocity = moveDirection * dashSpeed;
        }
    }

    void EndDash()
    {
        isDashing = false;
        rb.linearVelocity = Vector2.zero;
    }
}

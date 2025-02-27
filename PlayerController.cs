using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    private Vector2 moveDirection;
    private Rigidbody2D rb;
    private bool isDashing = false;
    private float lastDashTime = -999f;

    [Header("Water Stream Settings")]
    public Transform firePoint;
    [SerializeField] private GameObject waterStreamPrefab;
    private ParticleSystem currentWaterStream;
    private bool isWaterStreamPaused = false;
    public float waterForce = 10f;
    public float shootCooldown = 0.05f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;  // Disable gravity for top-down movement
    }

    void Update()
    {
        HandleMovement();
        HandleWatering();
        HandleDash();

        if (currentWaterStream != null)
        {
            UpdateWaterStream();
        }
    }

    // Handle player movement
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
        mousePosition.z = 0;

        Vector2 direction = (mousePosition - firePoint.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        firePoint.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
    }

    // Start the water stream or resume if paused
    private void ShootWater()
    {
        if (waterStreamPrefab == null)
        {
            Debug.LogError("🚨 Water stream prefab is not assigned!");
            return;
        }

        if (currentWaterStream == null || isWaterStreamPaused)
        {
            if (isWaterStreamPaused)
            {
                ResumeWaterStream();
            }
            else
            {
                CreateWaterStream();
            }
        }

        RotateHoseToMouse();
        ConfigureWaterStream();
    }

    // Create a new water stream if necessary
    private void CreateWaterStream()
    {
        currentWaterStream = Instantiate(waterStreamPrefab, firePoint.position, Quaternion.identity).GetComponent<ParticleSystem>();
        if (currentWaterStream == null)
        {
            Debug.LogError("🚨 ParticleSystem could not be retrieved from water stream prefab!");
            return;
        }
        currentWaterStream.transform.SetParent(firePoint);
    }

    // Configure the properties of the water stream
    private void ConfigureWaterStream()
    {
        var mainModule = currentWaterStream.main;
        mainModule.startSpeed = new ParticleSystem.MinMaxCurve(waterForce * 3.5f);
        mainModule.startLifetime = 0.2f;
        mainModule.gravityModifier = 0.01f;
        mainModule.simulationSpace = ParticleSystemSimulationSpace.World;

        var emissionModule = currentWaterStream.emission;
        emissionModule.rateOverTime = 250f;

        var shapeModule = currentWaterStream.shape;
        shapeModule.shapeType = ParticleSystemShapeType.Cone;
        shapeModule.angle = 5;
        shapeModule.radius = 0.02f;

        var velocityModule = currentWaterStream.velocityOverLifetime;
        velocityModule.enabled = true;
        velocityModule.space = ParticleSystemSimulationSpace.Local;
        velocityModule.x = new ParticleSystem.MinMaxCurve(1f * waterForce);
        velocityModule.y = new ParticleSystem.MinMaxCurve(1f * waterForce);

        var noiseModule = currentWaterStream.noise;
        noiseModule.enabled = true;
        noiseModule.strength = 0.15f;
        noiseModule.frequency = 2.5f;

        if (!currentWaterStream.isPlaying)
        {
            currentWaterStream.Play();
        }
    }

    // Pause the water stream
    private void PauseWaterStream()
    {
        if (currentWaterStream != null)
        {
            var emissionModule = currentWaterStream.emission;
            emissionModule.rateOverTime = 0f;

            var renderer = currentWaterStream.GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = false;

            currentWaterStream.Pause();
            isWaterStreamPaused = true;
        }
    }

    // Resume the water stream after being paused
    private void ResumeWaterStream()
    {
        if (currentWaterStream != null && isWaterStreamPaused)
        {
            var renderer = currentWaterStream.GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = true;

            var emissionModule = currentWaterStream.emission;
            emissionModule.rateOverTime = 250f;

            currentWaterStream.Play();
            isWaterStreamPaused = false;
        }
    }

    // Update the water stream's position and behavior
    private void UpdateWaterStream()
    {
        if (currentWaterStream == null) return;

        currentWaterStream.transform.position = firePoint.position;

        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0;

        Vector2 direction = (mousePosition - firePoint.position).normalized;
        currentWaterStream.transform.up = direction;
        RotateHoseToMouse();
    }

    // Handle player dash input and movement
    private void HandleDash()
    {
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
            rb.linearVelocity = dashDirection * dashSpeed;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        isDashing = false;
    }
}

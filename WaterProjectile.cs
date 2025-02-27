using UnityEngine;

public class WaterProjectile : MonoBehaviour
{
    public float waterSpeed = 10f; // Speed of water stream
    public float lifetime = 3f; // Lifetime of the water stream before being destroyed
    public float extinguishAmount = 0.25f; // How much fire intensity is reduced on collision

    private Rigidbody2D rb;

    private void Start()
    {
        // Destroy the water stream after a set time
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Move the water forward
        transform.Translate(Vector2.up * waterSpeed * Time.deltaTime);
    }

    // Detect collision with fire
    // Check if the projectile hits something with the tag "Fire"
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Fire"))
        {
            Fire fire = collision.gameObject.GetComponent<Fire>();
            
            if (fire != null)
            {
                fire.ExtinguishFire(extinguishAmount);
                PlayWaterSplashEffect(collision.contacts[0].point);
                Destroy(gameObject);
            }
            else
            {
                Debug.LogError("🚨 Fire component NOT FOUND on object tagged as 'Fire': " + collision.gameObject.name);
            }
        }
    }

    // Optionally add a method to play visual/audio effects when the water hits the fire
    void PlayWaterSplashEffect(Vector2 position)
    {
        // Example: Play sound effect
        // AudioManager.PlaySound("WaterSplash");

        // Example: Instantiate a particle effect at the position of collision
        // Instantiate(waterSplashParticle, position, Quaternion.identity);
    }
}

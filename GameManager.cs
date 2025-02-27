using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // References to active fires in the scene
    private List<Fire> activeFires = new List<Fire>();

    // Current temperature in the room
    public float currentTemperature = 20f;  // Room temperature starts at 20°C
    

    void Start()
    {
        // Find all fires in the scene at the start of the game
        FindAllFires();
    }

    void Update()
    {
        // Update the temperature based on all fires
        UpdateRoomTemperature();


        // Optionally, perform actions based on temperature
        HandleTemperatureEffects();
    }

    // Find all fire objects in the scene and add them to the active fires list
    void FindAllFires()
    {
        Fire[] fireObjects = Object.FindObjectsByType<Fire>(FindObjectsSortMode.None);  // Use the new method for finding objects
        activeFires = new List<Fire>(fireObjects);
    }

    // Update the current room temperature based on active fires
    void UpdateRoomTemperature()
    {
        currentTemperature = 20f;  // Reset temperature to a base value (room temperature)

        // Loop through each fire in the scene and increase the temperature
        foreach (Fire fire in activeFires)
        {
            // Increase the room temperature based on the fire's spread and intensity
            currentTemperature += fire.temperatureIncrease;
        }

        // Ensure the temperature doesn't exceed the maximum limit (e.g., 100°C)
        if (currentTemperature > 100f)
        {
            currentTemperature = 100f;
        }
    }

    // Handle events triggered by high temperature
    void HandleTemperatureEffects()
    {
        // Example: If the temperature exceeds a threshold, trigger certain events
        if (currentTemperature > 50f)
        {
            // Maybe cause more fires to start or damage structures
            Debug.Log("Warning! High temperature! More danger approaching!");
        }

        // Handle other possible game events (e.g., collapsing structures)
        if (currentTemperature > 75f)
        {
            // Trigger an emergency situation, like the room being completely engulfed
            TriggerEmergencyEvacuation();
        }
    }

    // Handle emergency actions (for example, fire spreading uncontrollably)
    void TriggerEmergencyEvacuation()
    {
        // Here we could add logic for triggering certain actions when the room is too hot
        Debug.Log("Emergency! The room is too hot! Take immediate action!");

        // You can add other game mechanics here, like doors being locked or certain exits being blocked
    }

    // Optional: You could have a method to manually trigger fire spread for testing purposes
    public void TriggerFireSpread()
    {
        foreach (Fire fire in activeFires)
        {
            fire.SpreadFireInRoom();  // Trigger the fire spread in each active fire (make sure SpreadFireInRoom is public)
        }
    }
}

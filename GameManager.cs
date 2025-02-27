using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public List<Fire> activeFires = new List<Fire>();
    public static float currentTemperature = 20f;
    public float maxTemperature = 100f;
    
    private bool evacuationTriggered = false;

    void Awake()
    {
        if (instance == null)
            instance = this;
    }

    void Update()
    {
        currentTemperature = Fire.currentTemperature;

        if (currentTemperature >= 80f && currentTemperature < 90f)
            Debug.LogWarning("🔥 Danger! Temperature is rising! Evacuate soon!");

        if (currentTemperature >= 90f && currentTemperature < maxTemperature)
            Debug.LogWarning("🔥 WARNING: Near Critical Heat Levels!");

        if (currentTemperature >= maxTemperature && !evacuationTriggered)
            TriggerEvacuation();
    }

    public static void TriggerEvacuation()
    {
        if (instance.evacuationTriggered) return;

        instance.evacuationTriggered = true;
        Debug.LogError("🚨 EVACUATION TRIGGERED! TEMPERATURE TOO HIGH! 🚨");

        instance.StartCoroutine(instance.EvacuationCountdown());
    }

    IEnumerator EvacuationCountdown()
    {
        yield return new WaitForSeconds(30f);
        Debug.LogError("🚨 FINAL EVACUATION! GAME OVER! 🚨");
        // Here you can add logic to end the game or show an evacuation screen.
    }
}

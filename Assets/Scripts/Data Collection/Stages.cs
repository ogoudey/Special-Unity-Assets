using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using ViveSR.anipal.Eye;


[System.Serializable]
public class StagePoint
{
    public Transform transform;
    public int period;
}

public class Stages : MonoBehaviour
{
    public List<StagePoint> stagePoints = new List<StagePoint>();
    private GameObject player;

    public bool _WaitForCalibration = true;
    public void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player"); // Get the player object

        if (player != null)
        {
            if (_WaitForCalibration)
            {
                StartCoroutine(WaitForCalibration());
            }
            else
            {
               StartCoroutine(MoveThroughStages()); 
            }
        }
        else
        {
            UnityEngine.Debug.LogError("Player not found!");
        }
    }

    private IEnumerator WaitForCalibration()
    {
        // Wait until EyeTrackingManager.instance is not null and calibrated is true
        while (EyeTrackingManager.instance == null || !EyeTrackingManager.instance.isCalibrated())
        {
            // Optionally log or add a delay to avoid overloading the frame rate
            yield return null;  // Wait until next frame
        }

        // Once calibrated, start moving through the stages
        StartCoroutine(MoveThroughStages());
    }

    private IEnumerator MoveThroughStages()
    {
        foreach (var stagePoint in stagePoints)
        {
            // Move the player to the next stagePoint
            player.transform.position = stagePoint.transform.position;

            // Log the current stage for debugging
            UnityEngine.Debug.Log($"Moving to: {stagePoint.transform}, waiting for {stagePoint.period} seconds.");

            // Wait for the specified time
            yield return new WaitForSeconds(stagePoint.period);
        }
        UnityEditor.EditorApplication.isPlaying = false;
    }
    
}
using UnityEngine;

public class VerticalMixer : MonoBehaviour
{
    private AudioSource audioSource;  // Reference to the Audio Source
    private AudioListener audioListener;  // Reference to the AudioListener
    public float maxHeight = 30f;    // Max height where the sound is loudest
    private float minHeight = 0f;     // Min height where the sound is quietest
    private float maxVolume = 1f;     // Max volume at maxHeight
    private float minVolume = 0.0f;   // Min volume at minHeight

    void Start()
    {
        // Get the AudioSource component attached to this GameObject
        audioSource = GetComponent<AudioSource>();

        // Get the AudioListener component attached to the Player (assuming it's on the Player)
        audioListener = Camera.main.GetComponent<AudioListener>(); // Assuming the AudioListener is on the Main Camera
    }

    void Update()
    {
        if (audioListener != null && audioSource != null)
        {
            // Get the AudioListener's (Player's) Y position
            float listenerHeight = audioListener.transform.position.y;

            // Clamp the listener height within the minHeight and maxHeight range
            listenerHeight = Mathf.Clamp(listenerHeight, minHeight, maxHeight);

            // Calculate the volume based on the height
            float volume = Mathf.Lerp(minVolume, maxVolume, (listenerHeight - minHeight) / (maxHeight - minHeight));
            UnityEngine.Debug.Log($"Setting volume to {volume}");
            // Apply the calculated volume to the AudioSource
            audioSource.volume = volume;
        }
        else
        {
            UnityEngine.Debug.Log($"No listener or audiosource"); 
        }
    }
}
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.UI;
using System.Collections;

using TMPro;
public class DollHeadRotator : MonoBehaviour
{
    [Header("Doll Settings")]
    [SerializeField] private Transform dollHead;
    [SerializeField] private float rotationSpeed = 90f; // Degrees per second
    [SerializeField] private bool isRotatingToFront = true;
    [SerializeField] private float frontAngle = 0;
    [SerializeField] private float backAngle = 180f;

    [Header("Game Timing")]
    [SerializeField] private float minGreenLightTime = 3f;
    [SerializeField] private float maxGreenLightTime = 8f;
    [SerializeField] private float minRedLightTime = 2f;
    [SerializeField] private float maxRedLightTime = 5f;

    [Header("Game Timer")]
    [SerializeField] private float gameTimeInSeconds = 120f; // 2 minutes
    [SerializeField] private TextMeshProUGUI timerText; // UI Text to display the timer
    [SerializeField] private AudioClip timerEndSound; // Sound to play when timer ends
    [SerializeField] private AudioClip timerTickSound; // Optional tick sound for last 10 seconds
    private float currentGameTime;
    private bool isTimerActive = false;

    [Header("Auto Start")]
    [SerializeField] private bool autoStartGame = true;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip greenLightSound;
    [SerializeField] private AudioClip redLightSound;
    [SerializeField] private AudioClip rotationSound;
    [SerializeField] private AudioClip elimination;
    [SerializeField] private AudioClip gunshot;

    [Header("Movement Detection")]
    [SerializeField] private float movementSensitivity = 0.01f; // How sensitive the movement detection is
    [SerializeField] private float rotationSensitivity = 0.5f; // How sensitive the rotation detection is
    [SerializeField] private float graceTime = 0.5f; // Short grace period when light changes to red

    [Header("References")]
    [SerializeField] public GameObject gameover_Panel;
    [SerializeField] public ARSessionOrigin arSessionOrigin;

    private Quaternion targetRotation;
    private bool isRotating = false;
    private float lightChangeTimer = 0f;
    private bool isGreenLight = false;
    private bool gameActive = false;
    private Camera arCamera;

    // Movement tracking variables
    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private float redLightStartTime;

    // Timer tick sound coroutine reference
    private Coroutine tickSoundCoroutine;

    void Start()
    {
        // Get AR Camera
        if (arSessionOrigin != null)
        {
            arCamera = arSessionOrigin.camera;
        }
        else
        {
            arCamera = Camera.main;
            Debug.LogWarning("AR Session Origin not assigned, using main camera instead.");
        }

        // Initialize tracking variables
        if (arCamera != null)
        {
            lastPosition = arCamera.transform.position;
            lastRotation = arCamera.transform.rotation;
        }

        // Initialize doll head to front position
        if (dollHead != null)
        {
            dollHead.localRotation = Quaternion.Euler(0, 0, frontAngle);
        }

        // Setup audio source if needed
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                Debug.Log("Created new AudioSource component");
            }
        }

        // Initialize game timer
        currentGameTime = gameTimeInSeconds;
        UpdateTimerDisplay();

        // Auto-start the game if enabled
        if (autoStartGame)
        {
            StartGame();
        }
    }

    public void StartGame()
    {
        gameActive = true;
        isTimerActive = true;
        currentGameTime = gameTimeInSeconds; // Reset timer to full time
        UpdateTimerDisplay();

        if (gameover_Panel != null)
        {
            gameover_Panel.SetActive(false);
        }

        // Start tick sound coroutine if timer sound is assigned
        if (timerTickSound != null)
        {
            if (tickSoundCoroutine != null)
            {
                StopCoroutine(tickSoundCoroutine);
            }
            tickSoundCoroutine = StartCoroutine(PlayTimerTickSounds());
        }

        // Start with green light
        SetGreenLight();

        // Begin rotation immediately
        RotateHead();
    }

    public void StopGame()
    {
        gameActive = false;
        isTimerActive = false;

        // Stop tick sound coroutine if it's running
        if (tickSoundCoroutine != null)
        {
            StopCoroutine(tickSoundCoroutine);
            tickSoundCoroutine = null;
        }
    }

    // Call this to manually trigger rotation
    public void RotateHead()
    {
        if (!isRotating && dollHead != null)
        {
            isRotatingToFront = !isRotatingToFront;
            float targetAngle = isRotatingToFront ? frontAngle : backAngle;
            targetRotation = Quaternion.Euler(0, 0, targetAngle);
            isRotating = true;

            // Only play rotation sound when turning from back to front
            // (which happens during red light)
            if (isRotatingToFront && !isGreenLight)
            {
                PlaySound(rotationSound);
            }

            Debug.Log("RotateHead called. Target Angle: " + targetAngle + ", RotatingToFront: " + isRotatingToFront);
        }
    }

    // Centralized sound playing function
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            // Stop any currently playing sound
            audioSource.Stop();

            // Set the new clip and play it
            audioSource.clip = clip;
            audioSource.Play();

            Debug.Log("Playing sound: " + clip.name);
        }
        else
        {
            if (clip == null)
                Debug.LogWarning("Attempted to play null audio clip");
            if (audioSource == null)
                Debug.LogError("AudioSource component is missing");
        }
    }

    // For one-shot sounds that shouldn't interrupt the current sound
    private void PlaySoundOneShot(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void SetGreenLight()
    {
        isGreenLight = true;
        isRotatingToFront = false;
        float targetAngle = backAngle;
        targetRotation = Quaternion.Euler(0, 0, targetAngle);
        isRotating = true;

        // Set next light change timer
        lightChangeTimer = Random.Range(minGreenLightTime, maxGreenLightTime);

        // Play green light sound
        PlaySound(greenLightSound);

        // Reset tracking variables
        if (arCamera != null)
        {
            lastPosition = arCamera.transform.position;
            lastRotation = arCamera.transform.rotation;
        }
    }

    private void SetRedLight()
    {
        isGreenLight = false;
        isRotatingToFront = true;
        float targetAngle = frontAngle;
        targetRotation = Quaternion.Euler(0, 0, targetAngle);
        isRotating = true;

        // Set next light change timer
        lightChangeTimer = Random.Range(minRedLightTime, maxRedLightTime);

        // Play red light sound
        PlaySound(redLightSound);

        // Record time when red light starts
        redLightStartTime = Time.time;

        // Update tracking position and rotation
        if (arCamera != null)
        {
            lastPosition = arCamera.transform.position;
            lastRotation = arCamera.transform.rotation;
        }
    }

    public void GameOver()
    {
        if (gameActive)
        {
            Debug.Log("Game Over!");
            gameActive = false;
            isTimerActive = false;

            // Stop tick sound coroutine if it's running
            if (tickSoundCoroutine != null)
            {
                StopCoroutine(tickSoundCoroutine);
                tickSoundCoroutine = null;
            }

            StartCoroutine(PlayGameOverSounds());
        }
    }

    private IEnumerator PlayGameOverSounds()
    {
        PlaySound(elimination);
        yield return new WaitForSeconds(3f); // Adjust delay as needed
        PlaySound(gunshot);
        yield return new WaitForSeconds(1f); // Adjust delay as needed
        gameover_Panel.SetActive(true);
    }

    // Coroutine to play tick sounds in the last 10 seconds
    private IEnumerator PlayTimerTickSounds()
    {
        while (currentGameTime > 0)
        {
            // Only play tick sounds in the last 10 seconds
            if (currentGameTime <= 10f)
            {
                PlaySoundOneShot(timerTickSound);
            }

            // Wait until next second
            yield return new WaitForSeconds(1f);
        }
    }

    // Format and display the time
    private void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(currentGameTime / 60);
            int seconds = Mathf.FloorToInt(currentGameTime % 60);

            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    // Check if timer has expired
    private void CheckTimer()
    {
        if (isTimerActive)
        {
            currentGameTime -= Time.deltaTime;

            if (currentGameTime <= 0)
            {
                currentGameTime = 0;
                isTimerActive = false;

                // Player wins if they survived the full time
                HandleTimerComplete();
            }

            UpdateTimerDisplay();
        }
    }

    // Method called when timer completes
    private void HandleTimerComplete()
    {
        // Play timer end sound if assigned
        if (timerEndSound != null)
        {
            PlaySound(timerEndSound);
        }

        // Player wins - implement win condition behavior here
        Debug.Log("Time's up! Player survived the full time!");

        // Stop the game
        StopGame();

        // Show win screen or other UI feedback
        // For now, we'll just show the gameover panel
        if (gameover_Panel != null)
        {
            // You can customize this to show a different win screen
            gameover_Panel.SetActive(true);
        }
    }

    private bool CheckCameraMovement()
    {
        if (arCamera == null) return false;

        // Calculate position and rotation differences
        float positionDifference = Vector3.Distance(arCamera.transform.position, lastPosition);
        float rotationDifference = Quaternion.Angle(arCamera.transform.rotation, lastRotation);

        // Check if either position or rotation exceeds sensitivity thresholds
        bool hasMoved = positionDifference > movementSensitivity || rotationDifference > rotationSensitivity;

        // Debug movement
        if (hasMoved)
        {
            Debug.Log($"Camera moved: Position diff={positionDifference}, Rotation diff={rotationDifference}");
        }

        // Update last position and rotation for next check
        lastPosition = arCamera.transform.position;
        lastRotation = arCamera.transform.rotation;

        return hasMoved;
    }

    void Update()
    {
        if (!gameActive) return;

        // Update timer
        CheckTimer();

        // Handle automatic light changes
        lightChangeTimer -= Time.deltaTime;
        if (lightChangeTimer <= 0)
        {
            if (isGreenLight)
            {
                SetRedLight();
            }
            else
            {
                SetGreenLight();
            }
        }

        // Check camera movement during red light
        if (!isGreenLight && !isRotating)
        {
            // Only check for movement after the grace period
            if (Time.time > redLightStartTime + graceTime)
            {
                if (CheckCameraMovement())
                {
                    GameOver();
                }
            }
        }

        // Handle rotation
        if (isRotating && dollHead != null)
        {
            dollHead.localRotation = Quaternion.RotateTowards(
                dollHead.localRotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );

            // Check if rotation is nearly complete
            if (Quaternion.Angle(dollHead.localRotation, targetRotation) < 0.1f)
            {
                dollHead.localRotation = targetRotation; // Snap to exact target
                isRotating = false;
            }
        }
    }

    // Public methods to pause/resume timer
    public void PauseTimer()
    {
        isTimerActive = false;
    }

    public void ResumeTimer()
    {
        isTimerActive = true;
    }

    // Method to add time to the current timer
    public void AddTime(float secondsToAdd)
    {
        currentGameTime += secondsToAdd;
        UpdateTimerDisplay();
    }

    // Method to adjust the sensitivity at runtime
    public void SetMovementSensitivity(float sensitivity)
    {
        movementSensitivity = Mathf.Max(0.001f, sensitivity);
    }

    // Method to adjust the rotation sensitivity at runtime
    public void SetRotationSensitivity(float sensitivity)
    {
        rotationSensitivity = Mathf.Max(0.1f, sensitivity);
    }
}
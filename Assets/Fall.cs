using UnityEngine;
using UnityEngine.XR.ARFoundation;
using TMPro;

public class ARGlassBridgeController : MonoBehaviour
{
    public GameObject stop;
    public GameObject gameover_Panel;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip breaksound;
    [SerializeField] private AudioClip bg;
    [SerializeField] private AudioClip elimination;

    [Header("Timer Settings")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private float totalGameTime = 120f; // 2 minutes in seconds
    private float currentTime;
    private bool isTimerRunning = true;

    [System.Serializable]
    public class LaneGlassPlatforms
    {
        public GameObject[] glassPlatforms;  // Platforms in each lane
    }

    [Header("Lane Configurations")]
    public LaneGlassPlatforms[] lanes;  // Multiple lanes of glass platforms

    [Header("Materials")]
    public Material safePlatformMaterial;
    public Material fragileGlassMaterial;
    public Material brokenGlassMaterial;

    [Header("Player Settings")]
    public ARSessionOrigin arSessionOrigin;  // AR Session Origin
    public GameObject objectToFall;  // Object to move when player dies

    [Header("Falling Mechanics")]
    public float fallSpeed = 5f;

    private Camera arCamera;
    private bool isPlayerDead = false;
    private bool isFalling = false;

    //jump time
    private float notOnGroundOrGlassTimer = 0f;
    public float fallDelay = 2f; // Time before the fall starts

    void Start()
    {
        PlaySound(bg, true);

        // Initialize timer
        currentTime = totalGameTime;
        UpdateTimerDisplay();

        // Find AR Camera
        arCamera = Camera.main;

        // Initialize glass platforms
        InitializeGlassPlatforms();

        // Add Collider if missing
        EnsureColliderExists();
    }

    private void EnsureColliderExists()
    {
        if (objectToFall != null)
        {
            // Add BoxCollider if no collider exists
            if (objectToFall.GetComponent<Collider>() == null)
            {
                Bounds bounds = CalculateRendererBounds(objectToFall);
                BoxCollider boxCollider = objectToFall.AddComponent<BoxCollider>();
                boxCollider.center = bounds.center;
                boxCollider.size = bounds.size;
            }
        }
    }

    private void PlaySound(AudioClip clip, bool loop = false)
    {
        if (clip != null && audioSource != null)
        {
            // Stop any currently playing sound
            audioSource.Stop();

            // Set the new clip and configure looping
            audioSource.clip = clip;
            audioSource.loop = loop; // Loop only when specified
            audioSource.Play();

            Debug.Log("Playing sound: " + clip.name + (loop ? " (Looping)" : ""));
        }
        else
        {
            if (clip == null)
                Debug.LogWarning("Attempted to play null audio clip");
            if (audioSource == null)
                Debug.LogError("AudioSource component is missing");
        }
    }

    private Bounds CalculateRendererBounds(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds;
        }

        // If no renderer, create a default bounds
        return new Bounds(obj.transform.position, Vector3.one);
    }

    private void InitializeGlassPlatforms()
    {
        // For each lane
        for (int laneIndex = 0; laneIndex < lanes.Length; laneIndex++)
        {
            // Determine which platforms will break in each lane
            for (int platformIndex = 0; platformIndex < lanes[laneIndex].glassPlatforms.Length; platformIndex++)
            {
                GameObject currentPlatform = lanes[laneIndex].glassPlatforms[platformIndex];

                // Break first platform in each lane (or subsequent platforms in a pattern)
                if (platformIndex == 0)
                {
                    // Set to fragile material
                    currentPlatform.GetComponent<Renderer>().material = fragileGlassMaterial;
                }
                else
                {
                    // Set to safe material
                    currentPlatform.GetComponent<Renderer>().material = safePlatformMaterial;
                }
            }
        }
    }

    private void Update()
    {
        // Update timer if the game is active
        if (isTimerRunning && !isPlayerDead && !isFalling)
        {
            UpdateTimer();
        }

        // Check player state if not already dead
        if (!isPlayerDead && !isFalling)
        {
            CheckPlayerState();
        }
        else if (!isFalling)  // Only start falling if not already falling
        {
            StartFalling();
        }
        else
        {
            // Handle falling
            PerformFall();
        }
    }

    private void UpdateTimer()
    {
        currentTime -= Time.deltaTime;

        if (currentTime <= 0)
        {
            currentTime = 0;
            isTimerRunning = false;
            TriggerPlayerDeath();
            Debug.Log("Time's up! Game over.");
        }

        UpdateTimerDisplay();
    }

    private void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(currentTime / 60);
            int seconds = Mathf.FloorToInt(currentTime % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    private void StartFalling()
    {
        PlaySound(breaksound);
        isFalling = true;
    }

    private void CheckPlayerState()
    {
        // Check if player is on ground or glass
        bool isOnGround = false;
        bool isOnGlass = false;

        // Use raycasting to check what's beneath the camera
        RaycastHit hit;
        if (Physics.Raycast(arCamera.transform.position, Vector3.down, out hit, 2f))
        {
            // Check if hit object is tagged ground
            if (hit.collider.CompareTag("ground"))
            {
                isOnGround = true;
            }

            // Check if hit object is a glass platform
            if (hit.collider.CompareTag("glass"))
            {
                isOnGlass = true;
            }
        }

        if (!isOnGround)
        {
            notOnGroundOrGlassTimer += Time.deltaTime;
            if (notOnGroundOrGlassTimer >= fallDelay)
            {
                TriggerPlayerDeath();
            }
        }
        else
        {
            notOnGroundOrGlassTimer = 0f; // Reset timer if on ground or glass
        }
    }

    private void TriggerPlayerDeath()
    {
        isPlayerDead = true;
        isTimerRunning = false;

        // Optional: Break the first platforms in each lane
        BreakInitialPlatforms();
    }

    private void BreakInitialPlatforms()
    {
        // Break first platform in each lane
        foreach (var lane in lanes)
        {
            if (lane.glassPlatforms.Length > 0)
            {
                GameObject firstPlatform = lane.glassPlatforms[0];
                firstPlatform.GetComponent<Renderer>().material = brokenGlassMaterial;
            }
        }
    }

    private void PerformFall()
    {
        if (objectToFall != null && isFalling == true)
        {
            // Move the object downward
            objectToFall.transform.Translate(Vector3.back * fallSpeed * Time.deltaTime);

            // Check for collision with "dead" tagged object using raycast
            RaycastHit hit;
            if (Physics.Raycast(arCamera.transform.position, Vector3.down, out hit, 2f))
            {
                // Check if hit object is tagged dead
                if (hit.collider.CompareTag("dead"))
                {
                    isFalling = false;
                    gameover_Panel.SetActive(true);
                    stop.SetActive(false);
                    GameOver();
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the collided object is tagged "dead"
        if (other.CompareTag("dead"))
        {
            isFalling = false;
            GameOver();
        }
    }

    private void GameOver()
    {
        Debug.Log("Game Over! Player fell or time ran out.");
        isTimerRunning = false;
        // Show game over panel
        if (gameover_Panel != null)
        {
            gameover_Panel.SetActive(true);
        }
        if (stop != null)
        {
            stop.SetActive(false);
        }
    }

    // Optional: Method to manually restart the game
    public void RestartGame()
    {
        isPlayerDead = false;
        isFalling = false;

        // Reset timer
        currentTime = totalGameTime;
        isTimerRunning = true;
        UpdateTimerDisplay();

        // Reinitialize platforms
        InitializeGlassPlatforms();

        // Reset object position if needed
        if (objectToFall != null)
        {
            objectToFall.transform.position = arSessionOrigin.transform.position;
        }

        // Hide game over panel
        if (gameover_Panel != null)
        {
            gameover_Panel.SetActive(false);
        }
        if (stop != null)
        {
            stop.SetActive(true);
        }
    }
}
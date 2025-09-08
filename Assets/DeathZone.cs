using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathZone : MonoBehaviour
{
    [Header("Death Settings")]
    [SerializeField] private float deathDelay = 0.5f; // Delay before restarting the scene
    [SerializeField] private bool useDeathAnimation = false; // Whether to play a death animation
    [SerializeField] private string deathAnimationTrigger = "Die"; // Animation trigger name
    
    [Header("Visual Feedback")]
    [SerializeField] private bool flashScreen = true; // Whether to flash the screen on death
    [SerializeField] private Color flashColor = Color.red; // Color to flash the screen
    [SerializeField] private float flashDuration = 0.2f; // Duration of the flash
    
    [Header("Audio")]
    [SerializeField] private bool playDeathSound = true; // Whether to play a death sound
    [SerializeField] private AudioClip deathSound; // Sound to play on death
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true; // Enable debug messages
    [SerializeField] private string playerTag = "Player"; // Tag to check for player
    
    private AudioSource audioSource;
    private bool isPlayerDead = false;
    
    private void Start()
    {
        // Get or add AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && playDeathSound)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Ensure we have a collider
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            if (debugMode) Debug.LogWarning("DeathZone: No Collider2D found. Adding a BoxCollider2D.");
            BoxCollider2D boxCollider = gameObject.AddComponent<BoxCollider2D>();
            boxCollider.isTrigger = true;
        }
        
        if (debugMode) Debug.Log("DeathZone initialized. Waiting for player collision.");
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (debugMode) Debug.Log("DeathZone: Trigger entered by " + other.gameObject.name + " with tag " + other.tag);
        
        // Check if the colliding object is the player
        if (other.CompareTag(playerTag) && !isPlayerDead)
        {
            if (debugMode) Debug.Log("DeathZone: Player detected in trigger. Killing player.");
            KillPlayer(other.gameObject);
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (debugMode) Debug.Log("DeathZone: Collision with " + collision.gameObject.name + " with tag " + collision.gameObject.tag);
        
        // Check if the colliding object is the player
        if (collision.gameObject.CompareTag(playerTag) && !isPlayerDead)
        {
            if (debugMode) Debug.Log("DeathZone: Player detected in collision. Killing player.");
            KillPlayer(collision.gameObject);
        }
    }
    
    private void KillPlayer(GameObject player)
    {
        if (debugMode) Debug.Log("DeathZone: KillPlayer called on " + player.name);
        
        isPlayerDead = true;
        
        // Disable player controls
        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null)
        {
            if (debugMode) Debug.Log("DeathZone: Disabling player controls.");
            playerController.canMove = false;
            playerController.canJump = false;
        }
        else if (debugMode)
        {
            Debug.LogWarning("DeathZone: PlayerController component not found on player.");
        }
        
        // Disable grapple hook
        GrappleHookController grappleHook = player.GetComponent<GrappleHookController>();
        if (grappleHook != null)
        {
            if (debugMode) Debug.Log("DeathZone: Releasing grapple hook.");
            grappleHook.ReleaseHook();
            grappleHook.enabled = false;
        }
        else if (debugMode)
        {
            Debug.LogWarning("DeathZone: GrappleHookController component not found on player.");
        }
        
        // Play death animation if available
        if (useDeathAnimation)
        {
            Animator animator = player.GetComponent<Animator>();
            if (animator != null)
            {
                if (debugMode) Debug.Log("DeathZone: Playing death animation.");
                animator.SetTrigger(deathAnimationTrigger);
            }
            else if (debugMode)
            {
                Debug.LogWarning("DeathZone: Animator component not found on player.");
            }
        }
        
        // Play death sound
        if (playDeathSound && deathSound != null && audioSource != null)
        {
            if (debugMode) Debug.Log("DeathZone: Playing death sound.");
            audioSource.PlayOneShot(deathSound);
        }
        else if (debugMode && playDeathSound)
        {
            Debug.LogWarning("DeathZone: Could not play death sound. Check if AudioClip is assigned.");
        }
        
        // Flash the screen
        if (flashScreen)
        {
            if (debugMode) Debug.Log("DeathZone: Starting screen flash.");
            StartCoroutine(FlashScreen());
        }
        
        // Restart the scene after delay
        if (debugMode) Debug.Log("DeathZone: Starting scene restart coroutine with delay: " + deathDelay);
        StartCoroutine(RestartScene());
    }
    
    private IEnumerator FlashScreen()
    {
        // Create a full-screen flash effect
        GameObject flashObject = new GameObject("DeathFlash");
        flashObject.transform.SetParent(transform);
        
        // Add a sprite renderer for the flash
        SpriteRenderer flashRenderer = flashObject.AddComponent<SpriteRenderer>();
        flashRenderer.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0);
        flashRenderer.sortingOrder = 999; // Ensure it's on top
        
        // Create a white texture for the flash
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        
        // Create a sprite from the texture
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        flashRenderer.sprite = sprite;
        
        // Scale the flash to cover the screen
        Camera mainCamera = Camera.main;
        float worldScreenHeight = mainCamera.orthographicSize * 2.0f;
        float worldScreenWidth = worldScreenHeight * mainCamera.aspect;
        flashObject.transform.localScale = new Vector3(worldScreenWidth, worldScreenHeight, 1);
        
        // Position the flash at the camera
        flashObject.transform.position = mainCamera.transform.position + new Vector3(0, 0, 1);
        
        // Fade in
        float elapsedTime = 0;
        while (elapsedTime < flashDuration / 2)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(0, 0.5f, elapsedTime / (flashDuration / 2));
            flashRenderer.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);
            yield return null;
        }
        
        // Fade out
        elapsedTime = 0;
        while (elapsedTime < flashDuration / 2)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(0.5f, 0, elapsedTime / (flashDuration / 2));
            flashRenderer.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);
            yield return null;
        }
        
        // Clean up
        Destroy(flashObject);
    }
    
    private IEnumerator RestartScene()
    {
        if (debugMode) Debug.Log("DeathZone: Waiting " + deathDelay + " seconds before restarting scene.");
        
        // Wait for the specified delay
        yield return new WaitForSeconds(deathDelay);
        
        if (debugMode) Debug.Log("DeathZone: Restarting scene.");
        
        // Restart the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    // Optional: Visualize the death zone in the editor
    private void OnDrawGizmos()
    {
        // Draw a red wireframe box to represent the death zone
        Gizmos.color = Color.red;
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            Gizmos.DrawWireCube(transform.position, boxCollider.size);
        }
        else
        {
            // Default size if no box collider
            Gizmos.DrawWireCube(transform.position, new Vector3(1, 1, 0));
        }
    }
} 
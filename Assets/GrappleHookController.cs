using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class GrappleHookController : MonoBehaviour
{
    // References
    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private LineRenderer ropeRenderer;
    [SerializeField] private LayerMask hookableLayerMask;
    [SerializeField] private LayerMask hookBlockerLayerMask; // New: layer mask for hook-blocking obstacles
    
    // Hook Properties
    [Header("Hook Properties")]
    [SerializeField] private float maxHookDistance = 10f;
    [SerializeField] private float hookSpeed = 15f;
    [SerializeField] private float pullSpeed = 5f;

    // Charges

    [Header("Hook Properties")]
    [SerializeField] public float maxCharge = 2;
    [SerializeField] public float charge = 2;
    
    // Swing Properties
    [Header("Swing Properties")]
    [SerializeField] private float swingForce = 1.5f;
    [SerializeField] private float swingDamping = 0.5f;
    [SerializeField] private float minRopeLength = 1f; // Minimum rope length when pulled
    [SerializeField] private float hookJumpForce = 1f;
    [SerializeField] private float hookRotationForce = 1f;
    [SerializeField] private float swingMomentumMultiplier = 2.5f; // New: multiplier for swing momentum
    [SerializeField] private float maxSwingSpeed = 15f; // New: maximum swing speed
    [SerializeField] private float swingAcceleration = 0.5f; // New: how quickly swing builds up
    
    // Zip Properties
    [Header("Zip Properties")]
    [SerializeField] private float zipSpeed = 20f; // Speed when zipping to hook points
    [SerializeField] private float zipStopDistance = 0.5f; // Distance to stop before reaching hook point

    [SerializeField] private bool autoRelease = true; // Automatically release when hook/zip completes
    
    // Input Configuration
    [Header("Input Configuration")]
    [SerializeField] private KeyCode hookKey = KeyCode.Mouse0;
    [SerializeField] private KeyCode pullKey = KeyCode.Mouse1;
    
    // References
    private Rigidbody2D rb;
    private Camera mainCamera;
    
    // State variables
    public bool isHooked = false;
    public bool isZipping = false; // New state for zipping movement
    private Vector2 hookPoint;
    private float currentRopeLength;
    private SpringJoint2D ropeJoint;

    private PlayerController playerController;

    private string hookSide;

    private float currentSwingMomentum = 0f; // New: tracks current swing momentum
    private Vector2 lastSwingDirection = Vector2.zero; // New: stores the last swing direction

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
        
        // Initialize rope renderer
        if (ropeRenderer != null)
        {
            ropeRenderer.positionCount = 2;
            ropeRenderer.enabled = false;
        }

        // Initialize charge amount on object initialization
        charge = maxCharge;
    }
    
    private void Update()
    {
        
        // Get mouse position in world space
        Vector2 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        
        // Fire hook on input
        if (Input.GetKeyDown(hookKey) && !isHooked)
        {
            FireHook(mousePos);
            rb.constraints = rb.constraints & ~RigidbodyConstraints2D.FreezeRotation;
            transform.rotation = Quaternion.identity;
        }
        
        // Pull rope to swing upward
        if (Input.GetKey(pullKey) && isHooked && !isZipping)
        {
            PullRope();
        }
        
        // Release hook
        if (Input.GetKeyUp(hookKey) && isHooked)
        {
            ReleaseHook();
            transform.rotation = Quaternion.identity;

            if(hookSide == "left")
            {
                rb.AddTorque(-hookRotationForce, ForceMode2D.Impulse);
            }

            if(hookSide == "right")
            {
                rb.AddTorque(hookRotationForce, ForceMode2D.Impulse);
            }
        }
        
        // Update rope visualization
        if (isHooked && ropeRenderer != null)
        {
            ropeRenderer.SetPosition(0, firePoint.position);
            ropeRenderer.SetPosition(1, hookPoint);
        }

        // Check if the player has gone above the hook point
        if(isHooked && !isZipping && hookPoint.y < transform.position.y)
        {
            ReleaseHook();
        }

        /*

        if(playerController.isGrounded && isHooked)
        {
            PullRope();
        }

        */

        if(playerController.isGrounded && charge != maxCharge)
        {
            charge = maxCharge;
        }
        
        // Apply swing movement based on horizontal input
        if (isHooked && !isZipping)
        {
            float horizontalInput = Input.GetAxisRaw("Horizontal");
            ApplySwingMovement(horizontalInput);
        }
        
        hookPreview();
    }
    
    private void FixedUpdate()
    {
        // Handle zipping movement in FixedUpdate for consistency with physics
        if (isZipping)
        {
            ZipToHookPoint();
        }
    }

    private void hookPreview()
    {
        // Get mouse position in world space
        Vector2 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);

        // Calculate the direction from fire point to mouse position
        Vector2 firePointPos = firePoint.position;
        Vector2 direction = (mousePos - firePointPos).normalized;

        // Perform raycast to find a hookable surface
        RaycastHit2D hit = Physics2D.Raycast(firePointPos, direction, maxHookDistance, hookableLayerMask);
        
        // If we hit a hookable surface, check if there's a blocker between the player and the hook point
        if (hit.collider != null)
        {
            // Check if there's a blocker between the player and the hook point
            RaycastHit2D blockerHit = Physics2D.Raycast(firePointPos, direction, Vector2.Distance(firePointPos, hit.point), hookBlockerLayerMask);
            
            // If there's a blocker between the player and the hook point, draw a line to the blocker
            if (blockerHit.collider != null)
            {
                Debug.DrawLine(firePointPos, blockerHit.point, Color.red);
            }
            else
            {
                // No blocker in the way, draw a line to the hook point
                Debug.DrawLine(firePointPos, hit.point, Color.green);
            }
        }
        else
        {
            // If no hookable surface is hit, draw a line to the maximum hook distance
            Debug.DrawLine(firePointPos, firePointPos + direction * maxHookDistance, Color.red);
        }
    }
    
    private void FireHook(Vector2 targetPosition)
    {

        if (charge > 0)
        {

            // Get reference to PlayerController first
            PlayerController playerController = GetComponent<PlayerController>();
            
            playerController.canMove = false;
            playerController.canJump = false;
            
            // Calculate the direction from fire point to mouse position
            Vector2 firePointPos = firePoint.position;
            Vector2 direction = (targetPosition - firePointPos).normalized;
            
            // First check for a hookable surface
            RaycastHit2D hit = Physics2D.Raycast(firePointPos, direction, maxHookDistance, hookableLayerMask);
            
            if (hit.collider != null)
            {
                // If we hit a hookable surface, check if there's a blocker between the player and the hook point
                RaycastHit2D blockerHit = Physics2D.Raycast(firePointPos, direction, Vector2.Distance(firePointPos, hit.point), hookBlockerLayerMask);
                
                // If there's a blocker between the player and the hook point, don't allow hooking
                if (blockerHit.collider != null)
                {
                    // Hook blocked - optional feedback
                    StartCoroutine(HookBlockedFeedback());
                    
                    // Restore player movement controls when hook is blocked
                    playerController.canMove = true;
                    playerController.canJump = true;
                    return;
                }
                
                // No blocker in the way, proceed with hooking
                // Only reset velocity when hook connects
                rb.velocity = Vector2.zero;
                
                charge -= 1;

                // Hook successful
                isHooked = true;
                hookPoint = hit.point;
                currentRopeLength = Vector2.Distance(firePointPos, hookPoint);
                
                // Enable rope visualization
                if (ropeRenderer != null)
                {
                    ropeRenderer.enabled = true;
                }

                // Determine if hook point is below player (using transform.position.y for center of gravity)
                if (hookPoint.y < transform.position.y)
                {
                    // Start zipping toward hook point
                    isZipping = true;
                    
                    // Movement handled in FixedUpdate instead of using a joint
                }
                else
                {
                    // Traditional swinging behavior for hooks above player
                    CreateRopeJoint();
                    
                    // Get the hook point's X position
                    float hitX = hit.point.x;

                    GetComponent<Rigidbody2D>().AddForce(Vector2.down * hookSpeed, ForceMode2D.Impulse);

                    // Check if hook is on the right of the player
                    if (hitX > transform.position.x)
                    {
                        Debug.Log("hook is on the right side");
                        GetComponent<Rigidbody2D>().AddForce(Vector2.right * hookSpeed, ForceMode2D.Impulse);

                        hookSide = "right";
                    }

                    // Check if hook is on the left of the player
                    if (hitX < transform.position.x)
                    {
                        Debug.Log("hook is on the left side");
                        GetComponent<Rigidbody2D>().AddForce(Vector2.left * hookSpeed, ForceMode2D.Impulse);

                        hookSide = "left";
                    }
                }
                
                // Optional visual and audio feedback
                StartCoroutine(HookImpactFeedback());
            }
            else
            {
                // Hook missed - optional feedback
                StartCoroutine(HookMissFeedback());
                
                // Restore player movement controls when hook misses
                playerController.canMove = true;
                playerController.canJump = true;
            }
        }
    }
    
    private void ZipToHookPoint()
    {
        // Calculate direction to hook point
        Vector2 direction = (hookPoint - (Vector2)transform.position).normalized;
        
        // Calculate distance to hook point
        float distanceToHook = Vector2.Distance(transform.position, hookPoint);
        
        // Check if we've reached the stop distance
        if (distanceToHook <= zipStopDistance)
        {
            // We've arrived at the hook point
            if (autoRelease)
            {
                ReleaseHook();
            }
            else
            {
                // Just stop zipping but keep hooked
                isZipping = false;
            }
            return;
        }
        
        // Move player toward hook point
        // Using MovePosition for smoother movement than directly setting transform position
        // This respects collisions and physics behavior
        rb.velocity = direction * zipSpeed;
    }
    
    private void CreateRopeJoint()
    {
        // Remove existing joint if it exists
        if (ropeJoint != null)
        {
            Destroy(ropeJoint);
        }
        
        // Create a new Spring Joint 2D
        ropeJoint = gameObject.AddComponent<SpringJoint2D>();
        ropeJoint.enableCollision = true;
        ropeJoint.autoConfigureConnectedAnchor = false;
        
        // Set anchor point
        ropeJoint.connectedAnchor = hookPoint;
        
        // Configure joint properties
        ropeJoint.distance = currentRopeLength;
        ropeJoint.dampingRatio = swingDamping;
        ropeJoint.frequency = swingForce;
    }
    
    private void PullRope()
    {
        // Reduce rope length to pull player upward
        if (currentRopeLength > minRopeLength)
        {
            currentRopeLength -= pullSpeed * Time.deltaTime;
            
            // Update joint distance
            if (ropeJoint != null)
            {
                ropeJoint.distance = currentRopeLength;
            }
        }
    }
    
    public void ReleaseHook()
    {
        // Apply swing momentum when releasing the hook
        if (currentSwingMomentum > 0.5f && isHooked && !isZipping)
        {
            // Apply a burst of force in the swing direction
            rb.AddForce(lastSwingDirection * currentSwingMomentum * swingMomentumMultiplier, ForceMode2D.Impulse);
            
            // Add a small upward boost
            rb.AddForce(Vector2.up * hookJumpForce * (currentSwingMomentum / maxSwingSpeed), ForceMode2D.Impulse);
        }
        else if (!isZipping)
        {
            // Default upward jump when releasing without significant momentum
            rb.AddForce(Vector2.up * hookJumpForce, ForceMode2D.Impulse);
        }

        if(hookSide == "left")
        {
            Debug.Log("hookSide value: " + hookSide);


            if(rb.rotation > 0)
            {
                rb.angularVelocity *= -1;
            }
            
            rb.AddTorque(hookRotationForce*-1, ForceMode2D.Impulse);
        }
        else if(hookSide == "right")
        {
            Debug.Log("hookSide value: " + hookSide);

            if(rb.rotation < 0)
            {
                rb.angularVelocity *= -1;
            }

            rb.AddTorque(hookRotationForce, ForceMode2D.Impulse);
        }

        // Reset swing momentum
        currentSwingMomentum = 0f;
        
        // Reset rope color
        if (ropeRenderer != null)
        {
            ropeRenderer.startColor = Color.white;
            ropeRenderer.endColor = Color.white;
        }

        playerController.canMove = true;
        playerController.canJump = true;

        isHooked = false;
        isZipping = false;
        
        // Destroy the joint
        if (ropeJoint != null)
        {
            Destroy(ropeJoint);
            ropeJoint = null;
        }
        
        // Disable rope visualization
        if (ropeRenderer != null)
        {
            ropeRenderer.enabled = false;
        }
    }
    
    private IEnumerator HookImpactFeedback()
    {
        // Add visual feedback when hook connects (e.g., particle effect)
        // Add sound effect
        
        yield return null;
    }
    
    private IEnumerator HookMissFeedback()
    {
        // Add visual feedback when hook misses
        // Add sound effect
        
        yield return null;
    }
    
    private IEnumerator HookBlockedFeedback()
    {
        // Add visual feedback when hook is blocked (e.g., particle effect)
        // Add sound effect
        
        yield return null;
    }
    
    // Enhanced swing movement method
    private void ApplySwingMovement(float horizontalInput)
    {
        if (isHooked && !isZipping)
        {
            // Calculate swing direction perpendicular to the rope
            Vector2 playerToHook = (Vector2)hookPoint - (Vector2)transform.position;
            Vector2 perpendicularDirection = new Vector2(-playerToHook.y, playerToHook.x).normalized;
            
            // Determine swing direction based on input
            Vector2 swingDirection = perpendicularDirection * Mathf.Sign(horizontalInput);
            
            // Build up swing momentum when input is provided
            if (Mathf.Abs(horizontalInput) > 0.1f)
            {
                // Increase momentum based on input strength and acceleration
                currentSwingMomentum += swingAcceleration * Mathf.Abs(horizontalInput) * Time.deltaTime;
                
                // Cap the maximum swing momentum
                currentSwingMomentum = Mathf.Min(currentSwingMomentum, maxSwingSpeed);
                
                // Store the last swing direction
                lastSwingDirection = swingDirection;
            }
            else
            {
                // Gradually reduce momentum when no input is provided
                currentSwingMomentum = Mathf.Max(0, currentSwingMomentum - swingDamping * Time.deltaTime);
            }
            
            // Apply force in the swing direction based on accumulated momentum
            if (currentSwingMomentum > 0.1f)
            {
                rb.AddForce(lastSwingDirection * currentSwingMomentum * swingForce, ForceMode2D.Force);
                
                // Visual feedback for swing power (optional)
                if (ropeRenderer != null)
                {
                    // Change rope color based on swing momentum
                    float normalizedMomentum = currentSwingMomentum / maxSwingSpeed;
                    ropeRenderer.startColor = Color.Lerp(Color.white, Color.red, normalizedMomentum);
                    ropeRenderer.endColor = Color.Lerp(Color.white, Color.red, normalizedMomentum);
                }
            }
        }
    }
}
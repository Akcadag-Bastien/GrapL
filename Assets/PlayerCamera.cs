using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public Transform player;
    public Vector3 offset;
    
    // Camera zoom settings
    [Header("Camera Zoom Settings")]
    [SerializeField] private float minZoom = 15f;
    [SerializeField] private float maxZoom = 30f;
    [SerializeField] private float zoomSpeed = 10f;
    [SerializeField] private float momentumThreshold = 5f;
    
    private Camera mainCamera;
    private Rigidbody2D playerRb;
    private float targetZoom;
    private float currentZoom;
    
    void Start()
    {
        mainCamera = GetComponent<Camera>();
        playerRb = player.GetComponent<Rigidbody2D>();
        
        // Initialize zoom
        currentZoom = minZoom;
        mainCamera.orthographicSize = currentZoom;
    }
    
    void Update()
    {
        // Update camera position
        transform.position = new Vector3(player.position.x + offset.x, 
                                         player.position.y + offset.y, 
                                         player.position.z + offset.z);
                                         
        // Calculate target zoom based on player velocity
        UpdateCameraZoom();
    }
    
    void UpdateCameraZoom()
    {
        if (playerRb != null)
        {
            // Calculate player speed (magnitude of velocity)
            float playerSpeed = playerRb.velocity.magnitude;
            
            // Calculate target zoom based on speed
            // Map speed from 0 to momentumThreshold to minZoom to maxZoom
            targetZoom = Mathf.Lerp(minZoom, maxZoom, Mathf.Clamp01(playerSpeed / momentumThreshold));
            
            // Smoothly interpolate current zoom to target zoom
            currentZoom = Mathf.Lerp(currentZoom, targetZoom, Time.deltaTime * zoomSpeed);
            
            // Apply zoom
            mainCamera.orthographicSize = currentZoom;
        }
    }
}
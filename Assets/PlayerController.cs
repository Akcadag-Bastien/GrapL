using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public string leftRightControl = "Horizontal";
    public string jumpControl = "Jump";
    public float speed = 10;
    public float jumpHeight = 1;
    private Transform playerTransform;

    private GrappleHookController grappleHookController;

    public bool isGrounded = false;

    public Rigidbody2D rb;

    private float verticalVelocity;
    private float horizontalVelocity;

    public bool canMove = true;
    public bool canJump = true;

    public Color color;
    
    void Start()
    {
        grappleHookController = GetComponent<GrappleHookController>();
        playerTransform = GetComponent<Transform>();


        horizontalVelocity = rb.velocity.x;
        verticalVelocity = rb.velocity.y;

        canMove = true;
        canJump = true;

        color = GetComponent<SpriteRenderer>().color;
    }
    
    void Update()
    {
        StopMomentum();

        // Update these values each frame
        horizontalVelocity = rb.velocity.x;
        verticalVelocity = rb.velocity.y;

        CheckIfPlayerIsStatic(verticalVelocity, horizontalVelocity);

        // Only move and jump if allowed
        if (canMove) Move();
        if (canJump) Jump();


        switch(grappleHookController.charge)
        {
        case 1:
            color = Color.yellow;
            break;
        case 2:
            color = Color.green;
            break;
        case 0:
            color = Color.red;
            break;
        }
        GetComponent<SpriteRenderer>().color = color;
    }

    void Move()
    {
        playerTransform.Translate(Vector2.right * Input.GetAxisRaw(leftRightControl) * speed * Time.deltaTime);
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {

        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {

        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }

    void Jump()
    {
        if (Input.GetButtonDown(jumpControl) && isGrounded)
        {
            // Apply an actual force for jumping rather than translation
            GetComponent<Rigidbody2D>().AddForce(Vector2.up * jumpHeight, ForceMode2D.Impulse);
        }
    }

    void StopMomentum()
    {
        if (Input.GetAxisRaw(leftRightControl) != 0 && isGrounded)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }
    }

    void CheckIfPlayerIsStatic(float yVelocity, float xVelocity)
    {
        // Use Mathf.Abs to check if player is truly static in all directions
        if (Mathf.Abs(xVelocity) < 2.0f * playerTransform.localScale.x && Mathf.Abs(yVelocity) < 1.0f * playerTransform.localScale.x && isGrounded)
        {
            transform.rotation = Quaternion.identity;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        if (Mathf.Abs(xVelocity) < 2.0f && Mathf.Abs(yVelocity) < 2.0f && grappleHookController.isHooked == true && grappleHookController.isZipping != true)
        {
            if(grappleHookController.isHooked)
            {
                grappleHookController.ReleaseHook();
            }
        }
    }
}
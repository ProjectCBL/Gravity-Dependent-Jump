using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{

    // Animation Curve Related
    private float gravityDelta = 0.0f;
    [SerializeField] private AnimationCurve gravityMultiplier;
    private float velocityDelta = 0.0f;
    [SerializeField] private AnimationCurve velocityCurve;

    // General Gameobject Properties
    private Rigidbody2D rb;
    private Vector2 jumpForceVector;
    public bool playerIsGrounded = false;
    public bool jumpLock = false;
    [SerializeField] private bool playerIsPressingJumpButton = false;
    [SerializeField][Range(0, 50)] private float airFrictionCoefficient = 2.0f;
    [SerializeField][Range(0, 50)] private float speed = 20.0f;
    [SerializeField][Range(0, 10.0f)] private float jumpHeight = 1.2f;
    [SerializeField] private Vector2 movementDirection = new Vector2(0, 0);

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        jumpForceVector = GenerateVelocityVector(jumpHeight);
    }

    void Update()
    {
        UpdateDelta();
        MovePlayer();
    }

    //region Rigidbody velocity manipulation

    private void MovePlayer()
    {
        MovePlayerHorizontally();
        MovePlayerVertically();
    }

    private void MovePlayerHorizontally()
    {
        float horizontalVelocity = movementDirection.x * speed * velocityCurve.Evaluate(velocityDelta);
        horizontalVelocity = (playerIsPressingJumpButton && movementDirection.x != 0 && !playerIsGrounded) ? applyAirFriction(horizontalVelocity) : horizontalVelocity;
        rb.velocity = new Vector2(horizontalVelocity, rb.velocity.y);
    }


    /*Where the magic begins...
    Description: This function controls the vertical velocity of the game object.  In order to achieve the 
    jump parabola we want, we'll need to account for gravity and the desired height.  First things first though is to 
    use our height to generate the approximate max vertical velocity we'll need to achieve in our Awake() method.
    Then we use our animation curve to generate evaluate our vertical gravity based on the amount of time the jump
    button was pressed and add our max vertical velocity to it.  If the button it let go we apply the gravity
    value at the end of the curve.
    */
    private void MovePlayerVertically()
    {
        //This value need to reflect the max value of the gravityMultiplier curve
        float maxGravityMultiplier = gravityMultiplier.keys[gravityMultiplier.keys.Length - 1].value;
        float verticalGravity = generateVerticalGravity(maxGravityMultiplier);
        rb.velocity = new Vector2(rb.velocity.x, verticalGravity);
    }

    private float generateVerticalGravity(float maxGravityMultiplier){
        if (playerIsPressingJumpButton && !jumpLock){
            return (Physics2D.gravity.y * (rb.gravityScale)) * gravityMultiplier.Evaluate(gravityDelta) + jumpForceVector.y;
        }
        else{
            return ((Physics2D.gravity.y * rb.gravityScale) * maxGravityMultiplier + jumpForceVector.y) * 1.15f;
        }
    }

    //endregion

    //region Animation Curve Deltas

    private void UpdateDelta()
    {
        UpdateVelocityDelta();
        UpdateGravityDelta();
    }

    private void UpdateVelocityDelta()
    {
        velocityDelta += (movementDirection.x != 0) ? Time.deltaTime : -(Time.deltaTime * 0.25f);
        velocityDelta = Mathf.Clamp(velocityDelta, 0, 1.0f);
    }

    private void UpdateGravityDelta()
    {
        gravityDelta += (playerIsPressingJumpButton) ? Time.deltaTime * 2.5f : 0;
        gravityDelta = Mathf.Clamp(gravityDelta, 0, 1.0f);
    }

    //endregion

    //region Velocity Modifiers/Accessors

    private Vector2 GenerateVelocityVector(float height)
    {
        return new Vector2(0, Mathf.Sqrt(-2 * (Physics2D.gravity.y * rb.gravityScale) * height));
    }

    private float applyAirFriction(float currentVelocity)
    {
        return currentVelocity - (airFrictionCoefficient * gravityDelta) * movementDirection.x;
    }

    //endregion

    //region Collision Reset Methods

    public void ZeroOutVerticalGravity()
    {
        rb.velocity = new Vector2(rb.velocity.x, 0.2f);
    }

    //endregion

    //region Unity Input Events

    public void OnMove(InputAction.CallbackContext ctx)
    {
        Vector2 movementVector = ctx.ReadValue<Vector2>();
        movementDirection.x = movementVector.x;
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        playerIsPressingJumpButton = ctx.performed;
        if(playerIsGrounded) gravityDelta = 0;
        if(!playerIsGrounded && playerIsPressingJumpButton == false) jumpLock = true;
    }

    //endregion

}

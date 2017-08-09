using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(PlatformerCollisionManager))]
[RequireComponent(typeof(PlatformerAnimationManager))]
public class PlatformerController : MonoBehaviour {

    [Header("Movement")]
    [SerializeField]
    private float _Gravity = -9.8f;

    [SerializeField]
    private float _WalkSpeed = 1;
    [SerializeField]
    private float _RunSpeed = 2;

    /// <summary>
    /// How much y velocity to add to the player when jumping
    /// </summary>
    [SerializeField]
    private float _JumpForce;

    /// <summary>
    /// How much should the player be able to move the character mid jump
    /// </summary>
    [SerializeField]
    private float _AirMovementSpeed;

    /// <summary>
    /// How long should the player hold the jump button for a high jump
    /// </summary>
    [SerializeField]
    private float _HighJumpTime = 0.05f;

    /// <summary>
    /// How much additional velocity should be added to the players jump during a high jump
    /// </summary>
    [SerializeField]
    private float _HighJumpVelocity;

    /// <summary>
    /// Time window in which player can jump after leaving the ground. Phantom jumps won't be allowed if this is 0
    /// </summary>
    [SerializeField]
    private float _PhantomJumpTime;

    [Header("Control")]
    [SerializeField]
    private string _HorizontalAxis = "Horizontal";

    [SerializeField]
    private string _JumpAxis = "Jump";

    [SerializeField]
    private string _RunAxis = "Run";

    /// <summary>
    /// What should the player collide with
    /// </summary>
    private LayerMask _Ground;

    /// <summary>
    /// The player's current velocity in units/second. Applied as movement each update.
    /// </summary>
    private Vector2 _Velocity;

    /// <summary>
    /// The position of the left joystick (or wasd axis) in normalized coordinates
    /// </summary>
    private float _HMovement;

    /// <summary>
    /// The input last frame
    /// </summary>
    private float _LastJumpInput;

    private bool _IsRunning, _IsGrounded, _LastGrounded, _ShouldJump, _HasHighJumped;

    private float _PhantomJumpTimer;

    /// <summary>
    /// How long has the jump button been held
    /// </summary>
    private float _JumpHeldTime;

    /// <summary>
    /// Reference to the Collision Manager
    /// </summary>
    private PlatformerCollisionManager _Collision;

    /// <summary>
    /// Reference to the collider
    /// </summary>
    private BoxCollider2D _Collider;

    // Use this for initialization
    void Start () {
        _Collision = GetComponent<PlatformerCollisionManager>();
        _Collider = GetComponent<BoxCollider2D>();
        _Ground = _Collision._CollisionLayers;
	}
	
	// Update is called once per frame
	void Update () {
        _HandleInput();
        _LastGrounded = _IsGrounded;
        _CheckGrounded();
        if (_PhantomJumpTimer > 0)
            _PhantomJumpTimer -= Time.deltaTime;
        _ApplyMovement();
        _Collision.Tick();
	}

    /// <summary>
    /// Check for the players inputs
    /// </summary>
    private void _HandleInput()
    {
        _HMovement = Input.GetAxis(_HorizontalAxis);
        _IsRunning = Input.GetAxis(_RunAxis) != 0;
        _ShouldJump = Input.GetAxis(_JumpAxis) != 0 && _LastJumpInput == 0;
        _LastJumpInput = Input.GetAxis(_JumpAxis);

        if (Input.GetAxis(_JumpAxis) == 0)
        {
            _JumpHeldTime = 0;
        }
        else
        {
            _JumpHeldTime += Time.deltaTime;
        }
    }

    /// <summary>
    /// Determine if the player is on the ground
    /// </summary>
    private void _CheckGrounded()
    {
        //Check the bottom of the player and see if there is collision there
        var hit = Physics2D.OverlapBox(new Vector3(transform.position.x, _Collider.bounds.min.y),
            new Vector2(_Collider.size.x * transform.lossyScale.x *.899f, 0.005f),
            transform.rotation.z,
            _Ground);

        if (hit)
        {
            if(_Velocity.y <0)
               _Velocity.y = 0;
            _IsGrounded = true;
            _HasHighJumped = false;
            _JumpHeldTime = 0;
        }
        else
        {
            _IsGrounded = false;
            if (_LastGrounded)
                _PhantomJumpTimer = _PhantomJumpTime;
        }

        //Check if the player hit its head
        hit = Physics2D.OverlapBox(new Vector3(transform.position.x, _Collider.bounds.max.y),
            new Vector2(_Collider.size.x * transform.lossyScale.x * .899f, 0.005f),
            transform.rotation.z,
            _Ground);

        if (hit && _Velocity.y >0) 
        {
            _Velocity.y = 0;
        }
    }

    /// <summary>
    /// Move the player
    /// </summary>
    private void _ApplyMovement()
    {
        //Set the players X velocity based on speed and whether they are on the ground
        if (_IsGrounded)
        {
            _Velocity.x = _HMovement * (_IsRunning ? _RunSpeed : _WalkSpeed);
        }
        else
        {
            if(_HMovement != 0)
                _Velocity.x = _HMovement * (_IsRunning ? _RunSpeed : _WalkSpeed);
            
        }

        //Apply gravity if the player isn't on ground
        if (!_IsGrounded)
            _Velocity.y += _Gravity * Time.deltaTime;

        //Jump if the player just pressed the jump button
        if (_ShouldJump && (_IsGrounded || _PhantomJumpTimer > 0))
        {
            _PhantomJumpTimer = 0;
            _Velocity.y = _JumpForce;
        }

        //If the player has held the jump button for a while, make the player jump a bit higher
        if (_JumpHeldTime >= _HighJumpTime && !_HasHighJumped && _Velocity.y >0)
        {
            _Velocity.y += _HighJumpVelocity;
            _HasHighJumped = true;
        }

        transform.position += Time.deltaTime * (Vector3)_Velocity;
    }

    public Vector2 GetVelocity()
    {
        return _Velocity;
    }

    public bool GetGrounded()
    {
        return _IsGrounded;
    }
}
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }

    public enum PlayerState
    {
        Idle,
        Moving,
        Jumping,
        Falling,
        Stunned,
        Eliminated,
        Loser,
        Winner,
    }

    [System.Serializable]
    public class Settings
    {
        [Header("Dive")]
        public float DiveForwardForce = 12f;
        public float DiveGroundForce = 10f;
        public float DiveDuration = 0.6f;

        [Tooltip("Slight upwards boost applied when starting an airborne dive.")]
        public float DiveBounceForce = 4f;

        [Header("Movements")]

        [Tooltip("Movement speed in km/h")]
        public float Speed = 18f;

        [Tooltip("Jump force in m/s")]
        public float JumpForce = 8f;

        [Tooltip("Player rotation speed towards movement direction")]
        public float RotationSpeed = 10f;

        [Tooltip("Ground detection tolerance")]
        public float GroundTolerance = 0.2f;

        [Tooltip("Layers considered as ground")]
        public LayerMask GroundLayer = 1;

        [Tooltip("Layers considered as death zone")]
        public LayerMask DeathLayer = 0;

        [Header("Forces")]

        [Tooltip("Decay rate of extra forces (m/s²)")]
        public float ExtraForcesDrag = 8f;

        [Header("Stun")]

        [Tooltip("Visual tumble speed while stunned (degrees per second)")]
        public float StunTumbleSpeed = 540f;

        [Tooltip("How fast the body rights itself once the stun ends")]
        public float StunRecoverSpeed = 12f;

        [Header("Debug")]

        [Tooltip("GUI logs of current state")]
        public bool StateLogs;
    }

    [System.Serializable]
    public class References
    {
        public CharacterController Controller;
        public InputActionAsset InputActions;
        public GameObject RetryPrefab;

        [Tooltip("Model root spun for the stun tumble. Defaults to the first child.")]
        public Transform Visual;
    }

    [System.Serializable]
    public class StateContainer
    {
        [Tooltip("Current player state")]
        public PlayerState CurrentState = PlayerState.Idle;

        [Tooltip("Is player paused?")]
        public bool IsPaused = false;
        
        [Tooltip("Current velocity in m/s")]
        public Vector3 Velocity;

        [Tooltip("Ground transform evaluated as parent")]
        public Transform Ground;
        
        public bool IsGrounded => Ground;
        public float VerticalVelocity => Velocity.y;
        public Vector3 HorizontalVelocity => new Vector3(Velocity.x, 0, Velocity.z);
    }

    [SerializeField] private Settings _settings;
    [SerializeField] private References _references;
    [SerializeField, ReadOnly] private StateContainer _state;
    
    public StateContainer State => _state;

    #region Constants
    private const float KMH_TO_MS = 1 / 3.6f;
    private const float STICK_FORCE = -5f;
    private const float GRAVITY = -20f;
    private const float MAX_GRAVITY = -50f;
    #endregion

    #region Public Fields

    public void Pause(bool pause)
    {
        _state.IsPaused = pause;
    }
    
    public void Lose()
    {
        if (_state.CurrentState ==  PlayerState.Loser)
            return;
        
        SetState(PlayerState.Loser);
    }
    
    public void Die()
    {
        if (_state.CurrentState ==  PlayerState.Eliminated)
            return;

        SetState(PlayerState.Eliminated);

        // The Retry prefab drives the death screen, then either respawns at the
        // last checkpoint (when a run is managed) or reloads the scene (legacy)
        Instantiate(_references.RetryPrefab);
    }

    public void Win()
    {
        if (_state.CurrentState == PlayerState.Winner)
            return;

        Pause(true);
        SetState(PlayerState.Winner);
    }

    /// <summary>
    /// Shoves the player. The horizontal part is applied as a decaying extra
    /// force (survives the per-frame input/ground resets); a positive Y launches.
    /// </summary>
    public void Knockback(Vector3 velocity)
    {
        _knockbackVelocity = new Vector3(velocity.x, 0f, velocity.z);

        if (velocity.y != 0f)
            _state.Velocity.y = velocity.y;
    }

    /// <summary>
    /// Bonks the player: takes away control for <paramref name="duration"/>
    /// seconds while a knockback flings them and the body tumbles, then they
    /// recover. A lightweight stand-in for a real ragdoll.
    /// </summary>
    public void Stun(float duration, Vector3 knockback)
    {
        switch (_state.CurrentState)
        {
            case PlayerState.Eliminated:
            case PlayerState.Loser:
            case PlayerState.Winner:
                return;
        }

        _stunTimer = Mathf.Max(_stunTimer, duration);
        _tumbleAxis = Random.onUnitSphere;

        SetState(PlayerState.Stunned);
        Knockback(knockback);
    }

    /// <summary>Moves the player to a pose, disabling the controller so it sticks.</summary>
    public void Teleport(Vector3 position, Quaternion rotation)
    {
        _references.Controller.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        _references.Controller.enabled = true;

        // Reset all momentum so the player respawns at a dead stop.
        _state.Velocity = Vector3.zero;
        _platformVelocity = Vector3.zero;
        _padVelocity = Vector3.zero;
        _knockbackVelocity = Vector3.zero;
        _state.Ground = null;

        Physics.SyncTransforms();
    }

    /// <summary>Restores control after a checkpoint respawn.</summary>
    public void Respawn()
    {
        _diveTimer = 0f;
        _isDivingOnPad = false;
        _padForward = Vector3.zero;

        Pause(false);
        SetState(PlayerState.Idle);
    }

    #endregion
    
    #region Private Fields
    // Inputs
    private InputAction _moveAction;
    private InputAction _jumpAction;

    // Camera
    private Camera _camera;

    // Ground
    private Vector3 _groundCheckRayOffset;
    private Vector3 _groundCheckSphereOffset;
    private float _groundCheckRadius;
    private Collider[] _overlapResults = new Collider[1];
    private Vector3 _lastPlatformPosition;
    private Quaternion _lastPlatformRotation;
    private Vector3 _platformVelocity;
    
    //Dive
    private InputAction _diveAction;
    private float _diveTimer = 0f;
    private bool _isDivingOnPad = false;
    private Vector3 _padForward;
    private Vector3 _padVelocity;

    // Knockback (e.g. spinning obstacles). Horizontal force that decays over time.
    private Vector3 _knockbackVelocity;

    // Stun (e.g. cannon item impact). Locks control while the body tumbles.
    private float _stunTimer;
    private Vector3 _tumbleAxis = Vector3.right;
    // Invisible pivot at the hitbox centre that the visual hangs from, so the stun
    // tumble spins about the middle of the body instead of the feet.
    private Transform _tumblePivot;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        if (!Instance)
            Instance = this;

        // Inputs
        _moveAction = _references.InputActions.FindActionMap("Player").FindAction("Move");
        _jumpAction = _references.InputActions.FindActionMap("Player").FindAction("Jump");
        _diveAction = _references.InputActions.FindActionMap("Player").FindAction("Interact");

        // Camera
        _camera = Camera.main;

        // Visual root used for the stun tumble
        if (_references.Visual == null && transform.childCount > 0)
            _references.Visual = transform.GetChild(0);

        // Ground check geometry
        CharacterController cc = _references.Controller;
        _groundCheckRayOffset = cc.center + Vector3.up * (-cc.height * .5f - cc.skinWidth + _settings.GroundTolerance);
        _groundCheckSphereOffset = cc.center + Vector3.up * (-cc.height * .5f + cc.radius - cc.skinWidth - _settings.GroundTolerance);
        _groundCheckRadius = cc.radius;

        // Hang the visual off an invisible pivot at the hitbox centre so the stun
        // tumble rotates about the middle of the body, not the feet.
        if (_references.Visual != null)
        {
            GameObject pivot = new GameObject("TumblePivot");
            pivot.transform.SetParent(transform, false);
            pivot.transform.localPosition = cc.center;
            _tumblePivot = pivot.transform;
            _references.Visual.SetParent(_tumblePivot, true);
        }
    }

    void OnEnable()
    {
        _moveAction?.Enable();
        _jumpAction?.Enable();
        _diveAction?.Enable();
    }

    void OnDisable()
    {
        _moveAction?.Disable();
        _jumpAction?.Disable();
        _diveAction?.Disable();
    }

    void Update()
    {
        float t = Time.deltaTime;

        switch (_state.CurrentState)
        {
            case PlayerState.Eliminated:
            case PlayerState.Loser:
            case PlayerState.Winner:
                return;
        }

        if (_state.CurrentState == PlayerState.Stunned)
        {
            UpdateStunned(t);
            return;
        }

        RestoreVisual(t);

        CheckGroundIsSloped();
        CheckGround(t);
        SetGravity(t);
        SetVelocity(t);
        SetJump();
        SetMovement(t);
        SetState();
        SetDivePad(t);
        SetDive(t);
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if ((_settings.DeathLayer.value & (1 << hit.gameObject.layer)) != 0)
            Die();
        
    }
    #endregion

    #region Player Logic
    private void CheckGround(float deltaTime)
    {
        // Raycast for center contact
        Vector3 rayOrigin = transform.position + _groundCheckRayOffset;
        bool rayHit = Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit rayInfo,
                                      _settings.GroundTolerance * 2f, _settings.GroundLayer);

        // OverlapSphere for edge contact
        Vector3 sphereOrigin = transform.position + _groundCheckSphereOffset;
        int overlapCount = Physics.OverlapSphereNonAlloc(sphereOrigin, _groundCheckRadius, _overlapResults, _settings.GroundLayer);
        bool sphereHit = overlapCount > 0;

        bool wasGrounded = _state.IsGrounded;
        bool isGrounded = rayHit || sphereHit;

        if (isGrounded)
        {
            Transform currentGround = rayHit ? rayInfo.collider.transform : _overlapResults[0].transform;

            // Initialize references when landing on a new surface to prevent teleporting
            if (currentGround != _state.Ground)
            {
                _state.Ground = currentGround;
                _lastPlatformPosition = _state.Ground.position;
                _lastPlatformRotation = _state.Ground.rotation;

                _platformVelocity.y = 0;

                return;
            }

            // Rotate player around platform pivot
            Quaternion rotationDelta = _state.Ground.rotation * Quaternion.Inverse(_lastPlatformRotation);
            float platformYaw = rotationDelta.eulerAngles.y;

            if (Mathf.Abs(platformYaw) > .001f)
            {
                Vector3 dir = transform.position - _state.Ground.position;
                dir = Quaternion.Euler(0, platformYaw, 0) * dir;
                transform.position = _state.Ground.position + dir;
                transform.Rotate(0, platformYaw, 0);
            }

            // Translation delta
            Vector3 platformDelta = _state.Ground.position - _lastPlatformPosition;
            transform.position += platformDelta;

            // Store current state for next frame
            _lastPlatformPosition = _state.Ground.position;
            _lastPlatformRotation = _state.Ground.rotation;

            // Sync physics broadphase to prevents CC from seeing stale overlap
            Physics.SyncTransforms();

            // Reset platform velocity
            _platformVelocity = Vector3.zero;
        }
        else
        {
            // Inherit platform velocity when player left the ground
            if (wasGrounded && _state.Ground != null)
            {
                _platformVelocity = (_state.Ground.position - _lastPlatformPosition) / Time.deltaTime;
            }
            // Decay velocity when player is in the air
            else
            {
                Vector3 platformVelocity = Vector3.MoveTowards(_platformVelocity, Vector3.zero, _settings.ExtraForcesDrag * deltaTime);
                platformVelocity.y = _platformVelocity.y;
                _platformVelocity = platformVelocity;
            }
            
            _state.Ground = null;
        }
    }

    private void SetGravity(float deltaTime)
    {
        if (_state.IsGrounded && _state.Velocity.y < 0)
        {
            _state.Velocity.y = STICK_FORCE;
        }
        else
        {
            if (_platformVelocity.y > 0)
            {
                _platformVelocity.y += GRAVITY * deltaTime;

                if (_platformVelocity.y < 0)
                    _state.Velocity.y += _platformVelocity.y;
            }
            else
            {
                _state.Velocity.y += GRAVITY * deltaTime;
            }

            _state.Velocity.y = Mathf.Max(_state.Velocity.y, MAX_GRAVITY);
        }
    }

    private void SetVelocity(float deltaTime)
    {
        if (_diveTimer > 0) return;
        Vector2 input = _moveAction.ReadValue<Vector2>();

        float speed = _settings.Speed * KMH_TO_MS;

        Vector3 moveInput = new Vector3(input.x, 0, input.y);
        moveInput = Quaternion.Euler(0, _camera.transform.eulerAngles.y, 0) * moveInput;
        moveInput *= speed;

        _state.Velocity.x = moveInput.x;
        _state.Velocity.z = moveInput.z;
        
        //Rotate Player
        if (moveInput.magnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveInput);
            float t = _settings.RotationSpeed * deltaTime;
            Vector3 euler = Quaternion.Slerp(transform.rotation, targetRot, t).eulerAngles;
            transform.rotation = Quaternion.Euler(0, euler.y, 0);
        }
    }

    private void SetJump()
    {
        // Velocity.y <= 0 guards against re-jumping while still rising: the ground
        // check keeps reporting grounded for a few frames after takeoff (tolerance
        // + overlap sphere), which otherwise lets you climb by spamming jump.
        if (_jumpAction.triggered && _state.IsGrounded && _state.Velocity.y <= 0)
        {
            _state.Velocity.y = _settings.JumpForce;
        }
    }

    [ContextMenu("Jumper")]
    public void Jumper(float force)
    {
        _state.Velocity = transform.up * force;
    }
    
    private void CheckGroundIsSloped()
    {
        if (_state.Ground == null) return;

        bool isSloped = _state.Ground.gameObject.layer == LayerMask.NameToLayer("Slope");

        if (isSloped)
        {
            _isDivingOnPad = true;
        }
        else
        {
            _isDivingOnPad = false;
        }
    }


    private void SetDivePad(float deltaTime)
    {
        if (_isDivingOnPad)
        {
            if (_padForward == Vector3.zero)
                _padForward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;

            _padVelocity = _padForward * _settings.DiveGroundForce;

            // Ne pas écraser un dive volontaire déjà lancé
            if (_diveTimer <= 0)
                _diveTimer = 0;

            return;
        }

        _padForward = Vector3.zero;
        _padVelocity = Vector3.zero;
    }
    private void SetDive(float deltaTime)
    {
        if (_diveTimer > 0)
        {
            // The dive ends the moment it touches the ground.
            if (_state.IsGrounded)
            {
                _diveTimer = 0f;
                return;
            }

            _diveTimer -= deltaTime;

            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                Quaternion.Euler(90, transform.eulerAngles.y, 0),
                deltaTime * 15f
            );

            return;
        }
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            Quaternion.Euler(0, transform.eulerAngles.y, 0),
            deltaTime * 10f
        );

        // You can only dive while airborne.
        if (_diveAction.triggered && !_state.IsGrounded)
        {
            _diveTimer = _settings.DiveDuration;

            Vector3 forward = transform.forward;
            _state.Velocity.x = forward.x * _settings.DiveForwardForce;
            _state.Velocity.z = forward.z * _settings.DiveForwardForce;

            // Slight upwards boost so the dive launches into a small arc.
            _state.Velocity.y = _settings.DiveBounceForce;
        }
    }


    private void SetMovement(float deltaTime)
    {
        Vector3 motion = _state.Velocity + _platformVelocity + _padVelocity + _knockbackVelocity;
        _references.Controller.Move(motion * deltaTime);

        // Knockback is an extra horizontal force that fades out on its own.
        _knockbackVelocity = Vector3.MoveTowards(_knockbackVelocity, Vector3.zero, _settings.ExtraForcesDrag * deltaTime);
    }

    /// <summary>
    /// Drives the player while stunned: control is suspended, the body keeps its
    /// gravity and decaying knockback, and the visual tumbles until the timer runs
    /// out, then we hand control back.
    /// </summary>
    private void UpdateStunned(float deltaTime)
    {
        CheckGround(deltaTime);
        SetGravity(deltaTime);

        // No steering while stunned — only the knockback and gravity move us.
        _state.Velocity.x = 0f;
        _state.Velocity.z = 0f;

        SetMovement(deltaTime);

        if (_tumblePivot != null)
            _tumblePivot.Rotate(_tumbleAxis, _settings.StunTumbleSpeed * deltaTime, Space.Self);

        _stunTimer -= deltaTime;
        if (_stunTimer <= 0f)
            SetState(_state.IsGrounded ? PlayerState.Idle : PlayerState.Falling);
    }

    /// <summary>Eases the tumbled body back upright once the stun has ended.</summary>
    private void RestoreVisual(float deltaTime)
    {
        if (_tumblePivot == null)
            return;

        _tumblePivot.localRotation = Quaternion.Slerp(
            _tumblePivot.localRotation, Quaternion.identity, _settings.StunRecoverSpeed * deltaTime);
    }

    public void SetState(PlayerState state)
    {
        State.CurrentState = state;
    }
    
    private void SetState()
    {
        switch (_state.CurrentState)
        {
            case PlayerState.Eliminated:
            case PlayerState.Loser:
            case PlayerState.Winner:
                return;
        }

        if (_diveTimer > 0)
        {
            State.CurrentState = PlayerState.Falling;
            return;
        }
        
        if (State.IsGrounded)
        {
            if (State.HorizontalVelocity.sqrMagnitude > .1f)
            {
                State.CurrentState = PlayerState.Moving;
            }
            else
            {
                State.CurrentState = PlayerState.Idle;
            }
            
        }
        else
        {
            if (State.VerticalVelocity > 0)
            {
                State.CurrentState = PlayerState.Jumping;
            }
            else
            {
                State.CurrentState = PlayerState.Falling;
            }
        }
    }
    #endregion
}

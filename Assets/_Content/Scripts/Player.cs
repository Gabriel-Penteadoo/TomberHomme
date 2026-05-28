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
        public float DiveDownForce = 8f;
        public float DiveGroundForce = 10f;
        public float DiveDuration = 0.6f;
        
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

        [Header("Debug")]

        [Tooltip("GUI logs of current state")]
        public bool StateLogs;
    }

    [System.Serializable]
    public class References
    {
        public CharacterController Controller;
        public InputActionAsset InputActions;
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

        // Ground check geometry
        CharacterController cc = _references.Controller;
        _groundCheckRayOffset = cc.center + Vector3.up * (-cc.height * .5f - cc.skinWidth + _settings.GroundTolerance);
        _groundCheckSphereOffset = cc.center + Vector3.up * (-cc.height * .5f + cc.radius - cc.skinWidth - _settings.GroundTolerance);
        _groundCheckRadius = cc.radius;
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

        CheckGround(t);
        SetGravity(t);
        SetVelocity(t);
        SetJump();
        SetMovement(t);
        SetState();
        SetDive(t);
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
        if (_jumpAction.triggered && _state.IsGrounded)
        {
            _state.Velocity.y = _settings.JumpForce;
        }
    }

    [ContextMenu("Jumper")]
    public void Jumper(float force)
    {
        _state.Velocity.y = force;
    }
    
    public void StartPadDive()
    {
        _isDivingOnPad = true;
    }

    public void StopPadDive()
    {
        _isDivingOnPad = false;
    }
    private void SetDive(float deltaTime)
    {

        if (_isDivingOnPad)
        {
            // Sauvegarder la direction une seule fois au début
            if (_padForward == Vector3.zero)
                _padForward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;

            // Rotation couchée
            float currentY = Mathf.Atan2(transform.forward.x, transform.forward.z) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                Quaternion.Euler(90, currentY, 0),
                Time.deltaTime * 15f
            );

            // Forcer la vélocité dans SetMovement via un champ dédié
            _padVelocity = _padForward * _settings.DiveGroundForce;

            _diveTimer = 0;
            return;
        }

        _padForward = Vector3.zero;
        _padVelocity = Vector3.zero;

        if (_diveTimer > 0)
        {
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

        if (_diveAction.triggered)
        {
            _diveTimer = _settings.DiveDuration;

            Vector3 forward = transform.forward;

            if (_state.IsGrounded)
            {
                _state.Velocity.x = forward.x * _settings.DiveGroundForce;
                _state.Velocity.z = forward.z * _settings.DiveGroundForce;
                _state.Velocity.y = 0;
            }
            else
            {
                _state.Velocity.x = forward.x * _settings.DiveForwardForce;
                _state.Velocity.z = forward.z * _settings.DiveForwardForce;
                _state.Velocity.y = -_settings.DiveDownForce;
            }
        }
    }
    

    private void SetMovement(float deltaTime)
    {
        Vector3 motion = _state.Velocity + _platformVelocity + _padVelocity; // ← ajoute _padVelocity
        _references.Controller.Move(motion * deltaTime);
    }
    private void SetState()
    {
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

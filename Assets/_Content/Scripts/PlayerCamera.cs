using UnityEngine;
using UnityEngine.InputSystem;

[ExecuteInEditMode]
public class PlayerCamera : MonoBehaviour
{
    [System.Serializable]
    private class Settings
    {
        [Header("Sensivity")]

        [Tooltip("Camera follow smoothness (0 = instant, 1 = smooth)")]
        [Range(0, 1)]
        public float FollowSmoothness = .1f;

        [Tooltip("Camera rotation sensitivity")]
        public float LookSensitivity = 20;

        [Header("Position")]

        [Tooltip("Camera distance from player")]
        public float Distance = 5;

        [Tooltip("Vertical offset from player")]
        public float VerticalOffset = 2;

        [Header("Pitch")]

        [Tooltip("Default pitch angle")]
        public float DefaultPitch = 20;

        [Tooltip("Min pitch angle")]
        public float MinPitch = -30;

        [Tooltip("Max pitch angle")]
        public float MaxPitch = 60;
    }

    [System.Serializable]
    public class References
    {
        public InputActionAsset InputActions;
        public Transform Target;
    }

    [SerializeField] private Settings _settings;
    [SerializeField] private References _references;

    private float _yaw;
    private float _pitch;

    private Vector3 _playerPosition;
    private InputAction _lookAction;
    
    void Awake()
    {
        _lookAction = _references.InputActions.FindActionMap("Player").FindAction("Look");
    }
    
    void OnEnable()
    {
        _lookAction?.Enable();
        
        _playerPosition = _references.Target.position;
        _pitch = _settings.DefaultPitch;
        
    }

    void OnDisable()
    {
        _lookAction?.Disable();
    }
    
    void LateUpdate()
    {
        float t = Time.deltaTime;
        
        SetCursor();
        SetYawAndPitch(t);
        SetPosition(t);
    }

    private void SetCursor()
    {
        if (!Application.isPlaying)
            return;

        bool lockCursor = !Player.Instance.State.IsPaused;

        Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockCursor;
    }

    private void SetYawAndPitch(float deltaTime)
    {
        if (Player.Instance && Player.Instance.State.IsPaused)
            return;
        
        
        Vector2 lookInput = _lookAction?.ReadValue<Vector2>() ?? Vector2.zero;

        _pitch -= lookInput.y * _settings.LookSensitivity * deltaTime;
        _pitch = Mathf.Clamp(_pitch, _settings.MinPitch, _settings.MaxPitch);

        _yaw += lookInput.x * _settings.LookSensitivity * deltaTime;
    }

    private void SetPosition(float deltaTime)
    {
        float t = (1.1f - _settings.FollowSmoothness) * 20 * deltaTime;
        _playerPosition = Vector3.Lerp(_playerPosition, _references.Target.position, t);

        Vector3 camPos = Vector3.back * _settings.Distance;
        camPos = Quaternion.Euler(_pitch, _yaw, 0) * camPos;
        camPos += _playerPosition;

        Quaternion camRot = Quaternion.LookRotation(_playerPosition - camPos);

        camPos.y += _settings.VerticalOffset;

        transform.position = camPos;
        transform.rotation = camRot;
    }
}

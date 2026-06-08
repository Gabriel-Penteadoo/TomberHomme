using UnityEngine;

/// <summary>
/// Rotates the object at a constant speed every frame. Used for spinning
/// obstacles such as the hammer (marteau).
/// </summary>
public class Spinner : MonoBehaviour
{
    [Tooltip("Local axis to rotate around")]
    [SerializeField] private Vector3 _axis = Vector3.up;

    [Tooltip("Rotation speed in degrees per second")]
    [SerializeField] private float _speed = 120f;

    [SerializeField] private Space _space = Space.Self;

    public Vector3 Axis => _axis;

    void Update()
    {
        transform.Rotate(_axis, _speed * Time.deltaTime, _space);
    }
}

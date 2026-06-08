using UnityEngine;

/// <summary>
/// Trigger volume that knocks the player aside as a spinning obstacle sweeps
/// through. The push goes to the pivot's right when the <see cref="Spinner"/>'s
/// axis Z is positive, and to its left when negative.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PushZone : MonoBehaviour
{
    [Tooltip("Spin pivot the push direction is taken from. Defaults to the parent.")]
    [SerializeField] private Transform _origin;

    [Tooltip("Horizontal knockback force")]
    [SerializeField] private float _force = 16f;

    [Tooltip("Upward launch added to the knockback")]
    [SerializeField] private float _lift = 6f;

    [Tooltip("Minimum delay between two pushes on the same player")]
    [SerializeField] private float _cooldown = 0.4f;

    private float _nextPushTime;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        TryPush(other);
    }

    void OnTriggerStay(Collider other)
    {
        TryPush(other);
    }

    private void TryPush(Collider other)
    {
        if (Time.time < _nextPushTime)
            return;

        if (!Player.Instance || Player.Instance.gameObject != other.gameObject)
            return;

        Transform origin = _origin ? _origin : (transform.parent ? transform.parent : transform);

        // Push sideways relative to the (non-spinning) spin axis: right when the
        // axis points +Z, left when it points -Z. Derived from the axis so it
        // stays stable no matter how far the hammer has rotated.
        Spinner spinner = origin.GetComponentInParent<Spinner>();
        Vector3 direction;

        if (spinner != null)
        {
            Vector3 axisWorld = origin.TransformDirection(spinner.Axis);
            direction = Vector3.Cross(Vector3.up, axisWorld);
        }
        else
        {
            direction = origin.right;
        }

        direction.y = 0f;
        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;

        Player.Instance.Knockback(direction * _force + Vector3.up * _lift);

        _nextPushTime = Time.time + _cooldown;
    }
}

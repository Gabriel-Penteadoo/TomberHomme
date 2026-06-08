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

    [Tooltip("Spinner driving this obstacle. Auto-found in parents if left empty.")]
    [SerializeField] private Spinner _spinner;

    private float _nextPushTime;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void Awake()
    {
        if (_spinner == null)
            _spinner = GetComponentInParent<Spinner>();
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

        // Push along the paddle's forward, flipped when the spinner spins the
        // other way (axis Z negative) so the knock follows the swing direction.
        Spinner spinner = origin.GetComponentInParent<Spinner>();
        float side = (spinner != null && spinner.Axis.z < 0f) ? -1f : 1f;

        Vector3 direction = transform.forward * side;

        Player.Instance.Knockback(direction * _force + Vector3.up * _lift);

        _nextPushTime = Time.time + _cooldown;
    }
}

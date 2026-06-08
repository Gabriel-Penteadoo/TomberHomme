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

        // Knock the player sideways across the (+Z) path: right (+X) when the
        // spinner's axis Z is positive, left (-X) when negative. We use the axis
        // VALUE you set, applied along a fixed world axis, so the direction stays
        // stable no matter how the FBX is oriented or how far the hammer spun.
        Spinner spinner = origin.GetComponentInParent<Spinner>();
        

        Vector3 direction = transform.forward;

        Player.Instance.Knockback(direction * _force + Vector3.up * _lift);

        _nextPushTime = Time.time + _cooldown;
    }
}

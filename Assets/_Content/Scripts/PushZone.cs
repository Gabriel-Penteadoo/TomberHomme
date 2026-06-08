using UnityEngine;

/// <summary>
/// Trigger volume that knocks the player away from an origin point (the spin
/// pivot by default). Put it on the moving parts of an obstacle (e.g. the
/// hammer paddles) so it shoves the player off when it sweeps through.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PushZone : MonoBehaviour
{
    [Tooltip("Point the player is pushed away from. Defaults to the parent (spin pivot).")]
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

        Vector3 direction = Player.Instance.transform.position - origin.position;
        direction.y = 0f;
        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;

        Player.Instance.Knockback(direction * _force + Vector3.up * _lift);

        _nextPushTime = Time.time + _cooldown;
    }
}

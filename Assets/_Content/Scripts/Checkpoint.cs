using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Drop this on a trigger volume to make it a checkpoint. When the player enters,
/// it updates the run's respawn point and records a split for the win-screen recap.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    [SerializeField] private string _label = "Checkpoint";

    [Tooltip("Optional transform used as the exact respawn pose. Defaults to this object.")]
    [SerializeField] private Transform _respawnAnchor;

    [Tooltip("Only triggers once. Disable to allow re-activation.")]
    [SerializeField] private bool _oneShot = true;

    [SerializeField] private UnityEvent _onActivated;

    private bool _used;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_used && _oneShot)
            return;

        if (!Player.Instance || Player.Instance.gameObject != other.gameObject)
            return;

        _used = true;

        Transform anchor = _respawnAnchor ? _respawnAnchor : transform;
        RunManager.Instance.SetCheckpoint(_label, anchor.position, anchor.rotation);

        _onActivated?.Invoke();
    }
}

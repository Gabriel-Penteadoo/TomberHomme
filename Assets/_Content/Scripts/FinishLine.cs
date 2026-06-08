using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Drop this on a trigger volume to make it the level's finish line. When the
/// player enters, the run ends and the win screen (with the final time and the
/// per-checkpoint recap) is shown.
/// </summary>
[RequireComponent(typeof(Collider))]
public class FinishLine : MonoBehaviour
{
    [SerializeField] private UnityEvent _onFinished;

    private bool _used;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_used)
            return;

        if (!Player.Instance || Player.Instance.gameObject != other.gameObject)
            return;

        _used = true;

        RunManager.Instance.Finish();

        _onFinished?.Invoke();
    }
}

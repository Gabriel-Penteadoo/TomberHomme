using UnityEngine;
using UnityEngine.Events;

public class PlayerEvent : MonoBehaviour
{
    [SerializeField] private UnityEvent _enPlayer;
    [SerializeField] private UnityEvent _outPlayer;

    void OnTriggerEnter(Collider col)
    {
        if (Player.Instance && Player.Instance.gameObject == col.gameObject)
        {
            _enPlayer?.Invoke();
        }
    }

    void OnTriggerExit(Collider col)
    {
        if (Player.Instance && Player.Instance.gameObject == col.gameObject)
        {
            _outPlayer?.Invoke();
        }
    }


}
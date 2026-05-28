using UnityEngine;
using UnityEngine.Events;

public class PlayerEvent : MonoBehaviour
{
    [SerializeField] private UnityEvent _enPlayer;
    void OnTriggerEnter(Collider col){
        if (Player.Instance && Player.Instance.gameObject == col.gameObject)
        {
            _enPlayer?.Invoke();
        }
    }
}
using UnityEngine;

/// <summary>
/// Slowly pushes a plane straight up. Touching the water kills the player; the
/// water then resets to its starting height so the respawned run starts fresh.
/// </summary>
[RequireComponent(typeof(Collider))]
public class RisingWater : MonoBehaviour
{
    [Tooltip("Vertical speed in units per second.")]
    [SerializeField] private float riseSpeed = 0.5f;

    [Tooltip("Optional height (world Y) at which the water stops rising. Leave below the start height to rise forever.")]
    [SerializeField] private bool useMaxHeight = false;
    [SerializeField] private float maxHeight = 10f;

    private float _startHeight;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void Awake()
    {
        _startHeight = transform.position.y;
    }

    void Update()
    {
        Vector3 position = transform.position;
        position.y += riseSpeed * Time.deltaTime;

        if (useMaxHeight && position.y > maxHeight)
        {
            position.y = maxHeight;
        }

        transform.position = position;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!Player.Instance || Player.Instance.gameObject != other.gameObject)
            return;

        Player.Instance.Die();

        // In checkpoint mode the scene isn't reloaded, so drop the water back to
        // its starting height in time for the player respawning at the checkpoint.
        float delay = RunManager.HasInstance ? RunManager.Instance.RespawnDelay : 0f;
        Invoke(nameof(ResetHeight), delay);
    }

    private void ResetHeight()
    {
        Vector3 position = transform.position;
        position.y = _startHeight;
        transform.position = position;
    }
}

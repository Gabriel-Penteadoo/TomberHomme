using UnityEngine;

public class RisingWater : MonoBehaviour
{
    [Tooltip("Vertical speed in units per second.")]
    [SerializeField] private float riseSpeed = 0.5f;

    [Tooltip("Optional height (world Y) at which the water stops rising. Leave below the start height to rise forever.")]
    [SerializeField] private bool useMaxHeight = false;
    [SerializeField] private float maxHeight = 10f;

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
}

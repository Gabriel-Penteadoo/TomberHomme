using UnityEngine;

/// <summary>
/// Gently shoves everything resting on a slope towards its bottom: the player
/// (so climbing it is harder) and any loose Rigidbody such as the items spat out
/// by the <see cref="Cannon"/> (so they get flushed back down).
///
/// Put this on the slope object. It builds a thin trigger slab over the surface
/// at runtime (a CharacterController only reports trigger overlaps, not
/// collisions against static geometry), then pushes whatever sits inside it down
/// the slope. The downhill direction is derived from the surface normal
/// (local up), so it follows however the slope is rotated.
/// </summary>
public class SlopeSlide : MonoBehaviour
{
    [Tooltip("Downhill drift speed forced on the player (m/s). Keep it below the " +
             "player's move speed so the climb stays possible, just harder.")]
    [SerializeField] private float _playerForce = 3.5f;

    [Tooltip("Downhill acceleration applied to loose items (m/s²).")]
    [SerializeField] private float _itemForce = 10f;

    [Tooltip("Height of the trigger slab built above the surface.")]
    [SerializeField] private float _slabHeight = 3f;

    void Awake()
    {
        BuildTriggerSlab();
    }

    /// <summary>
    /// Adds a trigger BoxCollider covering a slab just above the solid collider,
    /// so things standing on the slope fall inside it.
    /// </summary>
    private void BuildTriggerSlab()
    {
        Bounds local;
        Collider solid = GetComponent<Collider>();

        if (solid is MeshCollider mesh && mesh.sharedMesh != null)
            local = mesh.sharedMesh.bounds;
        else if (solid is BoxCollider box)
            local = new Bounds(box.center, box.size);
        else
            local = new Bounds(Vector3.zero, Vector3.one);

        BoxCollider trigger = gameObject.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = local.center + Vector3.up * (_slabHeight * 0.5f);
        trigger.size = new Vector3(local.size.x, local.size.y + _slabHeight, local.size.z);
    }

    private Vector3 Downhill()
    {
        // Gravity projected onto the surface plane (local up is the normal).
        return Vector3.ProjectOnPlane(Vector3.down, transform.up).normalized;
    }

    void OnTriggerStay(Collider other)
    {
        Vector3 downhill = Downhill();
        if (downhill.sqrMagnitude < 0.0001f)
            return; // Surface is flat, nothing to slide down.

        if (Player.Instance && Player.Instance.gameObject == other.gameObject)
        {
            // Horizontal-only so we nudge the player downslope without fighting
            // their gravity/jump. Re-applied every frame for a steady drift.
            Vector3 flat = new Vector3(downhill.x, 0f, downhill.z).normalized;
            Player.Instance.Knockback(flat * _playerForce);
            return;
        }

        Rigidbody body = other.attachedRigidbody;
        if (body != null && !body.isKinematic)
            body.AddForce(downhill * _itemForce, ForceMode.Acceleration);
    }
}

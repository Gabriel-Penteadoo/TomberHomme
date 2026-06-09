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
    ///
    /// The slab lives on a child object placed on the "Ignore Raycast" layer rather
    /// than on the slope itself. The slope sits on the Ground layer, and the player's
    /// ground check queries that layer and (by default) hits triggers — so a slab on
    /// the slope would read as solid ground metres above the surface, keeping the
    /// player "grounded" in mid-air and blocking the dive. Off the ground layer, the
    /// ground check ignores the slab while the slope's own solid collider still
    /// registers as ground when actually standing on it.
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

        GameObject slab = new GameObject("SlopeSlideTrigger");
        slab.layer = LayerMask.NameToLayer("Ignore Raycast");
        // Parent without keeping world position so the child shares the slope's
        // local space; the collider bounds below are expressed in that space.
        slab.transform.SetParent(transform, false);

        BoxCollider trigger = slab.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = local.center + Vector3.up * (_slabHeight * 0.5f);
        trigger.size = new Vector3(local.size.x, local.size.y + _slabHeight, local.size.z);

        // The trigger lives on the child, so its OnTrigger callbacks land there too.
        // Relay them back to this component, which owns the push logic and tunables.
        slab.AddComponent<SlopeSlideRelay>().Owner = this;
    }

    private Vector3 Downhill()
    {
        // Gravity projected onto the surface plane (local up is the normal).
        return Vector3.ProjectOnPlane(Vector3.down, transform.up).normalized;
    }

    /// <summary>
    /// Pushes whatever sits in the trigger slab down the slope. Invoked every frame
    /// by the child <see cref="SlopeSlideRelay"/> that owns the trigger collider.
    /// </summary>
    public void ApplySlide(Collider other)
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

/// <summary>
/// Sits on the trigger-slab child object built by <see cref="SlopeSlide"/> and
/// forwards its trigger callbacks to the parent. The slab is kept on a child (on a
/// non-ground layer) so the player's ground check can't mistake it for solid ground,
/// which means the trigger messages arrive here rather than on the SlopeSlide itself.
/// </summary>
public class SlopeSlideRelay : MonoBehaviour
{
    public SlopeSlide Owner;

    void OnTriggerStay(Collider other)
    {
        if (Owner != null)
            Owner.ApplySlide(other);
    }
}

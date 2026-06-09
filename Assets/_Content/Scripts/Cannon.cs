using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Periodically launches a random decorative item out of the muzzle like a
/// cannon. Each item gets a Rigidbody and a (non-trigger) Collider so it
/// physically blocks the player and hinders their climb up a path. Aim the
/// cannon by rotating the prefab: items are fired along the muzzle's local up.
/// </summary>
public class Cannon : MonoBehaviour
{
    [Header("Items")]
    [Tooltip("Prefabs picked from at random when firing. Auto-filled in the " +
             "editor from the decorative models folder when left empty.")]
    [SerializeField] private List<GameObject> _items = new List<GameObject>();

    [Tooltip("Point items are spawned from. Launch direction is its local up. " +
             "Defaults to this transform.")]
    [SerializeField] private Transform _muzzle;

    [Tooltip("Optional flash/smoke played on each shot.")]
    [SerializeField] private ParticleSystem _muzzleFlash;

    [Header("Firing")]
    [SerializeField] private bool _autoFire = true;

    [Tooltip("Seconds between shots")]
    [SerializeField] private float _fireInterval = 2f;

    [Tooltip("Random +/- added to the interval so shots feel less robotic")]
    [SerializeField] private float _intervalJitter = 0.5f;

    [Header("Launch")]
    [Tooltip("Launch speed in m/s")]
    [SerializeField] private float _launchSpeed = 18f;

    [Tooltip("Random +/- added to the launch speed")]
    [SerializeField] private float _speedJitter = 4f;

    [Tooltip("Random cone half-angle (degrees) applied to the launch direction")]
    [SerializeField] private float _spreadAngle = 8f;

    [Tooltip("Random tumble given to the item (degrees per second)")]
    [SerializeField] private float _spin = 180f;

    [Header("Item physics")]
    [SerializeField] private float _itemMass = 2f;

    [Tooltip("Seconds before a launched item is destroyed (0 = never)")]
    [SerializeField] private float _itemLifetime = 5f;

    [Header("Knockback")]
    [Tooltip("Shove the player when a flying item hits them (a CharacterController " +
             "ignores raw physics, so we push it explicitly).")]
    [SerializeField] private bool _knockback = true;

    [SerializeField] private float _knockbackForce = 12f;
    [SerializeField] private float _knockbackLift = 5f;

    [Tooltip("Seconds the player is stunned (tumbles, loses control) on a hit")]
    [SerializeField] private float _stunDuration = 1.2f;

    private float _nextFireTime;

    void OnEnable()
    {
        _nextFireTime = Time.time + NextDelay();
    }

    void Update()
    {
        if (!_autoFire)
            return;

        if (Time.time >= _nextFireTime)
        {
            Fire();
            _nextFireTime = Time.time + NextDelay();
        }
    }

    private float NextDelay()
    {
        return Mathf.Max(0.05f, _fireInterval + Random.Range(-_intervalJitter, _intervalJitter));
    }

    [ContextMenu("Fire")]
    public void Fire()
    {
        if (_items == null || _items.Count == 0)
            return;

        GameObject prefab = _items[Random.Range(0, _items.Count)];
        if (prefab == null)
            return;

        Transform muzzle = _muzzle != null ? _muzzle : transform;

        GameObject item = Instantiate(prefab, muzzle.position, muzzle.rotation);

        EnsureCollider(item);

        Rigidbody body = item.GetComponent<Rigidbody>();
        if (body == null)
            body = item.AddComponent<Rigidbody>();
        body.mass = _itemMass;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Spread the shot inside a cone around the muzzle's up axis.
        Vector3 direction = Quaternion.AngleAxis(Random.Range(0f, _spreadAngle), Random.onUnitSphere) * muzzle.up;
        float speed = _launchSpeed + Random.Range(-_speedJitter, _speedJitter);

        body.linearVelocity = direction * speed;
        body.angularVelocity = Random.onUnitSphere * (_spin * Mathf.Deg2Rad);

        // Carries lifetime + the player knockback on impact.
        CannonProjectile projectile = item.AddComponent<CannonProjectile>();
        projectile.Configure(_itemLifetime, _knockback, _knockbackForce, _knockbackLift, _stunDuration);

        if (_muzzleFlash != null)
            _muzzleFlash.Play();
    }

    /// <summary>
    /// Make sure the launched item has a non-trigger collider so it blocks the
    /// player. Falls back to a box sized from the renderers when none exists.
    /// </summary>
    private static void EnsureCollider(GameObject item)
    {
        Collider[] colliders = item.GetComponentsInChildren<Collider>();
        bool hasSolid = false;

        foreach (Collider c in colliders)
        {
            if (!c.isTrigger)
            {
                hasSolid = true;
                break;
            }
        }

        if (hasSolid)
            return;

        BoxCollider box = item.AddComponent<BoxCollider>();

        Renderer[] renderers = item.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            // Convert world bounds back into the item's local space.
            box.center = item.transform.InverseTransformPoint(bounds.center);
            box.size = item.transform.InverseTransformVector(bounds.size);
            box.size = new Vector3(Mathf.Abs(box.size.x), Mathf.Abs(box.size.y), Mathf.Abs(box.size.z));
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (_items == null)
            _items = new List<GameObject>();

        if (_items.Count > 0)
            return;

        const string folder = "Assets/_Content/Models/Platforms/decorative";
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
                _items.Add(prefab);
        }
    }
#endif
}

/// <summary>
/// Added to each launched item by <see cref="Cannon"/>. Despawns it after a
/// delay and shoves the player on contact (the player is a CharacterController,
/// which a Rigidbody impact alone won't move).
/// </summary>
public class CannonProjectile : MonoBehaviour
{
    private bool _knockback;
    private float _force;
    private float _lift;
    private float _stunDuration;

    public void Configure(float lifetime, bool knockback, float force, float lift, float stunDuration)
    {
        _knockback = knockback;
        _force = force;
        _lift = lift;
        _stunDuration = stunDuration;

        if (lifetime > 0f)
            Destroy(gameObject, lifetime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!_knockback)
            return;

        if (!Player.Instance || Player.Instance.gameObject != collision.gameObject)
            return;

        Vector3 direction = collision.transform.position - transform.position;
        direction.y = 0f;
        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.up;

        Vector3 hit = direction * _force + Vector3.up * _lift;

        // Stun bundles the knockback with a tumble + loss of control; fall back to
        // a plain shove if no stun is configured.
        if (_stunDuration > 0f)
            Player.Instance.Stun(_stunDuration, hit);
        else
            Player.Instance.Knockback(hit);
    }
}

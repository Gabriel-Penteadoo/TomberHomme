using UnityEngine;

public class RocketExplosion : MonoBehaviour
{
    [System.Serializable]
    public class References
    {
        public SphereCollider Collider;
        public ParticleSystem Explosion;
    }

    [System.Serializable]
    public class Settings
    {
        public float Radius = 5;
    }

    [SerializeField] private Settings _settings;
    [SerializeField] private References _references;

    void Awake()
    {
        _references.Collider.radius = 0;
        _references.Explosion.Play();
    }

    void Update()
    {
        _references.Collider.radius += Time.deltaTime * 50;

        if (_references.Collider.radius > _settings.Radius)
        {
            Destroy(gameObject);
        }
    }
}
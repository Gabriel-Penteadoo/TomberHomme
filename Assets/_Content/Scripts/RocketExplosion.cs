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

    [SerializeField] private References _references;
    [SerializeField] private Settings _settings;

    private float radius;
    private float time;

    void Awake()
    {
        radius = 1;
        _references.Explosion.Play();
    }

    void Update()
    {
        radius += Time.deltaTime * 15;
        _references.Collider.radius = radius <= _settings.Radius ? radius : 0;

        // Destroy after 5 seconds
        time += Time.deltaTime;
        if (time > 5)
        {
            Destroy(gameObject);
        }
    }
}
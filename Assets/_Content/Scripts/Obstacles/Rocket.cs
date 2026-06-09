using UnityEngine;
using UnityEngine.WSA;

public class Rocket : MonoBehaviour
{
    [System.Serializable]
    public class References
    {
        public Rigidbody Rigidbody;
        public ParticleSystem Fire;
        public GameObject Explosion;
    }

    [System.Serializable]
    public class Settings
    {
        public bool LaunchOnEnable = false;
        public float Speed = 20;
        public float Duration = 5;
    }
    
    [System.Serializable]
    public class State
    {
        public bool Launched = false;
        public bool Disabled = false;
        public float FlightTime = 0;
    }

    [SerializeField] private References _references;
    [SerializeField] private Settings _settings;
    [SerializeField, ReadOnly] private State _state;
    
    void OnEnable()
    {
        if (_settings.LaunchOnEnable)
        {
            Launch();
        }
    }

    void Update()
    {
        if (_state.Launched && _state.FlightTime < _settings.Duration)
        {
            Vector3 force = transform.up * _settings.Speed * Time.deltaTime * 100;

            _references.Rigidbody.AddForce(force);

            _state.FlightTime += Time.deltaTime;
        }
        else if (!_state.Disabled)
        {
            _state.Disabled = true;

            _references.Fire.Stop();
        }
    }

    void OnCollisionEnter(Collision col)
    {
        if (_state.Launched && _state.FlightTime > .5f)
        {
            GameObject explosion = Instantiate(_references.Explosion);
            explosion.transform.position = transform.position;

            Destroy(gameObject);
        }
    }

    [ContextMenu("Launch")]
    public void Launch()
    {
        _state.Launched = true;
        _state.Disabled = false;
        _state.FlightTime = 0;

        _references.Fire.Play();
    }
}
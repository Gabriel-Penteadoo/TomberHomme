using UnityEngine;

public class PlayerSoundManager : MonoBehaviour
{
    public static PlayerSoundManager Instance { get; private set; }

    [System.Serializable]
    public class References
    {
        public AudioSource OneShotSource;
        public AudioSource MoveSource;
    }

    [System.Serializable]
    public class Sounds
    {
        public AudioClip[] Jump;
        public AudioClip Fall;
        public AudioClip Move;
        public AudioClip[] Eliminate;
        public AudioClip Win;
        public AudioClip[] Lose;
    }

    [SerializeField] private References _references;
    [SerializeField] private Sounds _sounds;

    private Player.PlayerState _previousState;

    void Awake()
    {
        if (!Instance)
            Instance = this;

        if (_references.MoveSource)
        {
            _references.MoveSource.clip = _sounds.Move;
            _references.MoveSource.loop = true;
        }
    }

    void LateUpdate()
    {
        if (!Player.Instance)
            return;

        Player.PlayerState current = Player.Instance.State.CurrentState;

        if (current != _previousState)
        {
            OnStateChanged(current);
            _previousState = current;
        }

        if (_references.MoveSource)
        {
            bool moving = current == Player.PlayerState.Moving;
            if (moving && !_references.MoveSource.isPlaying)
                _references.MoveSource.Play();
            else if (!moving && _references.MoveSource.isPlaying)
                _references.MoveSource.Stop();
        }
    }

    private void OnStateChanged(Player.PlayerState to)
    {
        switch (to)
        {
            case Player.PlayerState.Jumping:
                PlayRandom(_sounds.Jump);
                break;
            case Player.PlayerState.Falling:
                Play(_sounds.Fall);
                break;
            case Player.PlayerState.Eliminated:
                PlayRandom(_sounds.Eliminate);
                break;
            case Player.PlayerState.Winner:
                Play(_sounds.Win);
                break;
            case Player.PlayerState.Loser:
                PlayRandom(_sounds.Lose);
                break;
        }
    }

    private void Play(AudioClip clip)
    {
        if (!clip || !_references.OneShotSource) return;
        _references.OneShotSource.PlayOneShot(clip);
    }

    private void PlayRandom(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return;
        Play(clips[Random.Range(0, clips.Length)]);
    }
}

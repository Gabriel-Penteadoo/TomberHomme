using UnityEngine;
using UnityEngine.SceneManagement;

public class Retry : MonoBehaviour
{
    [SerializeField] private GameObject _ui;

    private bool _waitForPlayer;
    private bool _checkpointMode;

    void Awake()
    {
        _ui.gameObject.SetActive(false);

        // When a run is active, respawn at the last checkpoint instead of
        // reloading the whole scene.
        _checkpointMode = RunManager.HasInstance && RunManager.Instance.HasRespawn;

        if (_checkpointMode)
        {
            Invoke(nameof(GoToCheckpoint), RunManager.Instance.RespawnDelay);
        }
        else
        {
            DontDestroyOnLoad(this);
            SceneManager.sceneLoaded += OnSceneLoaded;
            Invoke(nameof(ReloadScene), 2f);
        }
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        if (_waitForPlayer && Player.Instance)
        {
            _waitForPlayer = false;
            ShowRetryScreen();
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _waitForPlayer = true;
    }

    void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Teleports the player back to the last checkpoint, then shows the screen.
    void GoToCheckpoint()
    {
        RunManager rm = RunManager.Instance;

        if (Player.Instance)
            Player.Instance.Teleport(rm.RespawnPosition, rm.RespawnRotation);

        ShowRetryScreen();
    }

    void ShowRetryScreen()
    {
        if (Player.Instance)
        {
            transform.position = Player.Instance.transform.position;
            Player.Instance.Pause(true);
            Player.Instance.Lose();
        }

        _ui.gameObject.SetActive(true);
    }

    public void RetryGame()
    {
        Player.Instance.Pause(false);
        Player.Instance.SetState(Player.PlayerState.Idle);
        Destroy(gameObject);
    }
}

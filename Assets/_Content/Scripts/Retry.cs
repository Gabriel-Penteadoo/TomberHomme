using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Retry : MonoBehaviour
{
    [SerializeField] private GameObject _ui;
    
    private bool _waitForPlayer;
    
    void Awake()
    {
        _ui.gameObject.SetActive(false);
        DontDestroyOnLoad(this);
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        Invoke("ReloadScene", 2f);
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
            transform.position = Player.Instance.transform.position;
            _ui.gameObject.SetActive(true);
            Player.Instance.Pause(true);
            Player.Instance.Lose();
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
    
    public void RetryGame()
    {
        Player.Instance.Pause(false);
        Player.Instance.SetState(Player.PlayerState.Idle);
        Destroy(gameObject);
    }
}

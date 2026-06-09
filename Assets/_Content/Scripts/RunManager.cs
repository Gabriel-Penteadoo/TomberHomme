using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Central manager for a level run. It owns the on-screen timer, the current
/// respawn point (updated by <see cref="Checkpoint"/>) and the win screen shown
/// by <see cref="FinishLine"/> with a per-checkpoint recap.
///
/// A single instance is expected per gameplay scene. If a <see cref="Checkpoint"/>
/// or <see cref="FinishLine"/> is triggered while none exists, one is created
/// automatically so the system works even when nothing is wired in the scene.
/// </summary>
public class RunManager : MonoBehaviour
{
    [System.Serializable]
    public class Split
    {
        [Tooltip("Checkpoint label shown in the recap")]
        public string Name;

        [Tooltip("Total elapsed time when the checkpoint was reached")]
        public float Time;

        [Tooltip("Time spent on the segment leading to this checkpoint")]
        public float Segment;
    }

    [System.Serializable]
    public class Settings
    {
        [Tooltip("Scene loaded by the win screen 'Menu' button")]
        public string MenuScene = "MenuPrincipal";

        [Tooltip("Delay (death animation) before respawning at the last checkpoint")]
        public float RespawnDelay = 1f;
    }

    [SerializeField] private Settings _settings = new Settings();
    [SerializeField, ReadOnly] private float _elapsed;
    [SerializeField, ReadOnly] private List<Split> _splits = new List<Split>();

    public float Elapsed => _elapsed;
    public bool HasRespawn => _hasRespawn;
    public Vector3 RespawnPosition => _respawnPosition;
    public Quaternion RespawnRotation => _respawnRotation;
    public float RespawnDelay => _settings.RespawnDelay;
    public IReadOnlyList<Split> Splits => _splits;

    #region Singleton
    private static RunManager _instance;

    public static bool HasInstance => _instance != null;

    public static RunManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<RunManager>();

                if (_instance == null)
                    _instance = new GameObject("RunManager").AddComponent<RunManager>();
            }

            return _instance;
        }
    }
    #endregion

    #region State
    private bool _running;
    private bool _finished;

    private Vector3 _respawnPosition;
    private Quaternion _respawnRotation;
    private bool _hasRespawn;
    private float _lastSplitTime;
    #endregion

    #region UI
    private TMP_Text _timerLabel;
    private TMP_Text _countdownLabel;
    private GameObject _winPanel;
    private TMP_Text _winTotalLabel;
    private RectTransform _winRecapRoot;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    void Start()
    {
        BuildUI();

        // Hold the run behind a Mario Kart-style "3, 2, 1, GO!" countdown on the
        // race scenes; everywhere else the run starts immediately.
        if (ShouldPlayCountdown())
            StartCoroutine(CountdownRoutine());
        else
            _running = true;
    }

    void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    void Update()
    {
        // Capture the player's starting transform as the initial respawn point.
        if (!_hasRespawn && Player.Instance)
        {
            Transform t = Player.Instance.transform;
            SetRespawn(t.position, t.rotation);
        }

        // Timer keeps running through deaths/respawns so they cost time.
        if (_running && !_finished)
            _elapsed += Time.deltaTime;

        if (_timerLabel)
            _timerLabel.text = FormatTime(_elapsed);
    }
    #endregion

    #region Countdown
    // Scenes that open with the start countdown. Other scenes start their run
    // immediately, so a RunManager spawned on demand there never freezes the game.
    private static readonly string[] CountdownScenes = { "PlatformerScene", "RampUp" };

    private static bool ShouldPlayCountdown()
    {
        string active = SceneManager.GetActiveScene().name;
        foreach (string scene in CountdownScenes)
            if (active == scene)
                return true;

        return false;
    }

    /// <summary>
    /// Freezes the world (timeScale 0) and counts "3, 2, 1, GO!" before releasing
    /// control and starting the timer. Runs on unscaled time so it ticks while frozen.
    /// </summary>
    private IEnumerator CountdownRoutine()
    {
        Time.timeScale = 0f;

        string[] steps = { "3", "2", "1", "GO!" };
        Color number = Color.white;
        Color go = new Color(0.4f, 1f, 0.4f);

        for (int i = 0; i < steps.Length; i++)
        {
            bool isGo = i == steps.Length - 1;

            _countdownLabel.text = steps[i];
            _countdownLabel.color = isGo ? go : number;
            _countdownLabel.gameObject.SetActive(true);

            yield return PopLabel(_countdownLabel.rectTransform, isGo ? 0.7f : 1f);
        }

        _countdownLabel.gameObject.SetActive(false);

        Time.timeScale = 1f;
        _running = true;
    }

    /// <summary>Quick scale punch that settles to normal size, held for the rest of the beat.</summary>
    private static IEnumerator PopLabel(RectTransform rect, float duration)
    {
        const float popInPortion = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float settle = Mathf.Clamp01(elapsed / (duration * popInPortion));
            rect.localScale = Vector3.one * Mathf.Lerp(1.7f, 1f, settle);
            yield return null;
        }

        rect.localScale = Vector3.one;
    }
    #endregion

    #region Public API
    /// <summary>Registers a checkpoint: updates the respawn point and records a split.</summary>
    public void SetCheckpoint(string label, Vector3 position, Quaternion rotation)
    {
        SetRespawn(position, rotation);

        _splits.Add(new Split
        {
            Name = string.IsNullOrEmpty(label) ? $"Checkpoint {_splits.Count + 1}" : label,
            Time = _elapsed,
            Segment = _elapsed - _lastSplitTime,
        });

        _lastSplitTime = _elapsed;
    }

    /// <summary>Ends the run, freezes the timer and shows the win screen.</summary>
    public void Finish()
    {
        if (_finished)
            return;

        _finished = true;
        _running = false;

        if (Player.Instance)
            Player.Instance.Win();

        ShowWinScreen();
    }

    public void RetryRun()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(_settings.MenuScene))
            SceneManager.LoadScene(_settings.MenuScene);
    }
    #endregion

    #region Internals
    private void SetRespawn(Vector3 position, Quaternion rotation)
    {
        _respawnPosition = position;
        _respawnRotation = rotation;
        _hasRespawn = true;
    }

    private static string FormatTime(float seconds)
    {
        if (seconds < 0) seconds = 0;
        int minutes = (int)(seconds / 60f);
        float rest = seconds - minutes * 60f;
        return $"{minutes:00}:{rest:00.000}";
    }
    #endregion

    #region UI Construction
    private void BuildUI()
    {
        EnsureEventSystem();

        Canvas canvas = CreateCanvas("RunCanvas", 100);

        // Permanent timer, top-center of the screen.
        _timerLabel = CreateText(canvas.transform, "Timer", "00:00.000", 48, TextAlignmentOptions.Center);
        RectTransform timerRect = _timerLabel.rectTransform;
        timerRect.anchorMin = new Vector2(0.5f, 1f);
        timerRect.anchorMax = new Vector2(0.5f, 1f);
        timerRect.pivot = new Vector2(0.5f, 1f);
        timerRect.anchoredPosition = new Vector2(0f, -24f);
        timerRect.sizeDelta = new Vector2(400f, 80f);
        _timerLabel.fontStyle = FontStyles.Bold;
        _timerLabel.outlineWidth = 0.2f;
        _timerLabel.outlineColor = new Color32(0, 0, 0, 200);

        // Big centered countdown number, hidden until the start sequence plays.
        _countdownLabel = CreateText(canvas.transform, "Countdown", "", 220, TextAlignmentOptions.Center);
        RectTransform countdownRect = _countdownLabel.rectTransform;
        countdownRect.anchorMin = new Vector2(0.5f, 0.5f);
        countdownRect.anchorMax = new Vector2(0.5f, 0.5f);
        countdownRect.pivot = new Vector2(0.5f, 0.5f);
        countdownRect.anchoredPosition = Vector2.zero;
        countdownRect.sizeDelta = new Vector2(700f, 320f);
        _countdownLabel.fontStyle = FontStyles.Bold;
        _countdownLabel.outlineWidth = 0.25f;
        _countdownLabel.outlineColor = new Color32(0, 0, 0, 220);
        _countdownLabel.gameObject.SetActive(false);

        BuildWinPanel(canvas.transform);
    }

    private void BuildWinPanel(Transform parent)
    {
        _winPanel = new GameObject("WinPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        _winPanel.transform.SetParent(parent, false);
        _winPanel.layer = LayerMask.NameToLayer("UI");

        RectTransform panelRect = (RectTransform)_winPanel.transform;
        Stretch(panelRect);

        Image bg = _winPanel.GetComponent<Image>();
        bg.color = new Color(0.02f, 0.03f, 0.08f, 0.85f);

        // Title.
        TMP_Text title = CreateText(_winPanel.transform, "Title", "FINISH!", 96, TextAlignmentOptions.Center);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -80f);
        titleRect.sizeDelta = new Vector2(800f, 140f);
        title.fontStyle = FontStyles.Bold;

        // Total time.
        _winTotalLabel = CreateText(_winPanel.transform, "Total", "00:00.000", 56, TextAlignmentOptions.Center);
        RectTransform totalRect = _winTotalLabel.rectTransform;
        totalRect.anchorMin = new Vector2(0.5f, 1f);
        totalRect.anchorMax = new Vector2(0.5f, 1f);
        totalRect.pivot = new Vector2(0.5f, 1f);
        totalRect.anchoredPosition = new Vector2(0f, -220f);
        totalRect.sizeDelta = new Vector2(800f, 90f);

        // Recap list container with vertical layout.
        GameObject recap = new GameObject("Recap", typeof(RectTransform), typeof(VerticalLayoutGroup));
        recap.transform.SetParent(_winPanel.transform, false);
        _winRecapRoot = (RectTransform)recap.transform;
        _winRecapRoot.anchorMin = new Vector2(0.5f, 0.5f);
        _winRecapRoot.anchorMax = new Vector2(0.5f, 0.5f);
        _winRecapRoot.pivot = new Vector2(0.5f, 1f);
        _winRecapRoot.anchoredPosition = new Vector2(0f, 60f);
        _winRecapRoot.sizeDelta = new Vector2(900f, 0f);

        VerticalLayoutGroup layout = recap.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.spacing = 6f;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        // Buttons.
        CreateButton(_winPanel.transform, "RetryButton", "RETRY", new Vector2(-180f, 80f), RetryRun);
        CreateButton(_winPanel.transform, "MenuButton", "MENU", new Vector2(180f, 80f), GoToMenu);

        _winPanel.SetActive(false);
    }

    private void ShowWinScreen()
    {
        if (!_winPanel)
            return;

        _winTotalLabel.text = "TIME  " + FormatTime(_elapsed);

        // Clear previous rows (e.g. when reusing the panel).
        for (int i = _winRecapRoot.childCount - 1; i >= 0; i--)
            Destroy(_winRecapRoot.GetChild(i).gameObject);

        if (_splits.Count == 0)
        {
            AddRecapRow("No checkpoints reached");
        }
        else
        {
            foreach (Split split in _splits)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(split.Name);
                sb.Append("    ");
                sb.Append(FormatTime(split.Time));
                sb.Append("   (+");
                sb.Append(FormatTime(split.Segment));
                sb.Append(')');
                AddRecapRow(sb.ToString());
            }
        }

        _winPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void AddRecapRow(string content)
    {
        TMP_Text row = CreateText(_winRecapRoot, "Row", content, 36, TextAlignmentOptions.Center);
        LayoutElement le = row.gameObject.AddComponent<LayoutElement>();
        le.minHeight = 44f;
    }
    #endregion

    #region UI Helpers
    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject es = new GameObject("EventSystem", typeof(EventSystem));
        es.AddComponent<InputSystemUIInputModule>();
    }

    private static Canvas CreateCanvas(string name, int sortOrder)
    {
        GameObject go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.layer = LayerMask.NameToLayer("UI");

        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortOrder;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private static TMP_Text CreateText(Transform parent, string name, string content, float size, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");

        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = size;
        text.alignment = align;
        text.color = Color.white;
        text.raycastTarget = false;

        if (TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;

        return text;
    }

    private void CreateButton(Transform parent, string name, string label, Vector2 anchoredPos, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");

        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(280f, 90f);

        Image img = go.GetComponent<Image>();
        img.color = new Color(0.15f, 0.5f, 0.85f, 1f);

        Button button = go.GetComponent<Button>();
        button.targetGraphic = img;
        button.onClick.AddListener(onClick);

        TMP_Text text = CreateText(go.transform, "Label", label, 40, TextAlignmentOptions.Center);
        text.fontStyle = FontStyles.Bold;
        Stretch(text.rectTransform);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
    #endregion
}

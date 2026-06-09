using UnityEngine;
using UnityEngine.UI;

public class StyleMenuUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject StylePanel;
    public Transform ColorButtonParent;
    public GameObject ColorButtonPrefab;

    private GameObject _accessorySection;

    void Start()
    {
        StylePanel.SetActive(false);
        BuildColorButtons();
        Invoke(nameof(BuildAccessoryButtons), 0.1f);
    }

    public void TogglePanel()
    {
        bool next = !StylePanel.activeSelf;
        StylePanel.SetActive(next);
        if (_accessorySection != null) _accessorySection.SetActive(next);
    }

    private void BuildColorButtons()
    {
        if (PlayerCustomization.Instance == null) return;

        GridLayoutGroup grid = ColorButtonParent.GetComponent<GridLayoutGroup>()
                               ?? ColorButtonParent.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(50, 50);
        grid.spacing = new Vector2(5, 5);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;

        Color[] colors = PlayerCustomization.Instance.AvailableColors;
        for (int i = 0; i < colors.Length; i++)
        {
            int index = i;
            GameObject btn = Instantiate(ColorButtonPrefab, ColorButtonParent);
            btn.GetComponent<Image>().color = colors[i];
            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                PlayerCustomization.Instance.SelectColor(index);
                PlayerAppearance appearance = FindObjectOfType<PlayerAppearance>();
                if (appearance != null) appearance.ApplyColor();
            });
        }
    }

    private void BuildAccessoryButtons()
    {
        PlayerAppearance appearance = FindObjectOfType<PlayerAppearance>();
        if (appearance == null || PlayerCustomization.Instance == null) return;

        string[] accessories = appearance.GetAccessoryNames();
        if (accessories.Length == 0) return;

        // Panneau avec fond blanc, positionné sous le StylePanel
        GameObject section = new GameObject("AccessorySection", typeof(RectTransform));
        section.transform.SetParent(StylePanel.transform.parent, false);

        RectTransform sectionRT = section.GetComponent<RectTransform>();
        // Copie l'ancre du StylePanel et décale vers le bas
        RectTransform panelRT = StylePanel.GetComponent<RectTransform>();
        sectionRT.anchorMin = panelRT.anchorMin;
        sectionRT.anchorMax = panelRT.anchorMax;
        sectionRT.pivot = new Vector2(0.5f, 1f);
        sectionRT.anchoredPosition = panelRT.anchoredPosition + new Vector2(0f, -panelRT.sizeDelta.y - 20f);

        int cols = 3;
        float cellW = 80f, cellH = 32f, pad = 10f, spacing = 8f;
        int rows = Mathf.CeilToInt((accessories.Length + 1f) / cols); // +1 pour "Aucun"
        sectionRT.sizeDelta = new Vector2(cols * cellW + (cols - 1) * spacing + pad * 2,
                                          rows * cellH + (rows - 1) * spacing + pad * 2);

        Image bg = section.AddComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.95f);

        // Grid intérieure
        GameObject gridGO = new GameObject("AccessoryGrid", typeof(RectTransform));
        gridGO.transform.SetParent(section.transform, false);

        RectTransform gridRT = gridGO.GetComponent<RectTransform>();
        gridRT.anchorMin = Vector2.zero;
        gridRT.anchorMax = Vector2.one;
        gridRT.offsetMin = new Vector2(pad, pad);
        gridRT.offsetMax = new Vector2(-pad, -pad);

        GridLayoutGroup grid = gridGO.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(cellW, cellH);
        grid.spacing = new Vector2(spacing, spacing);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = cols;
        grid.childAlignment = TextAnchor.UpperLeft;

        CreateAccessoryButton(gridGO.transform, "Aucun", "");
        foreach (string name in accessories)
            CreateAccessoryButton(gridGO.transform, name, name);

        // Le panneau suit la visibilité du StylePanel
        section.SetActive(StylePanel.activeSelf);
        _accessorySection = section;
    }

    private void CreateAccessoryButton(Transform parent, string label, string accessoryName)
    {
        GameObject btnGO = new GameObject(label, typeof(RectTransform));
        btnGO.transform.SetParent(parent, false);

        Image img = btnGO.AddComponent<Image>();
        img.color = new Color(0.88f, 0.88f, 0.88f, 1f);

        Button btn = btnGO.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0.88f, 0.88f, 0.88f);
        cb.highlightedColor = new Color(0.7f, 0.85f, 1f);
        cb.pressedColor = new Color(0.4f, 0.6f, 1f);
        btn.colors = cb;

        GameObject textGO = new GameObject("Label", typeof(RectTransform));
        textGO.transform.SetParent(btnGO.transform, false);

        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        Text txt = textGO.AddComponent<Text>();
        txt.text = label;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 13;
        txt.color = new Color(0.15f, 0.15f, 0.15f);
        txt.alignment = TextAnchor.MiddleCenter;

        string nameToSelect = accessoryName;
        btn.onClick.AddListener(() =>
        {
            PlayerCustomization.Instance.SelectAccessory(nameToSelect);
            PlayerAppearance appearance = FindObjectOfType<PlayerAppearance>();
            if (appearance != null) appearance.ApplyAccessory();
        });
    }
}
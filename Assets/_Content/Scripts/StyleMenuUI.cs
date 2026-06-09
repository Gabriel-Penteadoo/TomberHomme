using UnityEngine;
using UnityEngine.UI;

public class StyleMenuUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject StylePanel;       // Panel qui contient les boutons couleur
    public Transform ColorButtonParent; // Layout group parent des boutons
    public GameObject ColorButtonPrefab; // Bouton avec une Image

    void Start()
    {
        StylePanel.SetActive(false);
        BuildColorButtons();
    } 

    public void TogglePanel()
    {
        StylePanel.SetActive(!StylePanel.activeSelf);
    }
    
    
    private void BuildColorButtons()
    {
        if (PlayerCustomization.Instance == null) return;

        // Setup grille sur le parent
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
}
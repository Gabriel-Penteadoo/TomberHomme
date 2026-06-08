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

        Color[] colors = PlayerCustomization.Instance.AvailableColors;

        for (int i = 0; i < colors.Length; i++)
        {
            int index = i; // capture pour le lambda
            GameObject btn = Instantiate(ColorButtonPrefab, ColorButtonParent);

            // Colorie le bouton pour qu'on voit la couleur
            btn.GetComponent<Image>().color = colors[i];

            // Au clic : sauvegarde + feedback visuel (optionnel)
            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                PlayerCustomization.Instance.SelectColor(index);
                // Si le player est déjà en scène (preview dans le menu), applique direct
                PlayerAppearance appearance = FindObjectOfType<PlayerAppearance>();
                if (appearance != null) appearance.ApplyColor();
            });
        }
    }
}
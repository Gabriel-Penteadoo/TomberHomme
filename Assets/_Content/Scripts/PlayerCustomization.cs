using UnityEngine;

public class PlayerCustomization : MonoBehaviour
{
    public static PlayerCustomization Instance { get; private set; }

    // Couleurs disponibles
    public Color[] AvailableColors = new Color[]
    {
        Color.white,
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        new Color(1f, 0.5f, 0f), // orange
        new Color(0.5f, 0f, 1f), // violet
        Color.black,
    };

    public int SelectedColorIndex { get; private set; } = 0;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Restaure le choix précédent
        SelectedColorIndex = PlayerPrefs.GetInt("PlayerColorIndex", 0);
    }

    public void SelectColor(int index)
    {
        SelectedColorIndex = Mathf.Clamp(index, 0, AvailableColors.Length - 1);
        PlayerPrefs.SetInt("PlayerColorIndex", SelectedColorIndex);
        PlayerPrefs.Save();
    }

    public Color GetSelectedColor() => AvailableColors[SelectedColorIndex];
}
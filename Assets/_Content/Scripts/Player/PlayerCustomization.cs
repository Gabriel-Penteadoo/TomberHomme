using UnityEngine;

public class PlayerCustomization : MonoBehaviour
{
    public static PlayerCustomization Instance { get; private set; }

    public Color[] AvailableColors = new Color[]
    {
        Color.white,
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        new Color(1f, 0.5f, 0f),
        new Color(0.5f, 0f, 1f),
        Color.black,
    };

    public int SelectedColorIndex { get; private set; } = 0;
    public string SelectedAccessoryName { get; private set; } = "";

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SelectedColorIndex = PlayerPrefs.GetInt("PlayerColorIndex", 0);
        SelectedAccessoryName = PlayerPrefs.GetString("PlayerAccessoryName", "");
    }

    public void SelectColor(int index)
    {
        SelectedColorIndex = Mathf.Clamp(index, 0, AvailableColors.Length - 1);
        PlayerPrefs.SetInt("PlayerColorIndex", SelectedColorIndex);
        PlayerPrefs.Save();
    }

    // "" = aucun accessoire
    public void SelectAccessory(string name)
    {
        SelectedAccessoryName = name;
        PlayerPrefs.SetString("PlayerAccessoryName", name);
        PlayerPrefs.Save();
    }

    public Color GetSelectedColor() => AvailableColors[SelectedColorIndex];
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAppearance : MonoBehaviour
{
    [Tooltip("Le renderer du body (mesh body sous accessories)")]
    public Renderer BodyRenderer;

    [Tooltip("Index du material M_bodywork_A dans BodyRenderer.materials")]
    public int BodyworkMaterialIndex = 3;

    // Nœud parent "accessories" dans la hiérarchie du bot
    private Transform _accessoriesNode;

    void Awake()
    {
        _accessoriesNode = FindDeep(transform.root, "accessories");
    }

    void Start()
    {
        // Realtime delay (not Invoke/WaitForSeconds) so the appearance still applies
        // while the start countdown freezes the game with Time.timeScale = 0.
        StartCoroutine(ApplyAllDelayed());
    }

    private IEnumerator ApplyAllDelayed()
    {
        yield return new WaitForSecondsRealtime(0.1f);
        ApplyAll();
    }

    public void ApplyAll()
    {
        ApplyColor();
        ApplyAccessory();
    }

    public void ApplyColor()
    {
        if (BodyRenderer == null) return;
        Material mat = BodyRenderer.materials[BodyworkMaterialIndex];
        mat.SetColor("_BaseColor", PlayerCustomization.Instance.GetSelectedColor());
    }

    // Retourne les noms de tous les accessoires détectés dans le bot
    public string[] GetAccessoryNames()
    {
        if (_accessoriesNode == null) return new string[0];
        var names = new List<string>();
        foreach (Transform child in _accessoriesNode)
            names.Add(child.name);
        return names.ToArray();
    }

    public void ApplyAccessory()
    {
        if (_accessoriesNode == null || PlayerCustomization.Instance == null) return;
        string selected = PlayerCustomization.Instance.SelectedAccessoryName;
        foreach (Transform child in _accessoriesNode)
            child.gameObject.SetActive(child.name == selected);
    }

    private Transform FindDeep(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (string.Equals(child.name, name, System.StringComparison.OrdinalIgnoreCase)) return child;
        return null;
    }
}
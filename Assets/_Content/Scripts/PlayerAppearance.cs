using UnityEngine;

public class PlayerAppearance : MonoBehaviour
{
    [Tooltip("Le renderer du body (mesh body sous accessories)")]
    public Renderer BodyRenderer;

    [Tooltip("Index du material M_bodywork_A dans BodyRenderer.materials")]
    public int BodyworkMaterialIndex = 3;

    void Start()
    {
        Invoke(nameof(ApplyColor), 0.1f);
    }

    public void ApplyColor()
    {
        if (BodyRenderer == null) return;

        Material mat = BodyRenderer.materials[BodyworkMaterialIndex];
        mat.SetColor("_BaseColor", PlayerCustomization.Instance.GetSelectedColor());
    }
}
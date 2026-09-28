using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Aplica esquinas redondeadas a un Image de UI usando el shader "UI/RoundedCorners".
/// Funciona también en modo edición gracias a [ExecuteAlways].
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Image))]
public class Modificador : MonoBehaviour
{
    /// <summary>Radio de las esquinas en píxeles.</summary>
    [Range(0, 100)]
    public float cornerRadius = 20f;

    private Image img;
    private RectTransform rt;
    private Material mat;

    void OnEnable()
    {
        img = GetComponent<Image>();
        rt = GetComponent<RectTransform>();

        Shader sh = Shader.Find("UI/RoundedCorners");
        if (sh == null)
        {
            Debug.LogError("No se encontró el shader UI/RoundedCorners");
            return;
        }

        // Material propio para este Image (no se guarda en la escena)
        mat = new Material(sh);
        mat.hideFlags = HideFlags.HideAndDontSave;
        img.material = mat;

        Actualizar();
    }

    void OnDisable()
    {
        // Regresa al material por defecto y libera el creado
        if (img != null) img.material = null;
        if (mat != null)
        {
            if (Application.isPlaying) Destroy(mat);
            else DestroyImmediate(mat);
            mat = null;
        }
    }

    void Update()
    {
        Actualizar();
    }

    /// <summary>Envía al shader el radio y el tamaño actual del rect.</summary>
    private void Actualizar()
    {
        if (mat == null) return;

        Rect r = rt.rect;
        mat.SetFloat("_Radius", cornerRadius);
        mat.SetFloat("_Width", r.width);
        mat.SetFloat("_Height", r.height);
    }
}

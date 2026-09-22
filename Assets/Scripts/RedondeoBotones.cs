using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Le pone esquinas redondeadas a cualquier Image de UI sin necesidad de
/// un sprite hecho a mano. Genera por código una textura redondeada y la
/// usa como sprite "Sliced", así las esquinas no se deforman aunque
/// cambies el tamaño del objeto. El color se sigue controlando desde el Image.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Image))]
public class RedondeoBotones : MonoBehaviour
{
    /// <summary>Radio de las esquinas. Para forma de pastilla usa la mitad del alto.</summary>
    [SerializeField, Min(1)] private float radius = 24f;

    /// <summary>Sprites ya generados, uno por radio, para no crear texturas repetidas.</summary>
    private static readonly Dictionary<int, Sprite> cache = new Dictionary<int, Sprite>();

    private void OnEnable() => Apply();

    private void OnValidate() => Apply();

    /// <summary>Asigna el sprite redondeado al Image.</summary>
    private void Apply()
    {
        Image image = GetComponent<Image>();
        if (image == null)
            return;

        image.sprite = GetSprite(Mathf.RoundToInt(radius));
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 1f;
    }

    /// <summary>Devuelve (o crea) el sprite redondeado para un radio dado.</summary>
    private static Sprite GetSprite(int r)
    {
        if (cache.TryGetValue(r, out Sprite cached) && cached != null)
            return cached;

        int size = r * 2 + 2;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontSave
        };

        Color32[] pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Centro del pixel y centro de la esquina más cercana
                float px = x + 0.5f;
                float py = y + 0.5f;
                float cx = Mathf.Clamp(px, r, size - r);
                float cy = Mathf.Clamp(py, r, size - r);

                float dist = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));

                // Borde suavizado (antialias) de 1 pixel
                float alpha = Mathf.Clamp01(r - dist + 0.5f);

                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        Sprite sprite = Sprite.Create(
            tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(r, r, r, r));

        sprite.hideFlags = HideFlags.DontSave;
        cache[r] = sprite;
        return sprite;
    }
}
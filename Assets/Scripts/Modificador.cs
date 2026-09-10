using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script que aplica esquinas redondeadas dinámicamente a un Image de UI
/// usando un shader personalizado.
/// </summary>
[RequireComponent(typeof(Image))]
public class Modificador : MonoBehaviour
{
    /// <summary>
    /// Radio de las esquinas redondeadas (0 - 100).
    /// Se puede modificar desde el Inspector en tiempo real.
    /// </summary>
    [Range(0, 100)]
    public float cornerRadius = 20f;

    /// <summary>
    /// Material instanciado que usa el shader de esquinas redondeadas.
    /// </summary>
    private Material mat;

    // =========================
    // INICIALIZACIÓN
    // =========================

    void Awake()
    {
        // Obtiene el componente Image automáticamente (garantizado por RequireComponent)
        Image img = GetComponent<Image>();

        // Crea un nuevo material basado en el shader personalizado
        mat = new Material(Shader.Find("UI/RoundedCorners"));

        // Asigna el material al Image
        img.material = mat;
    }

    // =========================
    // ACTUALIZACIÓN
    // =========================

    void Update()
    {
        // Actualiza el valor del radio en el shader cada frame
        if (mat != null)
        {
            mat.SetFloat("_Radius", cornerRadius);
        }
    }
}
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script simple y reutilizable para el botón de "regresar": cierra el panel
/// actual y muestra el panel anterior. Se puede usar en cualquier pantalla
/// que necesite un botón de "volver", solo asignando los paneles correctos
/// desde el Inspector.
/// </summary>
public class RegresarPanelIdioma : MonoBehaviour
{
    /// <summary>El panel que se va a cerrar al presionar el botón.</summary>
    [Header("Panel que se va a cerrar")]
    public GameObject panelActual;

    /// <summary>El panel que se va a mostrar al presionar el botón.</summary>
    [Header("Panel al que quieres volver")]
    public GameObject panelAnterior;

    /// <summary>Botón que dispara la acción de regresar.</summary>
    [Header("Botón de regresar")]
    public Button botonRegresar;

    /// <summary>
    /// Al iniciar, conecta el botón de regresar con su función correspondiente.
    /// </summary>
    void Start()
    {
        if (botonRegresar != null)
        {
            botonRegresar.onClick.RemoveAllListeners();
            botonRegresar.onClick.AddListener(Regresar);
        }
    }

    /// <summary>
    /// Cierra el panel actual y muestra el panel anterior.
    /// </summary>
    public void Regresar()
{
    if (panelActual != null) panelActual.SetActive(false);
    if (panelAnterior != null) panelAnterior.SetActive(true);
}
}
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Versión simple y directa de un botón de "regresar": cierra el panel
/// actual y muestra el panel del menú. Hace lo mismo que
/// <c>RegresarPanelIdioma</c>, pero con nombres de campos fijos ("panel
/// actual" / "panel menú") en vez de genéricos, para casos donde Unity
/// se pone necio con las referencias y conviene tener una versión
/// alternativa más directa.
/// </summary>
public class BotonRegresarSimple : MonoBehaviour
{
    /// <summary>El panel que se va a cerrar al presionar el botón.</summary>
    [Header("Panel actual")]
    public GameObject panelActual;

    /// <summary>El panel del menú al que se va a regresar.</summary>
    [Header("Panel al que vas a regresar")]
    public GameObject panelMenu;

    /// <summary>Botón que dispara la acción de regresar.</summary>
    [Header("Botón regresar")]
    public Button botonRegresar;

    /// <summary>
    /// Al iniciar, conecta el botón de regresar con su función correspondiente.
    /// </summary>
    void Start()
    {
        if (botonRegresar != null)
        {
            botonRegresar.onClick.RemoveAllListeners();
            botonRegresar.onClick.AddListener(RegresarAlMenu);
        }
    }

    /// <summary>
    /// Cierra el panel actual y muestra el panel del menú.
    /// </summary>
    public void RegresarAlMenu()
    {
        if (panelActual != null)
            panelActual.SetActive(false);

        if (panelMenu != null)
            panelMenu.SetActive(true);
    }
}
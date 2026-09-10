using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Este script controla la navegación principal del menú de lotería:
/// desde ahí el jugador puede ir a crear una lotería nueva, o ir a
/// jugar una lotería ya existente.
/// </summary>
public class NewLotery : MonoBehaviour
{
    // =========================
    // BOTONES
    // =========================

    /// <summary>Botón para ir al panel de crear una lotería nueva.</summary>
    public Button btnNuevaLoteria;

    /// <summary>Botón para ir al panel de jugar una lotería.</summary>
    public Button btnJugar;

    // =========================
    // PANELES
    // =========================

    /// <summary>Panel principal del menú.</summary>
    public GameObject panelMenu;

    /// <summary>Panel donde se crea una lotería nueva.</summary>
    public GameObject panelCrearLoteria;

    /// <summary>Panel donde se juega una lotería.</summary>
    public GameObject panelJugarLoteria;

    // =========================
    // INICIALIZACIÓN
    // =========================

    /// <summary>
    /// Al iniciar, conecta los dos botones con sus funciones correspondientes.
    /// Si algún botón no está asignado en el Inspector, avisa con un error
    /// en la consola para que sea fácil detectarlo.
    /// </summary>
    void Start()
    {
        // Configuración del botón "Nueva Lotería"
        if (btnNuevaLoteria != null)
        {
            btnNuevaLoteria.onClick.RemoveAllListeners();
            btnNuevaLoteria.onClick.AddListener(AbrirPanelCrear);
        }
        else
        {
            Debug.LogError("btnNuevaLoteria no está asignado");
        }

        // Configuración del botón "Jugar"
        if (btnJugar != null)
        {
            btnJugar.onClick.RemoveAllListeners();
            btnJugar.onClick.AddListener(AbrirPanelJugar);
        }
        else
        {
            Debug.LogError("btnJugar no está asignado");
        }
    }

    // =========================
    // CAMBIO DE PANELES
    // =========================

    /// <summary>
    /// Abre el panel de creación de una lotería nueva.
    /// </summary>
    void AbrirPanelCrear()
    {
        CambiarPanel(panelCrearLoteria);
        Debug.Log("Transición al panel de creación de lotería");
    }

    /// <summary>
    /// Abre el panel de jugar una lotería.
    /// </summary>
    void AbrirPanelJugar()
    {
        CambiarPanel(panelJugarLoteria);
        Debug.Log("Transición al panel de jugar lotería");
    }

    /// <summary>
    /// Oculta el menú principal y muestra el panel al que se quiere ir.
    /// </summary>
    /// <param name="panelDestino">Panel que se quiere mostrar.</param>
    void CambiarPanel(GameObject panelDestino)
    {
        // Oculta el menú principal
        if (panelMenu != null)
            panelMenu.SetActive(false);
        else
            Debug.LogWarning("panelMenu no está asignado");

        // Activa el panel destino
        if (panelDestino != null)
            panelDestino.SetActive(true);
        else
            Debug.LogWarning("panelDestino no está asignado");
    }
}
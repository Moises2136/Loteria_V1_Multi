using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Este script controla cómo se ve una tarjeta de lotería en la pantalla
/// (por ejemplo en una lista de loterías disponibles). Muestra su nombre,
/// si es pública o privada, cuántas cartas tiene, su imagen de portada,
/// y maneja los botones para editarla o jugarla.
/// </summary>
public class CardLoteriaUI : MonoBehaviour
{
    // =========================
    // ELEMENTOS UI
    // =========================

    /// <summary>Texto que muestra el nombre de la lotería.</summary>
    [Header("UI")]
    public TMP_Text txtNombre;

    /// <summary>Texto que indica si la lotería es pública o privada.</summary>
    public TMP_Text txtPrivadaPublica;

    /// <summary>Texto que muestra cuántas cartas tiene la lotería.</summary>
    public TMP_Text txtNumeroCartas;

    /// <summary>Imagen de portada de la lotería.</summary>
    public Image imgPortada;

    // =========================
    // BOTONES
    // =========================

    /// <summary>Botón para editar esta lotería.</summary>
    [Header("Botones")]
    public Button btnEditar;

    /// <summary>Botón para jugar esta lotería.</summary>
    public Button btnJugar;

    /// <summary>
    /// Guarda los datos de la lotería que se está mostrando actualmente en esta tarjeta.
    /// </summary>
    private DatosLoteria datosLoteria;

    /// <summary>
    /// Llena la tarjeta con la información de una lotería específica:
    /// su nombre, si es pública o privada, cuántas cartas tiene, su portada,
    /// y conecta los botones de editar/jugar a esa lotería.
    /// </summary>
    /// <param name="datos">Los datos de la lotería que se van a mostrar.</param>
    public void Configurar(DatosLoteria datos)
    {
        datosLoteria = datos;

        if (txtNombre != null)
            txtNombre.text = datos.nombreLoteria;

        if (txtPrivadaPublica != null)
            txtPrivadaPublica.text = datos.esPublica ? "Pública" : "Privada";

        if (txtNumeroCartas != null)
            txtNumeroCartas.text = datos.cartas.Count + " cartas";

        if (imgPortada != null)
            imgPortada.sprite = datos.imagenPortada;

        // Quitamos cualquier acción anterior antes de asignar la nueva,
        // así evitamos que el botón haga cosas duplicadas si esta tarjeta se reutiliza
        if (btnEditar != null)
        {
            btnEditar.onClick.RemoveAllListeners();
            btnEditar.onClick.AddListener(EditarLoteria);
        }

        if (btnJugar != null)
        {
            btnJugar.onClick.RemoveAllListeners();
            btnJugar.onClick.AddListener(JugarLoteria);
        }
    }

    /// <summary>
    /// Se ejecuta cuando el jugador le da clic al botón de editar.
    /// </summary>
    void EditarLoteria()
    {
        Debug.Log("Editar lotería: " + datosLoteria.nombreLoteria);
    }

    /// <summary>
    /// Se ejecuta cuando el jugador le da clic al botón de jugar.
    /// </summary>
    void JugarLoteria()
    {
        Debug.Log("Jugar lotería: " + datosLoteria.nombreLoteria);
    }
}
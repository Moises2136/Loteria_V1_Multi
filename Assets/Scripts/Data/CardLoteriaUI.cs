using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Este script controla cómo se ve una tarjeta de lotería en la pantalla
/// (por ejemplo en una lista de loterías disponibles). Muestra su nombre,
/// si es pública o privada (traducido al idioma actual), cuántas cartas
/// tiene, una portada elegida de la carpeta local Resources/Portadas, y
/// maneja los botones para editarla o jugarla.
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

    // =========================
    // PORTADAS LOCALES
    // =========================

    /// <summary>
    /// Portadas cargadas una sola vez desde Assets/Resources/Portadas
    /// (compartidas por todas las tarjetas).
    /// </summary>
    private static Sprite[] portadas;

    // =========================
    // IDIOMA
    // =========================

    /// <summary>Se suscribe al cambio de idioma para volver a traducir el estado.</summary>
    void OnEnable() => LanguageManager.OnIdiomaAplicado += AlCambiarIdioma;

    /// <summary>Se da de baja del cambio de idioma para evitar fugas de memoria.</summary>
    void OnDisable() => LanguageManager.OnIdiomaAplicado -= AlCambiarIdioma;

    /// <summary>Cuando cambia el idioma, vuelve a escribir "Pública/Privada".</summary>
    void AlCambiarIdioma(LanguageManager.Language idioma) => PintarPrivacidad();

    /// <summary>Escribe "Pública" o "Privada" en el idioma actual.</summary>
    void PintarPrivacidad()
    {
        if (txtPrivadaPublica == null || datosLoteria == null) return;

        txtPrivadaPublica.text = datosLoteria.esPublica
            ? LanguageManager.Traducir("Pública", "Public", "Publique", "Pubblica")
            : LanguageManager.Traducir("Privada", "Private", "Privée", "Privata");
    }

    // =========================
    // CONFIGURAR
    // =========================

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

        PintarPrivacidad();

        if (txtNumeroCartas != null)
            txtNumeroCartas.text = datos.cartas.Count.ToString();

        if (imgPortada != null)
        {
            imgPortada.sprite = ObtenerPortada(datos.nombreLoteria);
            imgPortada.color = Color.white;   // quita el tinte negro del prefab
            imgPortada.preserveAspect = true;
        }

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

    // =========================
    // PORTADA
    // =========================

    /// <summary>
    /// Elige una portada de la carpeta Resources/Portadas. Usa el nombre de
    /// la lotería para que cada lotería tenga siempre la misma portada
    /// (no cambia cada vez que se abre la lista).
    /// </summary>
    /// <param name="clave">Texto con el que se elige la portada (ej. nombre de la lotería).</param>
    /// <returns>Un sprite de la carpeta, o null si está vacía.</returns>
    Sprite ObtenerPortada(string clave)
    {
        if (portadas == null)
            portadas = Resources.LoadAll<Sprite>("Portadas");

        if (portadas.Length == 0)
        {
            Debug.LogWarning("No hay imágenes en Assets/Resources/Portadas.");
            return null;
        }

        int hash = 0;
        foreach (char ch in clave ?? "")
            hash = hash * 31 + ch;

        return portadas[(hash & 0x7fffffff) % portadas.Length];

        // Si prefieres una portada al azar cada vez, usa esta línea en lugar de la anterior:
        // return portadas[Random.Range(0, portadas.Length)];
    }

    // =========================
    // BOTONES
    // =========================

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
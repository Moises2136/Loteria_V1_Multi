using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Este script controla el panel de clasificación (ranking) del juego:
/// ordena a los jugadores por puntaje de mayor a menor, muestra el Top 3
/// destacado, los lugares 4 y 5 en una lista, y en qué posición quedó
/// el jugador local.
/// </summary>
/// <remarks>
/// Los datos vienen por ahora de <see cref="nombresJugadores"/> y
/// <see cref="puntajesJugadores"/> como valores de ejemplo/demo, pero también
/// se pueden reemplazar desde otro script usando <see cref="SetRankingData"/>
/// (por ejemplo, para conectar datos reales en vez de los de prueba).
/// </remarks>
public class Panel_Clas : MonoBehaviour
{
    // =========================
    // TÍTULO
    // =========================

    /// <summary>Texto del título principal del panel (ej. "Clasificación").</summary>
    public TMP_Text tituloPanel;

    // =========================
    // TOP 3
    // =========================

    /// <summary>Texto que muestra al jugador en primer lugar.</summary>
    public TMP_Text textoPrimerLugar;

    /// <summary>Texto que muestra al jugador en segundo lugar.</summary>
    public TMP_Text textoSegundoLugar;

    /// <summary>Texto que muestra al jugador en tercer lugar.</summary>
    public TMP_Text textoTercerLugar;

    // =========================
    // LISTA INFERIOR
    // =========================

    /// <summary>Texto que muestra al jugador en cuarto lugar.</summary>
    public TMP_Text textoCuartoLugar;

    /// <summary>Texto que muestra al jugador en quinto lugar.</summary>
    public TMP_Text textoQuintoLugar;

    // =========================
    // TU LUGAR
    // =========================

    /// <summary>Texto fijo con el título "Tu lugar".</summary>
    public TMP_Text textoTuLugarTitulo;

    /// <summary>Texto que muestra en qué posición quedó el jugador local (ej. "#4").</summary>
    public TMP_Text textoTuLugarValor;

    // =========================
    // BOTÓN REGRESAR
    // =========================

    /// <summary>Botón para regresar al panel anterior.</summary>
    public Button botonRegresar;

    /// <summary>Este mismo panel de clasificación.</summary>
    public GameObject panelActual;

    /// <summary>Panel al que se regresa al presionar el botón.</summary>
    public GameObject panelAnterior;

    // =========================
    // DATOS DEMO
    // =========================

    /// <summary>
    /// Nombres de los jugadores a mostrar (valores de ejemplo por defecto;
    /// se pueden reemplazar con datos reales usando <see cref="SetRankingData"/>).
    /// </summary>
    public string[] nombresJugadores = new string[5] { "Ana", "Luis", "Carlos", "Maria", "Tu" };

    /// <summary>Puntajes de los jugadores, en el mismo orden que <see cref="nombresJugadores"/>.</summary>
    public int[] puntajesJugadores = new int[5] { 100, 80, 60, 40, 20 };

    /// <summary>Nombre del jugador local, usado para identificar y resaltar su posición.</summary>
    public string nombreJugadorLocal = "Tu";

    // =========================
    // INICIALIZACIÓN
    // =========================

    /// <summary>
    /// Al iniciar, conecta el botón de regresar con su función correspondiente.
    /// </summary>
    void Start()
    {
        ConfigurarBoton();
    }

    /// <summary>
    /// Cada vez que se activa este panel, recarga y vuelve a mostrar la clasificación.
    /// </summary>
    void OnEnable()
    {
        CargarClasificacion();
    }

    /// <summary>
    /// Conecta el botón de regresar con su función.
    /// </summary>
    void ConfigurarBoton()
    {
        if (botonRegresar != null)
        {
            botonRegresar.onClick.RemoveAllListeners();
            botonRegresar.onClick.AddListener(Regresar);
        }
    }

    // =========================
    // CARGA DE CLASIFICACIÓN
    // =========================

    /// <summary>
    /// Ordena a los jugadores de mayor a menor puntaje, y actualiza todos
    /// los textos en pantalla: el Top 3, los lugares 4 y 5, y la posición
    /// del jugador local.
    /// </summary>
    public void CargarClasificacion()
    {
        if (tituloPanel != null)
            tituloPanel.text = "Clasificación";

        // Validar existencia de datos
        if (nombresJugadores == null || puntajesJugadores == null)
        {
            Debug.LogWarning("No hay datos de clasificación.");
            LimpiarTextos();
            return;
        }

        // Validar integridad de datos
        if (nombresJugadores.Length != puntajesJugadores.Length || nombresJugadores.Length == 0)
        {
            Debug.LogWarning("Datos inválidos de clasificación.");
            LimpiarTextos();
            return;
        }

        int total = nombresJugadores.Length;

        // Copia de datos para no modificar los originales
        string[] nombresOrdenados = new string[total];
        int[] puntajesOrdenados = new int[total];

        for (int i = 0; i < total; i++)
        {
            nombresOrdenados[i] = nombresJugadores[i];
            puntajesOrdenados[i] = puntajesJugadores[i];
        }

        // Ordenamiento descendente por puntaje (de mayor a menor)
        for (int i = 0; i < total - 1; i++)
        {
            for (int j = i + 1; j < total; j++)
            {
                if (puntajesOrdenados[j] > puntajesOrdenados[i])
                {
                    int tempP = puntajesOrdenados[i];
                    puntajesOrdenados[i] = puntajesOrdenados[j];
                    puntajesOrdenados[j] = tempP;

                    string tempN = nombresOrdenados[i];
                    nombresOrdenados[i] = nombresOrdenados[j];
                    nombresOrdenados[j] = tempN;
                }
            }
        }

        // Mostrar Top 3
        if (textoPrimerLugar != null)
            textoPrimerLugar.text = FormatoTop(1, nombresOrdenados, puntajesOrdenados);

        if (textoSegundoLugar != null)
            textoSegundoLugar.text = FormatoTop(2, nombresOrdenados, puntajesOrdenados);

        if (textoTercerLugar != null)
            textoTercerLugar.text = FormatoTop(3, nombresOrdenados, puntajesOrdenados);

        // Mostrar lugares 4 y 5
        if (textoCuartoLugar != null)
            textoCuartoLugar.text = FormatoLista(4, nombresOrdenados, puntajesOrdenados);

        if (textoQuintoLugar != null)
            textoQuintoLugar.text = FormatoLista(5, nombresOrdenados, puntajesOrdenados);

        // Buscar la posición del jugador local dentro de la lista ya ordenada
        int indiceJugadorLocal = -1;
        for (int i = 0; i < total; i++)
        {
            if (nombresOrdenados[i].ToLower() == nombreJugadorLocal.ToLower())
            {
                indiceJugadorLocal = i;
                break;
            }
        }

        if (textoTuLugarTitulo != null)
            textoTuLugarTitulo.text = "Tu lugar";

        if (textoTuLugarValor != null)
        {
            if (indiceJugadorLocal >= 0)
                textoTuLugarValor.text = "#" + (indiceJugadorLocal + 1);
            else
                textoTuLugarValor.text = "#-";
        }
    }

    // =========================
    // FORMATOS DE TEXTO
    // =========================

    /// <summary>
    /// Arma el texto que se muestra para un lugar del Top 3 (nombre en
    /// una línea y puntaje en la siguiente).
    /// </summary>
    /// <param name="posicion">Posición solicitada (1, 2 o 3).</param>
    /// <param name="nombres">Arreglo de nombres ya ordenado por puntaje.</param>
    /// <param name="puntajes">Arreglo de puntajes ya ordenado, en el mismo orden que <paramref name="nombres"/>.</param>
    /// <returns>Texto formateado, por ejemplo "ANA\n100 PTS".</returns>
    string FormatoTop(int posicion, string[] nombres, int[] puntajes)
    {
        int index = posicion - 1;

        if (index < 0 || index >= nombres.Length || index >= puntajes.Length)
            return "---\n0 PTS";

        return nombres[index].ToUpper() + "\n" + puntajes[index] + " PTS";
    }

    /// <summary>
    /// Arma el texto que se muestra para los lugares 4 y 5 (nombre y
    /// puntaje en una sola línea).
    /// </summary>
    /// <param name="posicion">Posición solicitada (4 o 5).</param>
    /// <param name="nombres">Arreglo de nombres ya ordenado por puntaje.</param>
    /// <param name="puntajes">Arreglo de puntajes ya ordenado, en el mismo orden que <paramref name="nombres"/>.</param>
    /// <returns>Texto formateado, por ejemplo "MARIA - 40 PTS".</returns>
    string FormatoLista(int posicion, string[] nombres, int[] puntajes)
    {
        int index = posicion - 1;

        if (index < 0 || index >= nombres.Length || index >= puntajes.Length)
            return "--- - 0 PTS";

        return nombres[index].ToUpper() + " - " + puntajes[index] + " PTS";
    }

    // =========================
    // LIMPIEZA
    // =========================

    /// <summary>
    /// Deja todos los textos del panel con valores por defecto (guiones y "0 PTS"),
    /// usado cuando no hay datos válidos de clasificación.
    /// </summary>
    void LimpiarTextos()
    {
        if (textoPrimerLugar != null) textoPrimerLugar.text = "---\n0 PTS";
        if (textoSegundoLugar != null) textoSegundoLugar.text = "---\n0 PTS";
        if (textoTercerLugar != null) textoTercerLugar.text = "---\n0 PTS";
        if (textoCuartoLugar != null) textoCuartoLugar.text = "--- - 0 PTS";
        if (textoQuintoLugar != null) textoQuintoLugar.text = "--- - 0 PTS";
        if (textoTuLugarTitulo != null) textoTuLugarTitulo.text = "Tu lugar";
        if (textoTuLugarValor != null) textoTuLugarValor.text = "#-";
    }

    // =========================
    // NAVEGACIÓN
    // =========================

    /// <summary>
    /// Cierra este panel y regresa al panel anterior.
    /// </summary>
    public void Regresar()
    {
        if (panelActual != null)
            panelActual.SetActive(false);

        if (panelAnterior != null)
            panelAnterior.SetActive(true);
    }

    // =========================
    // CARGA EXTERNA DE DATOS
    // =========================

    /// <summary>
    /// Permite que otro script le pase datos reales de ranking a este panel
    /// (en vez de usar los datos de ejemplo), y recarga la vista de inmediato.
    /// </summary>
    /// <param name="nombres">Nombres de los jugadores.</param>
    /// <param name="puntajes">Puntajes de los jugadores, en el mismo orden que <paramref name="nombres"/>.</param>
    /// <param name="nombreLocal">Nombre del jugador local, para resaltar su posición.</param>
    public void SetRankingData(string[] nombres, int[] puntajes, string nombreLocal)
    {
        nombresJugadores = nombres;
        puntajesJugadores = puntajes;
        nombreJugadorLocal = nombreLocal;
        CargarClasificacion();
    }
}
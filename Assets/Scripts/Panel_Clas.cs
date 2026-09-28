using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controla el panel unificado de estadísticas y clasificación:
/// arriba muestra las estadísticas del jugador local (partidas, ganadas,
/// perdidas y % de victorias) y abajo el ranking real desde Firestore
/// (Top 3, lugares 4 y 5, y la posición del jugador local).
/// </summary>
/// <remarks>
/// Los datos se piden a <see cref="EstadisticasGameService"/> cada vez que el panel se activa.
/// </remarks>
public class Panel_Clas : MonoBehaviour
{
    // =========================
    // TÍTULO
    // =========================

    /// <summary>Texto del título principal del panel.</summary>
    public TMP_Text tituloPanel;

    // =========================
    // MIS ESTADÍSTICAS
    // =========================

    /// <summary>Valor de la tarjeta "Partidas jugadas".</summary>
    public TMP_Text textoPartidasJugadas;

    /// <summary>Valor de la tarjeta "Ganadas".</summary>
    public TMP_Text textoGanadas;

    /// <summary>Valor de la tarjeta "Perdidas".</summary>
    public TMP_Text textoPerdidas;

    /// <summary>Valor de la tarjeta "% de victorias".</summary>
    public TMP_Text textoPorcentajeVictorias;

    // =========================
    // TOP 3
    // =========================

    public TMP_Text textoPrimerLugar;
    public TMP_Text textoSegundoLugar;
    public TMP_Text textoTercerLugar;

    // =========================
    // LISTA INFERIOR
    // =========================

    public TMP_Text textoCuartoLugar;
    public TMP_Text textoQuintoLugar;

    // =========================
    // TU LUGAR
    // =========================

    /// <summary>Texto fijo con el título "Tu lugar".</summary>
    public TMP_Text textoTuLugarTitulo;

    /// <summary>Texto con la posición del jugador local (ej. "#4").</summary>
    public TMP_Text textoTuLugarValor;

    // =========================
    // BOTÓN REGRESAR
    // =========================

    public Button botonRegresar;
    public GameObject panelActual;
    public GameObject panelAnterior;

    /// <summary>
    /// Cuenta las cargas solicitadas. Si el panel se abre dos veces seguidas,
    /// las respuestas viejas de Firestore se ignoran para no pisar datos nuevos.
    /// </summary>
    int cargaActual = 0;

    const int TAMANO_TOP = 5;

    // =========================
    // INICIALIZACIÓN
    // =========================

    void Start()
    {
        ConfigurarBoton();
    }

    /// <summary>Cada vez que se activa el panel, vuelve a cargar los datos.</summary>
    void OnEnable()
    {
        CargarTodo();
    }

    void ConfigurarBoton()
    {
        if (botonRegresar != null)
        {
            botonRegresar.onClick.RemoveAllListeners();
            botonRegresar.onClick.AddListener(Regresar);
        }
    }

    // =========================
    // CARGA
    // =========================

    /// <summary>
    /// Pide al servicio las estadísticas del jugador, su posición y el Top 5.
    /// </summary>
    public void CargarTodo()
    {
        int idCarga = ++cargaActual;

        if (tituloPanel != null) tituloPanel.text = "Estadísticas";
        if (textoTuLugarTitulo != null) textoTuLugarTitulo.text = "Tu lugar";

        MostrarCargando();

        // Mis estadísticas y mi posición
        EstadisticasGameService.ObtenerMisEstadisticas(misStats =>
        {
            if (!SigueVigente(idCarga)) return;

            if (misStats == null || misStats.Value.partidasJugadas == 0)
            {
                MostrarMisEstadisticas(new EstadisticasJugador());
                SetTexto(textoTuLugarValor, "#-");
                return;
            }

            EstadisticasJugador stats = misStats.Value;
            MostrarMisEstadisticas(stats);

            EstadisticasGameService.ObtenerMiPosicion(stats.ganadas, posicion =>
            {
                if (!SigueVigente(idCarga)) return;
                SetTexto(textoTuLugarValor, posicion > 0 ? "#" + posicion : "#-");
            });
        });

        // Ranking
        EstadisticasGameService.ObtenerTop(TAMANO_TOP, top =>
        {
            if (!SigueVigente(idCarga)) return;
            MostrarRanking(top);
        });
    }

    /// <summary>
    /// Evita actualizar la UI si el panel se destruyó, se cerró
    /// o si ya hay una carga más reciente en curso.
    /// </summary>
    bool SigueVigente(int idCarga)
    {
        return this != null && isActiveAndEnabled && idCarga == cargaActual;
    }

    // =========================
    // MOSTRAR DATOS
    // =========================

    void MostrarMisEstadisticas(EstadisticasJugador stats)
    {
        SetTexto(textoPartidasJugadas, stats.partidasJugadas.ToString());
        SetTexto(textoGanadas, stats.ganadas.ToString());
        SetTexto(textoPerdidas, stats.perdidas.ToString());
        SetTexto(textoPorcentajeVictorias, stats.PorcentajeVictorias.ToString("0") + "%");
    }

    void MostrarRanking(List<EstadisticasJugador> top)
    {
        SetTexto(textoPrimerLugar, FormatoTop(0, top));
        SetTexto(textoSegundoLugar, FormatoTop(1, top));
        SetTexto(textoTercerLugar, FormatoTop(2, top));
        SetTexto(textoCuartoLugar, FormatoLista(3, top));
        SetTexto(textoQuintoLugar, FormatoLista(4, top));
    }

    void MostrarCargando()
    {
        SetTexto(textoPartidasJugadas, "-");
        SetTexto(textoGanadas, "-");
        SetTexto(textoPerdidas, "-");
        SetTexto(textoPorcentajeVictorias, "-");

        SetTexto(textoPrimerLugar, "...");
        SetTexto(textoSegundoLugar, "...");
        SetTexto(textoTercerLugar, "...");
        SetTexto(textoCuartoLugar, "...");
        SetTexto(textoQuintoLugar, "...");
        SetTexto(textoTuLugarValor, "...");
    }

    // =========================
    // FORMATOS DE TEXTO
    // =========================

    /// <summary>Formato del Top 3: nombre en una línea y victorias en la siguiente.</summary>
    string FormatoTop(int index, List<EstadisticasJugador> top)
    {
        if (index >= top.Count) return "---";
        return top[index].nombre.ToUpper() + "\n" + TextoVictorias(top[index].ganadas);
    }

    /// <summary>Formato de los lugares 4 y 5: nombre y victorias en una línea.</summary>
    string FormatoLista(int index, List<EstadisticasJugador> top)
    {
        if (index >= top.Count) return "---";
        return top[index].nombre.ToUpper() + " - " + TextoVictorias(top[index].ganadas);
    }

    string TextoVictorias(int ganadas)
    {
        return ganadas + (ganadas == 1 ? " victoria" : " victorias");
    }

    void SetTexto(TMP_Text texto, string valor)
    {
        if (texto != null) texto.text = valor;
    }

    // =========================
    // NAVEGACIÓN
    // =========================

    public void Regresar()
    {
        if (panelActual != null) panelActual.SetActive(false);
        if (panelAnterior != null) panelAnterior.SetActive(true);
    }
}
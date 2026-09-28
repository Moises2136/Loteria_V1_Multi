using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Estado de conexión de un amigo (lo llena Photon desde el panel).</summary>
public enum EstadoAmigo
{
    Desconocido,
    Conectado,
    EnPartida,
    Desconectado
}

/// <summary>
/// Controla una tarjeta de la lista de amigos. La misma tarjeta tiene dos modos:
/// - Solicitud: muestra nombre y botones Aceptar/Rechazar, sin estado.
/// - Amigo: muestra nombre y estado de conexión, sin botones.
/// Va en el prefab (Friends_Online / ItemAmigo).
/// </summary>
public class AmigoPrefab : MonoBehaviour
{
    [Header("Textos del usuario")]
    public TMP_Text textoNombre;
    public TMP_Text textoEstado;

    [Header("Botones")]
    public Button botonAceptar;
    public Button botonRechazar;

    [Header("Colores de estado del amigo")]
    public Color colorConectado = new Color(0.30f, 0.65f, 0.25f);
    public Color colorEnPartida = new Color(0.90f, 0.55f, 0.10f);
    public Color colorDesconectado = new Color(0.55f, 0.55f, 0.55f);

    /// <summary>Datos del usuario que representa esta tarjeta.</summary>
    public AmigoInfo Info { get; private set; }

    // =========================
    // MODOS
    // =========================

    /// <summary>Configura la tarjeta como solicitud pendiente.</summary>
    public void ConfigurarComoSolicitud(AmigoInfo info,
                                        Action<AmigoPrefab> alAceptar,
                                        Action<AmigoPrefab> alRechazar)
    {
        Info = info;
        SetTexto(textoNombre, info.nombre);

        if (textoEstado != null) textoEstado.gameObject.SetActive(false);

        ConfigurarBoton(botonAceptar, () => alAceptar?.Invoke(this));
        ConfigurarBoton(botonRechazar, () => alRechazar?.Invoke(this));

        SetInteractuable(true);
    }

    /// <summary>Configura la tarjeta como amigo, con su estado inicial.</summary>
    public void ConfigurarComoAmigo(AmigoInfo info, EstadoAmigo estado = EstadoAmigo.Desconocido)
    {
        Info = info;
        SetTexto(textoNombre, info.nombre);

        if (botonAceptar != null) botonAceptar.gameObject.SetActive(false);
        if (botonRechazar != null) botonRechazar.gameObject.SetActive(false);

        if (textoEstado != null) textoEstado.gameObject.SetActive(true);
        SetEstado(estado);
    }

    // =========================
    // ESTADO
    // =========================

    /// <summary>Actualiza el texto y color del estado de conexión.</summary>
    public void SetEstado(EstadoAmigo estado)
    {
        if (textoEstado == null) return;

        switch (estado)
        {
            case EstadoAmigo.Conectado:
                textoEstado.text = "• Conectado";
                textoEstado.color = colorConectado;
                break;
            case EstadoAmigo.EnPartida:
                textoEstado.text = "• En partida";
                textoEstado.color = colorEnPartida;
                break;
            case EstadoAmigo.Desconectado:
                textoEstado.text = "• Desconectado";
                textoEstado.color = colorDesconectado;
                break;
            default:
                textoEstado.text = "";
                break;
        }
    }

    /// <summary>
    /// Bloquea o desbloquea los botones. Se usa mientras se procesa
    /// Aceptar/Rechazar para que no se presionen dos veces.
    /// </summary>
    public void SetInteractuable(bool valor)
    {
        if (botonAceptar != null) botonAceptar.interactable = valor;
        if (botonRechazar != null) botonRechazar.interactable = valor;
    }

    // =========================
    // AUXILIARES
    // =========================

    void ConfigurarBoton(Button boton, UnityEngine.Events.UnityAction accion)
    {
        if (boton == null) return;
        boton.gameObject.SetActive(true);
        boton.onClick.RemoveAllListeners();
        boton.onClick.AddListener(accion);
    }

    void SetTexto(TMP_Text texto, string valor)
    {
        if (texto != null) texto.text = valor;
    }
}
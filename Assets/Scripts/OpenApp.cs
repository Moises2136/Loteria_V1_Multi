using UnityEngine;
using UnityEngine.Video;
using System.Collections;

/// <summary>
/// Este script controla la pantalla de bienvenida (splash) al abrir la app.
/// Reproduce un video de introducción si hay uno asignado, o simplemente
/// espera unos segundos si no lo hay, y al terminar pasa a la pantalla de
/// inicio de sesión. También avisa a otros scripts (como <c>SessionManager</c>)
/// cuando el splash ya terminó, mediante el evento <see cref="OnSplashTerminado"/>.
/// </summary>
public class OpenApp : MonoBehaviour
{
    /// <summary>
    /// Se dispara en el momento exacto en que el splash termina (ya sea porque
    /// acabó el video, o porque pasó el tiempo de espera). Otros scripts se
    /// suscriben a este evento para saber cuándo es seguro cambiar de pantalla.
    /// </summary>
    public static event System.Action OnSplashTerminado;

    /// <summary>Panel de la pantalla de bienvenida (splash) que se muestra al abrir la app.</summary>
    [Header("Panel inicial")]
    public GameObject PanelAbrirAPP;

    /// <summary>Panel de inicio de sesión, al que se pasa cuando termina el splash.</summary>
    [Header("Panel siguiente")]
    public GameObject PanelInicioSesion;

    /// <summary>Cuántos segundos se espera antes de pasar a la siguiente pantalla, si no hay video asignado.</summary>
    [Header("Duración en segundos si no hay video")]
    public float duracion = 5f;

    /// <summary>Video opcional que se reproduce en la pantalla de bienvenida.</summary>
    [Header("Video opcional de inicio")]
    public VideoPlayer video_inicio;

    /// <summary>
    /// Al iniciar, muestra el panel de splash y oculta el de login. Si hay un
    /// video de inicio asignado, lo reproduce; si no, simplemente espera el
    /// tiempo definido en <see cref="duracion"/>.
    /// </summary>
    private void Start()
    {
        if (PanelAbrirAPP != null)
            PanelAbrirAPP.SetActive(true);

        if (PanelInicioSesion != null)
            PanelInicioSesion.SetActive(false);

        if (video_inicio != null && video_inicio.clip != null)
        {
            StartCoroutine(ReproducirVideoInicio());
        }
        else
        {
            StartCoroutine(MostrarPanelSecuencial());
        }
    }

    /// <summary>
    /// Prepara y reproduce el video de inicio desde el principio, asegurándose
    /// de que empiece limpio (frame 0) incluso si el video ya se había reproducido
    /// antes. Cuando el video termina, se llama automáticamente a <see cref="VideoTermino"/>.
    /// </summary>
    private IEnumerator ReproducirVideoInicio()
    {
        // Limpieza de estado
        video_inicio.Stop();
        video_inicio.frame = 0;
        video_inicio.time = 0;

        // Seguridad: evitar múltiples suscripciones
        video_inicio.loopPointReached -= VideoTermino;
        video_inicio.loopPointReached += VideoTermino;

        // Preparar video antes de reproducir
        video_inicio.Prepare();

        while (!video_inicio.isPrepared)
            yield return null;

        // Volver a asegurar inicio desde el frame 0
        video_inicio.frame = 0;
        video_inicio.time = 0;

        video_inicio.Play();

        Debug.Log("Video de inicio reproduciéndose desde el principio.");
    }

    /// <summary>
    /// Se llama automáticamente cuando el video de inicio termina de reproducirse.
    /// Se desuscribe del evento y pasa a la pantalla de inicio de sesión.
    /// </summary>
    /// <param name="vp">El VideoPlayer que terminó (lo manda Unity automáticamente).</param>
    private void VideoTermino(VideoPlayer vp)
    {
        video_inicio.loopPointReached -= VideoTermino;
        Debug.Log("El video ha terminado.");
        CambiarAPanelInicio();
    }

    /// <summary>
    /// Espera el tiempo definido en <see cref="duracion"/> (usado cuando no hay
    /// video de inicio) y después pasa a la pantalla de inicio de sesión.
    /// </summary>
    private IEnumerator MostrarPanelSecuencial()
    {
        yield return new WaitForSeconds(duracion);
        CambiarAPanelInicio();
    }

    /// <summary>
    /// Detiene el video si seguía reproduciéndose, cierra el panel de splash,
    /// muestra el panel de inicio de sesión, y avisa a quien esté escuchando
    /// (como <c>SessionManager</c>) que el splash ya terminó.
    /// </summary>
    private void CambiarAPanelInicio()
    {
        if (video_inicio != null && video_inicio.isPlaying)
            video_inicio.Stop();

        if (PanelAbrirAPP != null)
            PanelAbrirAPP.SetActive(false);

        if (PanelInicioSesion != null)
            PanelInicioSesion.SetActive(true);

        OnSplashTerminado?.Invoke();
    }
}
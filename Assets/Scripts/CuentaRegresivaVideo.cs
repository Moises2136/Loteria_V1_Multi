using UnityEngine;
using UnityEngine.Video;
using System.Collections;

/// <summary>
/// Este script controla la cuenta regresiva (con video) que se muestra
/// justo antes de que empiece la partida. Reproduce un video de cuenta
/// regresiva y, cuando termina, pasa automáticamente al panel de juego
/// correcto según el modo (Solo o Multijugador). Es el mismo patrón de
/// video que <c>OpenApp</c>, aplicado aquí a la cuenta regresiva.
/// </summary>
public class CuentaRegresivaVideo : MonoBehaviour
{
    /// <summary>Este mismo panel de cuenta regresiva.</summary>
    [Header("Panel actual")]
    public GameObject panelCuentaRegresiva;

    /// <summary>Panel de juego en modo Solo, al que se pasa si <see cref="esMultijugador"/> es falso.</summary>
    [Header("Siguientes paneles")]
    public GameObject panelJuegaLoteriaSolo;

    /// <summary>Panel de juego en modo Multijugador, al que se pasa si <see cref="esMultijugador"/> es verdadero.</summary>
    public GameObject panelJuegaLoteriaMultiplayer;

    /// <summary>Video de cuenta regresiva que se reproduce en este panel.</summary>
    [Header("Video countdown")]
    public VideoPlayer videoCuentaRegresiva;

    /// <summary>Indica a qué modo de juego se debe pasar al terminar la cuenta regresiva.</summary>
    [Header("Modo")]
    public bool esMultijugador = false;

    [Header("FX Sound")]

    /// <summary>FX contador regresivo.</summary>
    public AudioSource audioRegresivo;
    public AudioClip sonidoRegresivo;



    /// <summary>
    /// Cada vez que se activa este panel, reproduce el video de cuenta
    /// regresiva si hay uno asignado; si no, pasa de inmediato al
    /// siguiente panel (con un aviso en consola de que faltó el video).
    /// </summary>
    private void OnEnable()
    {
        if (videoCuentaRegresiva != null && videoCuentaRegresiva.clip != null)
        {
            StartCoroutine(ReproducirVideo());
        }
        else
        {
            Debug.LogWarning("No hay video de cuenta regresiva asignado.");
            AbrirSiguientePanel();
        }
    }

    /// <summary>
    /// Prepara y reproduce el video de cuenta regresiva desde el principio,
    /// asegurándose de que empiece limpio (frame 0) incluso si ya se había
    /// reproducido antes. Cuando el video termina, se llama automáticamente
    /// a <see cref="VideoTermino"/>.
    /// </summary>
    private IEnumerator ReproducirVideo()
    {
        videoCuentaRegresiva.Stop();
        videoCuentaRegresiva.frame = 0;
        videoCuentaRegresiva.time = 0;

        videoCuentaRegresiva.loopPointReached -= VideoTermino;
        videoCuentaRegresiva.loopPointReached += VideoTermino;

        videoCuentaRegresiva.Prepare();

        while (!videoCuentaRegresiva.isPrepared)
            yield return null;

        videoCuentaRegresiva.frame = 0;
        videoCuentaRegresiva.time = 0;

        if (audioRegresivo != null && sonidoRegresivo != null)
            audioRegresivo.PlayOneShot(sonidoRegresivo);

        
        videoCuentaRegresiva.Play();

        Debug.Log("Cuenta regresiva iniciada.");
    }

    /// <summary>
    /// Se llama automáticamente cuando el video de cuenta regresiva termina
    /// de reproducirse. Se desuscribe del evento y abre el siguiente panel.
    /// </summary>
    /// <param name="vp">El VideoPlayer que terminó (lo manda Unity automáticamente).</param>
    private void VideoTermino(VideoPlayer vp)
    {
        videoCuentaRegresiva.loopPointReached -= VideoTermino;
        AbrirSiguientePanel();
    }

    /// <summary>
    /// Cierra el panel de cuenta regresiva y abre el panel de juego correcto,
    /// según si el modo actual es Multijugador o Solo.
    /// </summary>
    private void AbrirSiguientePanel()
    {
        if (panelCuentaRegresiva != null)
            panelCuentaRegresiva.SetActive(false);

        if (esMultijugador)
        {
            if (panelJuegaLoteriaMultiplayer != null)
                panelJuegaLoteriaMultiplayer.SetActive(true);
        }
        else
        {
            if (panelJuegaLoteriaSolo != null)
                panelJuegaLoteriaSolo.SetActive(true);
        }
    }

    /// <summary>
    /// Define si la cuenta regresiva debe llevar al modo Multijugador o al
    /// modo Solo al terminar. Se llama desde fuera antes de mostrar este
    /// panel — por ejemplo, ya vimos que <c>SalaFlowManager.ComenzarFlujo()</c>
    /// llama a esto con <c>false</c> para configurar el modo Solo.
    /// </summary>
    /// <param name="multiplayer">Verdadero para modo Multijugador, falso para modo Solo.</param>
    public void ConfigurarModo(bool multiplayer)
    {
        esMultijugador = multiplayer;
    }
}
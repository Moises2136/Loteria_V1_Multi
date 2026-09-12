using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Este script controla el modo oscuro/claro del juego: al presionar el
/// botón de Modo Oscuro o el de Modo Claro, cambia de golpe el color de
/// todos los fondos, textos e imágenes registrados en las listas del Inspector.
/// </summary>
public class DarkModeManager : MonoBehaviour
{
    /// <summary>Indica si el modo oscuro está activo actualmente.</summary>
    [Header("Modo actual")]
    [SerializeField] private bool darkMode = false;

    /// <summary>Botón que activa el modo oscuro.</summary>
    [Header("Botones")]
    [SerializeField] private Button darkModeButton;

    /// <summary>Botón que activa el modo claro.</summary>
    [SerializeField] private Button lightModeButton;

    /// <summary>Lista de fondos/paneles cuyo color cambia según el modo.</summary>
    [Header("Fondos / Paneles")]
    [SerializeField] private List<Image> backgrounds = new List<Image>();

    /// <summary>Lista de textos cuyo color cambia según el modo.</summary>
    [Header("Textos")]
    [SerializeField] private List<TextMeshProUGUI> texts = new List<TextMeshProUGUI>();

    /// <summary>Lista de imágenes (íconos, sprites) cuyo color cambia según el modo.</summary>
    [Header("Imágenes que deben cambiar")]
    [SerializeField] private List<Image> images = new List<Image>();

    /// <summary>Color de fondo cuando el modo claro está activo.</summary>
    [Header("Colores")]
    [SerializeField] private Color lightBackground = Color.white;

    /// <summary>Color de fondo cuando el modo oscuro está activo.</summary>
    [SerializeField] private Color darkBackground = new Color(0.08f, 0.08f, 0.08f);

    /// <summary>Color de texto cuando el modo claro está activo.</summary>
    [SerializeField] private Color lightText = Color.black;

    /// <summary>Color de texto cuando el modo oscuro está activo.</summary>
    [SerializeField] private Color darkText = Color.white;

    /// <summary>
    /// Al iniciar, conecta cada botón con su modo correspondiente (oscuro
    /// o claro), y aplica de inmediato el modo actual a todos los
    /// elementos registrados.
    /// </summary>
    private void Start()
    {
        if (darkModeButton != null)
        {
            darkModeButton.onClick.AddListener(ActivarModoOscuro);
        }
        else
        {
            Debug.LogWarning("DarkModeButton is not assigned in the inspector.");
        }

        if (lightModeButton != null)
        {
            lightModeButton.onClick.AddListener(ActivarModoClaro);
        }
        else
        {
            Debug.LogWarning("LightModeButton is not assigned in the inspector.");
        }

        ApplyDarkMode();
    }

    /// <summary>
    /// Activa el modo oscuro (llamado por el botón de Modo Oscuro).
    /// </summary>
    public void ActivarModoOscuro()
    {
        SetDarkMode(true);
    }

    /// <summary>
    /// Activa el modo claro / blanco (llamado por el botón de Modo Claro).
    /// </summary>
    public void ActivarModoClaro()
    {
        SetDarkMode(false);
    }

    /// <summary>
    /// Cambia entre modo oscuro y modo claro, y aplica el cambio de inmediato.
    /// </summary>
    public void ToggleDarkMode()
    {
        SetDarkMode(!darkMode);
    }

    /// <summary>
    /// Establece directamente el modo oscuro como activado o desactivado,
    /// y aplica el cambio de inmediato a todos los elementos registrados.
    /// </summary>
    /// <param name="enabled">Verdadero para activar el modo oscuro, falso para el modo claro.</param>
    public void SetDarkMode(bool enabled)
    {
        darkMode = enabled;
        ApplyDarkMode();
    }

    /// <summary>
    /// Recorre las tres listas (fondos, textos, imágenes) y les aplica el
    /// color correspondiente según si el modo oscuro está activo o no.
    /// </summary>
    private void ApplyDarkMode()
    {
        // Fondos de los paneles y extras que manejen colores
        foreach (Image background in backgrounds)
        {
            if (background != null)
            {
                background.color = darkMode
                    ? darkBackground
                    : lightBackground;
            }
        }

        // Textos del juego para modificar su color
        foreach (TextMeshProUGUI text in texts)
        {
            if (text != null)
            {
                text.color = darkMode
                    ? darkText
                    : lightText;
            }
        }

        // Imágenes agregadas como los sprites
        foreach (Image image in images)
        {
            if (image != null)
            {
                image.color = darkMode
                    ? darkText
                    : lightText;
            }
        }
    }
}
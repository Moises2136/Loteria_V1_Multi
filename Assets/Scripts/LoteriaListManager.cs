using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Este script crea y muestra una lista de tarjetas de lotería en pantalla,
/// instanciando el prefab de tarjeta y llenándolo con datos.
/// </summary>
/// <remarks>
/// Actualmente solo genera loterías de <b>ejemplo/demo</b> (con datos
/// inventados y cartas vacías, sin sprite ni nombre real) usando
/// <see cref="DatosLoteria"/>/<see cref="DatosCarta"/> — no está conectado
/// a Firestore ni a las loterías reales que crea el jugador con
/// <c>CreateLotery</c>/<c>LoteriaDropdownLoader</c> (esas usan
/// <c>LoteriaData</c>/<c>CartaData</c> en su lugar). Es un buen candidato
/// a reemplazar por datos reales cuando conectes esta lista con Firestore.
/// </remarks>
public class LoteriaListManager : MonoBehaviour
{
    // =========================
    // REFERENCIAS
    // =========================

    /// <summary>Contenedor donde se van a colocar las tarjetas de lotería.</summary>
    public Transform contenedor;

    /// <summary>
    /// Prefab de la tarjeta de lotería. Debe tener el script
    /// <c>CardLoteriaUI</c> para poder configurarse con los datos.
    /// </summary>
    public GameObject prefabLoteria;

    // =========================
    // INICIALIZACIÓN
    // =========================

    /// <summary>
    /// Al iniciar, crea tres tarjetas de lotería de ejemplo, solo para
    /// tener contenido de prueba visible en pantalla.
    /// </summary>
    void Start()
    {
        // Datos de ejemplo (demo)
        CrearLoteriaDemo("Familia de plantas medicinales", false, 100);
        CrearLoteriaDemo("Plantas medicinales de mercados", true, 50);
        CrearLoteriaDemo("Lotería de ejemplo", false, 30);
    }

    // =========================
    // CREACIÓN DE LOTERÍA
    // =========================

    /// <summary>
    /// Crea una tarjeta de lotería con datos de ejemplo (sin conexión a
    /// Firestore) y la agrega a la lista en pantalla.
    /// </summary>
    /// <param name="nombre">Nombre de la lotería de ejemplo.</param>
    /// <param name="esPublica">Si la lotería de ejemplo se muestra como pública o privada.</param>
    /// <param name="numeroCartas">Cuántas cartas (vacías, sin datos reales) tendrá la lotería de ejemplo.</param>
    public void CrearLoteriaDemo(string nombre, bool esPublica, int numeroCartas)
    {
        // Validaciones básicas
        if (contenedor == null || prefabLoteria == null)
        {
            Debug.LogWarning("Faltan referencias en LoteriaListManager.");
            return;
        }

        // Instancia el prefab dentro del contenedor
        GameObject nueva = Instantiate(prefabLoteria, contenedor);

        // Obtiene el script de la tarjeta UI
        CardLoteriaUI card = nueva.GetComponent<CardLoteriaUI>();

        if (card != null)
        {
            // Crear objeto de datos
            DatosLoteria datos = new DatosLoteria();
            datos.nombreLoteria = nombre;
            datos.esPublica = esPublica;

            // Generar cartas simuladas
            for (int i = 0; i < numeroCartas; i++)
            {
                datos.cartas.Add(new DatosCarta());
            }

            // Configurar la tarjeta UI con los datos
            card.Configurar(datos);
        }
        else
        {
            Debug.LogWarning("El prefab no tiene CardLoteriaUI.");
        }
    }
}
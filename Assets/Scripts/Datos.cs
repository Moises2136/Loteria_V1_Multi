using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Representa una sola carta dentro de una lotería, con su nombre y su imagen.
/// </summary>
/// <remarks>
/// A diferencia de <see cref="CartaData"/> (que usa una URL de imagen y se
/// guarda tal cual en Firestore), esta versión usa un <c>Sprite</c> directo
/// de Unity — pensada para mostrarse en pantalla, no para guardarse en la
/// base de datos. Es justamente el tipo de dato que espera <c>CardLoteriaUI</c>.
/// </remarks>
[System.Serializable]
public class DatosCarta
{
    /// <summary>Nombre de la carta.</summary>
    public string nombreCarta;

    /// <summary>Imagen de la carta, ya lista para mostrarse en pantalla.</summary>
    public Sprite imagenCarta;
}

/// <summary>
/// Representa una lotería completa, lista para mostrarse en pantalla:
/// su nombre, si es pública o privada, su imagen de portada, y la lista
/// de todas sus cartas.
/// </summary>
/// <remarks>
/// Es el tipo de dato que recibe <c>CardLoteriaUI.Configurar(...)</c> para
/// llenar una tarjeta de lotería en una lista — el equivalente "visual" de
/// <see cref="LoteriaData"/>, que en cambio está pensado para guardarse
/// en Firestore.
/// </remarks>
[System.Serializable]
public class DatosLoteria
{
    /// <summary>Nombre de la lotería.</summary>
    public string nombreLoteria;

    /// <summary>Indica si la lotería es pública (visible para otros) o privada.</summary>
    public bool esPublica;

    /// <summary>Imagen de portada de la lotería.</summary>
    public Sprite imagenPortada;

    /// <summary>Lista de cartas que pertenecen a esta lotería.</summary>
    public List<DatosCarta> cartas = new List<DatosCarta>();
}
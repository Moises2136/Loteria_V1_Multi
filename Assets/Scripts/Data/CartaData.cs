/// <summary>
/// Representa la información de una sola carta de la lotería:
/// su nombre, descripción, y en qué ruta está guardada su imagen.
/// </summary>
[System.Serializable]
public class CartaData
{
    /// <summary>ID único que identifica a esta carta.</summary>
    public int id;

    /// <summary>Nombre de la carta.</summary>
    public string nombre;

    /// <summary>Descripción o texto explicativo de la carta.</summary>
    public string descripcion;

    /// <summary>Ruta o URL donde está guardada la imagen de la carta.</summary>
    public string rutaImagen;
}
using UnityEngine;

/// <summary>
/// Guarda temporalmente los datos de la carta que se está armando en
/// <c>PanelAgregarCartaUI</c> (la imagen recortada, el nombre y la
/// descripción) mientras se recorta la imagen, se sube a Cloudinary,
/// y se guarda en Firestore.
/// </summary>
/// <remarks>
/// Es una clase <c>static</c>, igual que <see cref="LoteriaTemp"/>: sus
/// valores existen una sola vez en todo el juego, y cualquier script
/// puede leerlos o modificarlos directamente sin necesitar una instancia.
/// Por eso es importante llamar <see cref="Limpiar"/> cuando se termina
/// de usar una carta (ya sea porque se guardó o se canceló), para no
/// dejar datos de una carta vieja pegados la próxima vez que se arme una nueva.
/// </remarks>
public static class CartTemp
{
    /// <summary>Imagen ya recortada de la carta que se está armando.</summary>
    public static Texture2D imagen;

    /// <summary>Nombre de la carta que se está armando.</summary>
    public static string nombre;

    /// <summary>Descripción de la carta que se está armando.</summary>
    public static string descripcion;

    /// <summary>URL de la imagen ya subida a Cloudinary (vacía hasta que se sube).</summary>
    public static string rutaImagen;

    /// <summary>
    /// Borra todos los datos temporales, dejando la clase lista para
    /// empezar a armar una carta nueva desde cero.
    /// </summary>
    public static void Limpiar()
    {
        imagen = null;
        nombre = "";
        descripcion = "";
        rutaImagen = "";
    }
}
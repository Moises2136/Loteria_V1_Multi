/// <summary>
/// Guarda temporalmente los datos de la lotería que se está creando o
/// editando, para que varios paneles (como <c>PanelAgregarCartaUI</c> o
/// <c>CreateLotery</c>) puedan compartir esta información sin necesidad
/// de pasársela directamente entre sí.
/// </summary>
/// <remarks>
/// Es una clase <c>static</c>, así que sus valores existen una sola vez
/// en todo el juego y cualquier script puede leerlos o modificarlos
/// directamente (por ejemplo <c>LoteriaTemp.nombre</c>), sin necesitar
/// una instancia ni una referencia. Por eso es importante llamar
/// <see cref="Limpiar"/> cuando se termina de usar, para no dejar datos
/// de una lotería vieja pegados la próxima vez que se cree una nueva.
/// </remarks>
public static class LoteriaTemp
{
    /// <summary>ID de la lotería que se está creando o editando (vacío si aún no se ha guardado en Firestore).</summary>
    public static string loteriaId = "";

    /// <summary>Nombre de la lotería que se está creando o editando.</summary>
    public static string nombre = "";

    /// <summary>Cuántas cartas se han agregado hasta ahora a esta lotería.</summary>
    public static int totalCartas = 0;

    /// <summary>
    /// Borra todos los datos temporales, dejando la clase lista para
    /// empezar a armar una lotería nueva desde cero.
    /// </summary>
    public static void Limpiar()
    {
        loteriaId = "";
        nombre = "";
        totalCartas = 0;
    }
}
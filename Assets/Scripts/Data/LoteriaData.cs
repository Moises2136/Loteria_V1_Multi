using System.Collections.Generic;

/// <summary>
/// Representa una lotería completa: su información básica
/// y la lista de todas las cartas que la componen.
/// </summary>
[System.Serializable]
public class LoteriaData
{
    /// <summary>ID único que identifica a esta lotería.</summary>
    public int id;

    /// <summary>Nombre de la lotería.</summary>
    public string nombre;

    /// <summary>Lista de todas las cartas que pertenecen a esta lotería.</summary>
    public List<CartaData> cartas = new List<CartaData>();
}
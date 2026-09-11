using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lleva el registro de las loterías jugadas recientemente en este dispositivo,
/// guardado en PlayerPrefs. Se usa para mostrar accesos rápidos en el menú principal.
/// </summary>
public static class RecentLoteriasManager
{
    private const string key_recientes = "loterias_recientes";
    private const int max_recientes = 3;

    /// <summary>
    /// Marca una lotería como "jugada recientemente": la mueve al principio
    /// de la lista (sin duplicarla) y recorta la lista a un máximo de 3.
    /// </summary>
    /// <param name="loteriaDocId">ID de la lotería que se acaba de seleccionar para jugar.</param>
    public static void RegistrarJugada(string loteriaDocId)
    {
        if (string.IsNullOrWhiteSpace(loteriaDocId)) return;

        List<string> lista = ObtenerRecientes();

        lista.Remove(loteriaDocId);
        lista.Insert(0, loteriaDocId);

        while (lista.Count > max_recientes)
            lista.RemoveAt(lista.Count - 1);

        PlayerPrefs.SetString(key_recientes, string.Join(",", lista));
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Devuelve los IDs de las loterías jugadas recientemente, de la más
    /// reciente a la más antigua un maximo de 3
    /// </summary>
    public static List<string> ObtenerRecientes()
    {
        string guardado = PlayerPrefs.GetString(key_recientes, "");
        List<string> lista = new List<string>();

        if (!string.IsNullOrWhiteSpace(guardado))
            foreach (string id in guardado.Split(','))
                if (!string.IsNullOrWhiteSpace(id))
                    lista.Add(id);

        return lista;
    }
}
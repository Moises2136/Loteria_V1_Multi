using System;
using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;

/// <summary>
/// Datos de estadísticas de un jugador.
/// </summary>
public struct EstadisticasJugador
{
    public string uid;
    public string nombre;
    public int partidasJugadas;
    public int ganadas;
    public int perdidas;

    /// <summary>Porcentaje de victorias (0 a 100).</summary>
    public float PorcentajeVictorias =>
        partidasJugadas == 0 ? 0f : (float)ganadas / partidasJugadas * 100f;
}

/// <summary>
/// Guarda y consulta estadísticas de partidas en Firestore.
/// Todas las respuestas llegan en el hilo principal de Unity (ContinueWithOnMainThread),
/// así que es seguro tocar la UI dentro de los callbacks.
/// </summary>
public static class EstadisticasGameService
{
    // Ajusta estos nombres a como tengas tu colección de usuarios en Firestore
    const string COLECCION = "usuarios";
    const string CAMPO_NOMBRE = "username";
    const string CAMPO_JUGADAS = "partidasJugadas";
    const string CAMPO_GANADAS = "ganadas";
    const string CAMPO_PERDIDAS = "perdidas";

    static FirebaseFirestore Db => FirebaseFirestore.DefaultInstance;

    /// <summary>UID del usuario con sesión iniciada, o null si no hay sesión.</summary>
    public static string UidActual => FirebaseAuth.DefaultInstance.CurrentUser?.UserId;

    // =========================
    // ESCRITURA
    // =========================

    /// <summary>
    /// Registra el resultado de una partida para el jugador local.
    /// Usa incrementos atómicos, así que no importa si dos partidas terminan casi al mismo tiempo.
    /// Si el documento no tiene aún los campos, los crea.
    /// </summary>
    public static void RegistrarResultado(bool gano, Action<bool> alTerminar = null)
    {
        string uid = UidActual;
        if (string.IsNullOrEmpty(uid))
        {
            Debug.LogWarning("[Estadisticas] No hay sesión; no se registra el resultado.");
            alTerminar?.Invoke(false);
            return;
        }

        var cambios = new Dictionary<string, object>
        {
            { CAMPO_JUGADAS, FieldValue.Increment(1) },
            { gano ? CAMPO_GANADAS : CAMPO_PERDIDAS, FieldValue.Increment(1) }
        };

        Db.Collection(COLECCION).Document(uid)
          .SetAsync(cambios, SetOptions.MergeAll)
          .ContinueWithOnMainThread(t =>
          {
              if (t.IsFaulted || t.IsCanceled)
                  Debug.LogError("[Estadisticas] Error al registrar: " + t.Exception);
              alTerminar?.Invoke(t.IsCompletedSuccessfully);
          });
    }

    // =========================
    // LECTURA
    // =========================

    /// <summary>Obtiene las estadísticas del jugador local.</summary>
    public static void ObtenerMisEstadisticas(Action<EstadisticasJugador?> alTerminar)
    {
        string uid = UidActual;
        if (string.IsNullOrEmpty(uid))
        {
            alTerminar?.Invoke(null);
            return;
        }

        Db.Collection(COLECCION).Document(uid)
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(t =>
          {
              if (t.IsFaulted || t.IsCanceled || !t.Result.Exists)
              {
                  alTerminar?.Invoke(null);
                  return;
              }
              alTerminar?.Invoke(Mapear(t.Result));
          });
    }

    /// <summary>
    /// Obtiene los mejores jugadores ordenados por partidas ganadas.
    /// Los usuarios que nunca han jugado no aparecen (no tienen el campo).
    /// </summary>
    public static void ObtenerTop(int limite, Action<List<EstadisticasJugador>> alTerminar)
    {
        Db.Collection(COLECCION)
          .OrderByDescending(CAMPO_GANADAS)
          .Limit(limite)
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(t =>
          {
              var lista = new List<EstadisticasJugador>();

              if (t.IsFaulted || t.IsCanceled)
              {
                  Debug.LogError("[Estadisticas] Error al leer el ranking: " + t.Exception);
                  alTerminar?.Invoke(lista);
                  return;
              }

              foreach (DocumentSnapshot doc in t.Result.Documents)
                  lista.Add(Mapear(doc));

              alTerminar?.Invoke(lista);
          });
    }

    /// <summary>
    /// Calcula la posición del jugador contando cuántos tienen más victorias que él.
    /// Si hay empate, comparten posición. Devuelve -1 si hubo error.
    /// </summary>
    public static void ObtenerMiPosicion(int misGanadas, Action<int> alTerminar)
    {
        Db.Collection(COLECCION)
          .WhereGreaterThan(CAMPO_GANADAS, misGanadas)
          .Count
          .GetSnapshotAsync(AggregateSource.Server)
          .ContinueWithOnMainThread(t =>
          {
              if (t.IsFaulted || t.IsCanceled)
              {
                  Debug.LogError("[Estadisticas] Error al calcular posición: " + t.Exception);
                  alTerminar?.Invoke(-1);
                  return;
              }
              alTerminar?.Invoke((int)t.Result.Count + 1);
          });
    }

    // =========================
    // AUXILIARES
    // =========================

    static EstadisticasJugador Mapear(DocumentSnapshot doc)
    {
        return new EstadisticasJugador
        {
            uid = doc.Id,
            nombre = doc.TryGetValue(CAMPO_NOMBRE, out string n) ? n : "Jugador",
            partidasJugadas = LeerInt(doc, CAMPO_JUGADAS),
            ganadas = LeerInt(doc, CAMPO_GANADAS),
            perdidas = LeerInt(doc, CAMPO_PERDIDAS)
        };
    }

    static int LeerInt(DocumentSnapshot doc, string campo)
    {
        return doc.TryGetValue(campo, out long valor) ? (int)valor : 0;
    }
}
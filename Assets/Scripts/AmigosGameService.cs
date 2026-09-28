using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;

/// <summary>Datos básicos de un amigo o de quien envió una solicitud.</summary>
public struct AmigoInfo
{
    public string uid;
    public string nombre;
}

/// <summary>Resultado de intentar enviar una solicitud de amistad.</summary>
public enum ResultadoSolicitud
{
    Enviada,
    AceptadaAutomaticamente, // la otra persona ya te había enviado solicitud
    NoEncontrado,
    EsTuCodigo,
    YaSonAmigos,
    SinSesion,
    Error
}

/// <summary>
/// Maneja todo lo de amigos en Firestore: código de amigo, solicitudes y lista de amigos.
/// Estructura:
///   usuarios/{uid}                       → campo codigoAmigo
///   usuarios/{uid}/solicitudes/{deUid}   → solicitudes que TE enviaron
///   usuarios/{uid}/amigos/{amigoUid}     → tus amigos
/// </summary>
/// <remarks>
/// Los métodos son async: desde la UI se llaman con await dentro de un método "async void".
/// En Unity, después del await se sigue en el hilo principal, así que se puede tocar la UI.
/// </remarks>
public static class AmigosService
{
    // Ajusta estos nombres a tu colección de usuarios
    const string COLECCION = "usuarios";
    const string CAMPO_NOMBRE = "username";
    const string CAMPO_CODIGO = "codigoAmigo";

    const string SUB_AMIGOS = "amigos";
    const string SUB_SOLICITUDES = "solicitudes";

    const string CARACTERES = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    const int LARGO_CODIGO = 6;

    static readonly System.Random random = new System.Random();

    static FirebaseFirestore Db => FirebaseFirestore.DefaultInstance;
    static string UidActual => FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
    static DocumentReference Usuario(string uid) => Db.Collection(COLECCION).Document(uid);

    // =========================
    // CÓDIGO DE AMIGO
    // =========================

    /// <summary>
    /// Devuelve el código de amigo del usuario. Si todavía no tiene, genera
    /// uno único y lo guarda. Devuelve null si no hay sesión o hubo error.
    /// </summary>
    public static async Task<string> ObtenerOCrearMiCodigo()
    {
        string uid = UidActual;
        if (string.IsNullOrEmpty(uid)) return null;

        try
        {
            DocumentSnapshot snap = await Usuario(uid).GetSnapshotAsync();
            if (snap.Exists && snap.TryGetValue(CAMPO_CODIGO, out string existente)
                && !string.IsNullOrEmpty(existente))
                return existente;

            // Intentar hasta encontrar un código que nadie tenga en la base de datos.
            for (int intento = 0; intento < 5; intento++)
            {
                string nuevo = GenerarCodigo();
                if (await BuscarUidPorCodigo(nuevo) != null) continue;

                await Usuario(uid).SetAsync(
                    new Dictionary<string, object> { { CAMPO_CODIGO, nuevo } },
                    SetOptions.MergeAll);
                return nuevo;
            }

            Debug.LogError("[Amigos] No se pudo generar un código único.");
        }
        catch (Exception e)
        {
            Debug.LogError("[Amigos] Error al obtener código: " + e);
        }
        return null;
    }

    // =========================
    // SOLICITUDES
    // =========================

    /// <summary>
    /// Envía una solicitud de amistad al usuario con ese código.
    /// Si esa persona ya te había mandado solicitud, se aceptan mutuamente.
    /// </summary>
    public static async Task<ResultadoSolicitud> EnviarSolicitud(string codigo)
    {
        string miUid = UidActual;
        if (string.IsNullOrEmpty(miUid)) return ResultadoSolicitud.SinSesion;

        codigo = codigo?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(codigo)) return ResultadoSolicitud.NoEncontrado;

        try
        {
            string otroUid = await BuscarUidPorCodigo(codigo);
            if (otroUid == null) return ResultadoSolicitud.NoEncontrado;
            if (otroUid == miUid) return ResultadoSolicitud.EsTuCodigo;

            DocumentSnapshot yaAmigo = await Usuario(miUid).Collection(SUB_AMIGOS)
                                                           .Document(otroUid).GetSnapshotAsync();
            if (yaAmigo.Exists) return ResultadoSolicitud.YaSonAmigos;

            // ¿La otra persona ya me había enviado solicitud? Entonces aceptamos directo.
            DocumentSnapshot inversa = await Usuario(miUid).Collection(SUB_SOLICITUDES)
                                                           .Document(otroUid).GetSnapshotAsync();
            if (inversa.Exists)
            {
                bool ok = await AceptarSolicitud(otroUid, LeerNombre(inversa));
                return ok ? ResultadoSolicitud.AceptadaAutomaticamente : ResultadoSolicitud.Error;
            }

            string miNombre = await ObtenerMiNombre(miUid);

            await Usuario(otroUid).Collection(SUB_SOLICITUDES).Document(miUid).SetAsync(
                new Dictionary<string, object>
                {
                    { "nombre", miNombre },
                    { "fecha", FieldValue.ServerTimestamp }
                });

            return ResultadoSolicitud.Enviada;
        }
        catch (Exception e)
        {
            Debug.LogError("[Amigos] Error al enviar solicitud: " + e);
            return ResultadoSolicitud.Error;
        }
    }

    /// <summary>
    /// Acepta una solicitud: los agrega como amigos en ambos lados y borra la solicitud.
    /// Todo se hace en un solo lote, así que o se guarda todo o nada.
    /// </summary>
    public static async Task<bool> AceptarSolicitud(string deUid, string deNombre)
    {
        string miUid = UidActual;
        if (string.IsNullOrEmpty(miUid)) return false;

        try
        {
            string miNombre = await ObtenerMiNombre(miUid);

            WriteBatch batch = Db.StartBatch();

            batch.Set(Usuario(miUid).Collection(SUB_AMIGOS).Document(deUid),
                new Dictionary<string, object>
                {
                    { "nombre", deNombre },
                    { "desde", FieldValue.ServerTimestamp }
                });

            batch.Set(Usuario(deUid).Collection(SUB_AMIGOS).Document(miUid),
                new Dictionary<string, object>
                {
                    { "nombre", miNombre },
                    { "desde", FieldValue.ServerTimestamp }
                });

            batch.Delete(Usuario(miUid).Collection(SUB_SOLICITUDES).Document(deUid));

            await batch.CommitAsync();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("[Amigos] Error al aceptar solicitud: " + e);
            return false;
        }
    }

    /// <summary>Rechaza (borra) una solicitud recibida.</summary>
    public static async Task<bool> RechazarSolicitud(string deUid)
    {
        string miUid = UidActual;
        if (string.IsNullOrEmpty(miUid)) return false;

        try
        {
            await Usuario(miUid).Collection(SUB_SOLICITUDES).Document(deUid).DeleteAsync();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("[Amigos] Error al rechazar solicitud: " + e);
            return false;
        }
    }

    /// <summary>Elimina una amistad en ambos lados.</summary>
    public static async Task<bool> EliminarAmigo(string amigoUid)
    {
        string miUid = UidActual;
        if (string.IsNullOrEmpty(miUid)) return false;

        try
        {
            WriteBatch batch = Db.StartBatch();
            batch.Delete(Usuario(miUid).Collection(SUB_AMIGOS).Document(amigoUid));
            batch.Delete(Usuario(amigoUid).Collection(SUB_AMIGOS).Document(miUid));
            await batch.CommitAsync();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("[Amigos] Error al eliminar amigo: " + e);
            return false;
        }
    }

    // =========================
    // LISTAS
    // =========================

    /// <summary>Solicitudes que le han enviado al usuario.</summary>
    public static Task<List<AmigoInfo>> ObtenerSolicitudes() => LeerSubcoleccion(SUB_SOLICITUDES);

    /// <summary>Amigos del usuario, ordenados por nombre.</summary>
    public static async Task<List<AmigoInfo>> ObtenerAmigos()
    {
        List<AmigoInfo> amigos = await LeerSubcoleccion(SUB_AMIGOS);
        amigos.Sort((a, b) => string.Compare(a.nombre, b.nombre, StringComparison.OrdinalIgnoreCase));
        return amigos;
    }

    static async Task<List<AmigoInfo>> LeerSubcoleccion(string sub)
    {
        var lista = new List<AmigoInfo>();
        string miUid = UidActual;
        if (string.IsNullOrEmpty(miUid)) return lista;

        try
        {
            QuerySnapshot snap = await Usuario(miUid).Collection(sub).GetSnapshotAsync();
            foreach (DocumentSnapshot doc in snap.Documents)
                lista.Add(new AmigoInfo { uid = doc.Id, nombre = LeerNombre(doc) });
        }
        catch (Exception e)
        {
            Debug.LogError($"[Amigos] Error al leer {sub}: " + e);
        }
        return lista;
    }

    // =========================
    // AUXILIARES
    // =========================

    static async Task<string> BuscarUidPorCodigo(string codigo)
    {
        QuerySnapshot snap = await Db.Collection(COLECCION)
                                     .WhereEqualTo(CAMPO_CODIGO, codigo)
                                     .Limit(1)
                                     .GetSnapshotAsync();

        foreach (DocumentSnapshot doc in snap.Documents)
            return doc.Id;

        return null;
    }

    static async Task<string> ObtenerMiNombre(string miUid)
    {
        DocumentSnapshot snap = await Usuario(miUid).GetSnapshotAsync();
        if (snap.Exists && snap.TryGetValue(CAMPO_NOMBRE, out string nombre)
            && !string.IsNullOrWhiteSpace(nombre))
            return nombre;

        string local = PlayerPrefs.GetString("usuario", "");
        return string.IsNullOrWhiteSpace(local) ? "Jugador" : local;
    }

    static string LeerNombre(DocumentSnapshot doc)
    {
        return doc.TryGetValue("nombre", out string n) && !string.IsNullOrWhiteSpace(n) ? n : "Jugador";
    }

    static string GenerarCodigo()
    {
        char[] c = new char[LARGO_CODIGO];
        for (int i = 0; i < LARGO_CODIGO; i++)
            c[i] = CARACTERES[random.Next(CARACTERES.Length)];
        return new string(c);
    }
}
using UnityEngine;
using TMPro;

/// <summary>
/// Limpia todos los campos de texto del panel cuando este se desactiva
/// Esto es útil para que al abrir un panel de login o registro, los campos estén vacíos y no con lo que el usuario escribió la última vez o algo raro de la DB
/// </summary>
public class LimpiezaCampos : MonoBehaviour
{
    private void OnDisable()
    {
        foreach (TMP_InputField campo in GetComponentsInChildren<TMP_InputField>(true))
        {
            campo.SetTextWithoutNotify("");
        }

        TMP_InputField[] campos = GetComponentsInChildren<TMP_InputField>(true);
    Debug.Log($"[{name}] Limpiando {campos.Length} campos");

    foreach (TMP_InputField campo in campos)
    {
        campo.text = "";
    }
    }
}
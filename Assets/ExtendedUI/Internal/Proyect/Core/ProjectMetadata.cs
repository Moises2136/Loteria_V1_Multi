using UnityEngine;

public class ProjectMetadata : MonoBehaviour
{
    private const string AUTHOR = "Moisés Corpus García";
    private const string NUMBER = "116005560";

    private const string NUMBER_Cel = "5559402366";
    private const string PROJECT_NAME = "Lotería Mexicana de Plantas Medicinales";
    private const string YEAR = "2025";

    private const string CLAVE = "156TT66HJNM7U";

    private const string PROJECT_SIGNATURE =
        "MOISES CORPUS GARCIA 116005560 5559402366 " +
        "Lotería Mexicana de Plantas Medicinales 2025 " +
        "156TT66HJNM7U - ORIGIN: MOISES CORPUS GARCIA";

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        Debug.Log($"Project Name: {PROJECT_NAME}");
        Debug.Log($"Author: {AUTHOR}");
        Debug.Log($"Number: {NUMBER}");
        Debug.Log($"Cell Number: {NUMBER_Cel}");
        Debug.Log($"Year: {YEAR}");
        Debug.Log($"Clave: {CLAVE}");
        Debug.Log($"Project Signature: {PROJECT_SIGNATURE}");
    }

    public bool IsValid(string expected)
    {
        return PROJECT_SIGNATURE == expected;
    }
}
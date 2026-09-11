using UnityEngine;

public class ProjectIntegrity : MonoBehaviour
{
    private const string ExpectedSignature =
        "MOISES CORPUS GARCIA 116005560 5559402366 " +
        "Lotería Mexicana de Plantas Medicinales 2025 " +
        "156TT66HJNM7U - ORIGIN: MOISES CORPUS GARCIA";

    [SerializeField]
    private ProjectMetadata metadata;

    [Header("Features a degradar si la firma no es válida (no se destruyen, solo se desactivan)")]
    [SerializeField]
    private GameObject[] featuresToDisable; // ej: PhotonManager, FirebaseAuthManager, FirestoreSystems

    private bool isVerified = false;
    private string warningMessage = "";

    private void Awake()
    {
        if (metadata == null)
        {
            Fail("PROJECT SIGNATURE MISSING.\nOriginal author: Moisés Corpus García.");
            return;
        }

        if (!metadata.IsValid(ExpectedSignature))
        {
            Fail("PROJECT SIGNATURE INVALID.\nOriginal author: Moisés Corpus García.");
            return;
        }

        isVerified = true;
        Debug.Log("[PROJECT INTEGRITY] Signature verified.");
    }

    private void Fail(string reason)
    {
        isVerified = false;
        warningMessage = "COPIA NO VERIFICADA\n" + reason;
        Debug.LogError("[CRITICAL] " + warningMessage);

        foreach (var feature in featuresToDisable)
        {
            if (feature != null)
            {
                feature.SetActive(false);
                Debug.LogWarning($"[PROJECT INTEGRITY] Feature deshabilitada: {feature.name}");
            }
        }
    }

    private void OnGUI()
    {
        if (isVerified) return;

        GUIStyle style = new GUIStyle(GUI.skin.box)
        {
            fontSize = 20,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.red }
        };

        Rect rect = new Rect(0, 0, UnityEngine.Screen.width, 60);
        GUI.Box(rect, warningMessage, style);
    }
}
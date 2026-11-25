using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// One-time project defaults for the sandbox: gamma colour space, background running, open the main scene.
[InitializeOnLoad]
public static class ProjectSetup
{
    static ProjectSetup()
    {
        if (PlayerSettings.colorSpace != ColorSpace.Gamma) PlayerSettings.colorSpace = ColorSpace.Gamma;
        PlayerSettings.runInBackground = true;
        PlayerSettings.productName = "Frontline Sandbox";
        EditorApplication.delayCall += OpenMain;
    }

    static void OpenMain()
    {
        if (Application.isPlaying) return;
        var sc = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(sc.path) && !sc.isDirty && System.IO.File.Exists("Assets/Scenes/Main.unity"))
            EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
    }

    [MenuItem("RTS/Open Main Scene")]
    static void Menu() { EditorSceneManager.OpenScene("Assets/Scenes/Main.unity"); }
}

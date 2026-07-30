// ============================================================
// SpriteBakerBakeSceneMenu — 베이크 전용 씬 열기 / Play
// ============================================================

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SpriteBakerBakeSceneMenu
{
    public const string BakeScenePath = "Assets/Dist/Scenes/SpriteBakerBake.unity";

    [MenuItem("Dist/SpriteBaker/Open Bake Scene")]
    static void OpenBakeScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        if (!System.IO.File.Exists(BakeScenePath))
        {
            Debug.LogError($"[SpriteBakerBakeSceneMenu] Missing scene: {BakeScenePath}");
            return;
        }

        EditorSceneManager.OpenScene(BakeScenePath);
    }

    [MenuItem("Dist/SpriteBaker/Play Bake Scene")]
    static void PlayBakeScene()
    {
        OpenBakeScene();
        if (!Application.isPlaying)
            EditorApplication.isPlaying = true;
    }
}

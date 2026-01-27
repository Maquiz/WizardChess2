using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor utility to set up the MainMenu scene and configure Build Settings.
/// Run via: WizardChess > Setup MainMenu Scene
/// </summary>
public class MainMenuSceneSetup
{
    [MenuItem("WizardChess/Setup MainMenu Scene")]
    public static void SetupMainMenuScene()
    {
        // Save current scene first
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        string scenePath = "Assets/Scenes/MainMenu.unity";

        // Create scene if it doesn't exist
        if (!System.IO.File.Exists(System.IO.Path.Combine(Application.dataPath, "../", scenePath)))
        {
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(newScene, scenePath);
            Debug.Log("[MainMenuSetup] Created new scene at " + scenePath);
        }
        else
        {
            EditorSceneManager.OpenScene(scenePath);
            Debug.Log("[MainMenuSetup] Opened existing scene at " + scenePath);
        }

        // Clear scene (except Camera if present)
        var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var obj in allObjects)
        {
            if (obj != null && obj.transform.parent == null)
            {
                Object.DestroyImmediate(obj);
            }
        }

        // Create Main Camera
        GameObject cameraObj = new GameObject("Main Camera");
        cameraObj.tag = "MainCamera";
        Camera cam = cameraObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.03f, 0.1f, 1f);
        cam.orthographic = false;
        cameraObj.AddComponent<AudioListener>();

        // Create Directional Light
        GameObject lightObj = new GameObject("Directional Light");
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.5f;
        lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);

        // Create MainMenuController
        GameObject controllerObj = new GameObject("MainMenuController");
        controllerObj.AddComponent<MainMenuUI>();

        // Save scene
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), scenePath);
        Debug.Log("[MainMenuSetup] MainMenu scene setup complete!");

        // Add to Build Settings
        SetupBuildSettings(scenePath);
    }

    private static void SetupBuildSettings(string mainMenuPath)
    {
        string boardPath = "Assets/Scenes/Board.unity";

        var scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(mainMenuPath, true),
            new EditorBuildSettingsScene(boardPath, true)
        };

        EditorBuildSettings.scenes = scenes;
        Debug.Log("[MainMenuSetup] Build Settings updated: MainMenu (index 0), Board (index 1)");
    }
}

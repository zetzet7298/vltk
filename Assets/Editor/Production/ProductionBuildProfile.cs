using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VLTK.Production.App;

namespace VLTK.Production.Editor
{
    public static class ProductionBuildProfile
    {
        public const string ScenePath = "Assets/Scenes/Production/ProductionBootstrap.unity";

        [MenuItem("VLTK/Production/Validate Editor Entry")]
        public static void ValidateEditorEntry()
        {
            bool sceneRegistered = EditorBuildSettings.scenes.Any(s => s.path == ScenePath && s.enabled);
            if (!sceneRegistered)
                throw new System.InvalidOperationException("Production scene missing from EditorBuildSettings");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (Object.FindFirstObjectByType<ProductionBootstrapper>() == null)
                throw new System.InvalidOperationException("ProductionBootstrapper missing from production scene");
            Debug.Log("Production editor entry validated: " + scene.path);
        }
    }
}

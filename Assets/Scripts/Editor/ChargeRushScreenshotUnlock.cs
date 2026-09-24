using System.IO;
using ChargeRush.Core;
using ChargeRush.Data;
using ChargeRush.Save;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChargeRush.Editor
{
    /// <summary>One-click fully unlocked save for App Store / marketing screenshots.</summary>
    public static class ChargeRushScreenshotUnlock
    {
        private const string CatalogPath = "Assets/Resources/GameData/GameCatalog.asset";
        private const string SaveFileName = "chargerush_save.json";

        [MenuItem("ChargeRush/Screenshot Prep/Unlock All Levels & Achievements")]
        public static void UnlockAll()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = Resources.Load<GameCatalog>("GameData/GameCatalog");
            }

            if (catalog == null)
            {
                EditorUtility.DisplayDialog(
                    "ChargeRush",
                    "Could not find GameCatalog. Open Boot once or run Build All Content.",
                    "OK");
                return;
            }

            if (Application.isPlaying && SaveManager.Instance != null)
            {
                SaveManager.Instance.UnlockAllForScreenshots(catalog);
                ReloadUi();
                Debug.Log("ChargeRush: unlocked all levels, challenges, and achievements for screenshots.");
                return;
            }

            var data = SaveManager.BuildScreenshotSave(catalog);
            data.Version = SaveManager.CurrentVersion;
            var path = Path.Combine(Application.persistentDataPath, SaveFileName);
            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllText(path, JsonUtility.ToJson(data, true));
            Debug.Log($"ChargeRush: screenshot save written to {path}");
            EditorUtility.DisplayDialog(
                "ChargeRush",
                "Screenshot save written.\n\nEnter Play from Boot to load the unlocked progress.",
                "OK");
        }

        [MenuItem("ChargeRush/Screenshot Prep/Reset Progress")]
        public static void ResetProgress()
        {
            if (Application.isPlaying && SaveManager.Instance != null)
            {
                SaveManager.Instance.ResetProgress();
                ReloadUi();
                Debug.Log("ChargeRush: progress reset.");
                return;
            }

            var path = Path.Combine(Application.persistentDataPath, SaveFileName);
            var backup = Path.Combine(Application.persistentDataPath, "chargerush_save.bak.json");
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            if (File.Exists(backup))
            {
                File.Delete(backup);
            }

            EditorUtility.DisplayDialog(
                "ChargeRush",
                "Save files deleted.\n\nEnter Play from Boot for a fresh start.",
                "OK");
        }

        private static void ReloadUi()
        {
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.Load(SceneLoader.MainMenuScene);
                return;
            }

            var scene = SceneManager.GetActiveScene();
            if (scene.IsValid() && !string.IsNullOrEmpty(scene.path))
            {
                SceneManager.LoadScene(scene.buildIndex);
            }
        }
    }
}

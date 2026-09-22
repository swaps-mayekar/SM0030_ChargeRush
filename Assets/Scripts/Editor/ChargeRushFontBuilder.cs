using System.Collections.Generic;
using System.IO;
using ChargeRush.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace ChargeRush.Editor
{
    /// <summary>
    /// Builds TMP SDF assets from Google Fonts and applies them across UI scenes.
    /// Heading/buttons: Outfit Bold. Body: DM Sans Regular.
    /// </summary>
    public static class ChargeRushFontBuilder
    {
        private const string SourceRoot = "Assets/Fonts/Google";
        private const string HeadingTtf = SourceRoot + "/Outfit-Bold.ttf";
        private const string BodyTtf = SourceRoot + "/DMSans-Regular.ttf";
        private const string ResourcesFontsRoot = "Assets/Resources/Fonts";
        private const string HeadingAssetPath = ResourcesFontsRoot + "/OutfitBold SDF.asset";
        private const string BodyAssetPath = ResourcesFontsRoot + "/DMSansRegular SDF.asset";

        private static readonly string[] ScenePaths = ChargeRushAuthoredScenes.All;

        private static readonly HashSet<string> HeadingNames = new HashSet<string>
        {
            "Title",
            "UpgradesTitle",
            "PauseTitle",
            "CompleteTitle",
            "FailTitle",
            "Label",
            "PaymentPopup",
            "Stars"
        };

        [MenuItem("ChargeRush/Build UI Fonts")]
        public static void BuildUiFonts()
        {
            var pair = EnsureFontAssets();
            if (pair.heading == null || pair.body == null)
            {
                Debug.LogError("ChargeRush: Failed to build one or more UI font assets.");
                return;
            }

            UiFonts.SetEditorOverrides(pair.heading, pair.body);
            ApplyFontsToScenesOnDisk(pair.heading, pair.body);
            UiFonts.ClearEditorOverrides();

            SetDefaultTmpFont(pair.body);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("ChargeRush: UI fonts ready — Outfit Bold (heading/buttons), DM Sans (body).");
        }

        public static (TMP_FontAsset heading, TMP_FontAsset body) EnsureFontAssets()
        {
            EnsureFolders();
            AssetDatabase.ImportAsset(HeadingTtf, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(BodyTtf, ImportAssetOptions.ForceUpdate);

            var heading = CreateOrUpdateFontAsset(HeadingTtf, HeadingAssetPath, "OutfitBold SDF");
            var body = CreateOrUpdateFontAsset(BodyTtf, BodyAssetPath, "DMSansRegular SDF");
            if (heading != null && body != null)
            {
                UiFonts.SetEditorOverrides(heading, body);
                SetDefaultTmpFont(body);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return (heading, body);
        }

        private static void SetDefaultTmpFont(TMP_FontAsset body)
        {
            var tmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
            var tmpSettings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(tmpSettingsPath);
            if (tmpSettings == null)
            {
                return;
            }

            var so = new SerializedObject(tmpSettings);
            var defaultFont = so.FindProperty("m_defaultFontAsset");
            if (defaultFont == null)
            {
                return;
            }

            defaultFont.objectReferenceValue = body;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tmpSettings);
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            if (!AssetDatabase.IsValidFolder(ResourcesFontsRoot))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Fonts");
            }
        }

        private static TMP_FontAsset CreateOrUpdateFontAsset(string ttfPath, string assetPath, string assetName)
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (source == null)
            {
                Debug.LogError($"ChargeRush: Missing source font at {ttfPath}. Import the TTF first.");
                return null;
            }

            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath) != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            var fontAsset = TMP_FontAsset.CreateFontAsset(
                source,
                90,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic);

            if (fontAsset == null)
            {
                Debug.LogError($"ChargeRush: TMP_FontAsset.CreateFontAsset failed for {ttfPath}");
                return null;
            }

            fontAsset.name = assetName;
            AssetDatabase.CreateAsset(fontAsset, assetPath);

            if (fontAsset.atlasTexture != null)
            {
                fontAsset.atlasTexture.name = assetName + " Atlas";
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
            }

            if (fontAsset.material != null)
            {
                fontAsset.material.name = assetName + " Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            EditorUtility.SetDirty(fontAsset);
            return fontAsset;
        }

        private static void ApplyFontsToScenesOnDisk(TMP_FontAsset heading, TMP_FontAsset body)
        {
            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            for (var i = 0; i < ScenePaths.Length; i++)
            {
                var path = ScenePaths[i];
                if (!File.Exists(path))
                {
                    continue;
                }

                if (ChargeRushAuthoredScenes.TryPreserve(path, "font apply"))
                {
                    continue;
                }

                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var texts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (var t = 0; t < texts.Length; t++)
                {
                    ApplyRole(texts[t], heading, body);
                    EditorUtility.SetDirty(texts[t]);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            if (!string.IsNullOrEmpty(active) && File.Exists(active))
            {
                EditorSceneManager.OpenScene(active, OpenSceneMode.Single);
            }
        }

        private static void ApplyRole(TextMeshProUGUI text, TMP_FontAsset heading, TMP_FontAsset body)
        {
            if (text == null)
            {
                return;
            }

            var role = ResolveRole(text.gameObject.name);
            text.font = role == UiTextRole.Heading ? heading : body;
        }

        internal static UiTextRole ResolveRole(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
            {
                return UiTextRole.Body;
            }

            if (HeadingNames.Contains(objectName) || objectName.EndsWith("Title"))
            {
                return UiTextRole.Heading;
            }

            return UiTextRole.Body;
        }
    }
}

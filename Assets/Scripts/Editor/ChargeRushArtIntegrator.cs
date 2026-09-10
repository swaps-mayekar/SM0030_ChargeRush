using System.Collections.Generic;
using System.IO;
using ChargeRush.Data;
using ChargeRush.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ChargeRush.Editor
{
    /// <summary>Imports and wires production art into ScriptableObjects, prefabs, and scenes.</summary>
    public static class ChargeRushArtIntegrator
    {
        private const string ArtRoot = "Assets/Art";

        [MenuItem("ChargeRush/Apply Final Art")]
        public static void ApplyFinalArt()
        {
            ConfigureSpriteImports();
            AssetDatabase.Refresh();

            var devices = LoadAll<DeviceData>("Assets/ScriptableObjects/Devices");
            var customers = LoadAll<CustomerData>("Assets/ScriptableObjects/Customers");
            var levels = LoadAll<LevelData>("Assets/ScriptableObjects/Levels");

            ApplyDeviceArt(devices);
            ApplyCustomerArt(customers);
            ApplyLevelBackgrounds(levels);
            UpdatePrefabs();
            UpdateGameplayScene();
            UpdateMainMenuScene();
            UpdateHudChrome();
            AssignAppIcon();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("ChargeRush final art applied.");
        }

        private static void AssignAppIcon()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ArtRoot}/Icons/app_icon.png");
            if (icon == null)
            {
                return;
            }

            PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.iOS, new[] { icon }, IconKind.Application);
            PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Standalone, new[] { icon }, IconKind.Application);
        }

        private static void ConfigureSpriteImports()
        {
            var folders = new[]
            {
                $"{ArtRoot}/Characters",
                $"{ArtRoot}/Devices",
                $"{ArtRoot}/Environment",
                $"{ArtRoot}/UI",
                $"{ArtRoot}/Effects",
                $"{ArtRoot}/Connectors",
                $"{ArtRoot}/Icons"
            };

            foreach (var folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    continue;
                }

                var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
                for (var i = 0; i < guids.Length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null)
                    {
                        continue;
                    }

                    var isBackground = path.Contains("/bg_");
                    var isCounter = path.Contains("/counter");
                    var isWorldArt = path.Contains("/Characters/")
                        || path.Contains("/Devices/")
                        || path.Contains("/Effects/")
                        || path.Contains("/Connectors/")
                        || path.Contains("/Environment/");
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.spritePixelsPerUnit = isBackground
                        ? 100f
                        : isCounter
                            ? 256f
                            : isWorldArt
                                ? 512f
                                : 128f;
                    importer.maxTextureSize = isBackground ? 2048 : 1024;
                    importer.textureCompression = TextureImporterCompression.CompressedHQ;
                    importer.SaveAndReimport();
                }
            }
        }

        private static void ApplyDeviceArt(List<DeviceData> devices)
        {
            var charged = LoadSprite($"{ArtRoot}/Devices/device_charged_glow.png");
            var map = new Dictionary<string, string>
            {
                ["device_basic_phone"] = $"{ArtRoot}/Devices/device_basic_phone.png",
                ["device_advanced_phone"] = $"{ArtRoot}/Devices/device_advanced_phone.png",
                ["device_tablet"] = $"{ArtRoot}/Devices/device_tablet.png",
                ["device_powerbank"] = $"{ArtRoot}/Devices/device_powerbank.png",
                ["device_camera"] = $"{ArtRoot}/Devices/device_camera.png",
                ["device_handheld"] = $"{ArtRoot}/Devices/device_handheld.png",
                ["device_notebook"] = $"{ArtRoot}/Devices/device_notebook.png"
            };

            for (var i = 0; i < devices.Count; i++)
            {
                var device = devices[i];
                if (device == null || !map.TryGetValue(device.DeviceId, out var path))
                {
                    continue;
                }

                var idle = LoadSprite(path);
                device.AssignSprites(idle, idle, charged != null ? charged : idle);
                EditorUtility.SetDirty(device);
            }
        }

        private static void ApplyCustomerArt(List<CustomerData> customers)
        {
            var map = new Dictionary<string, string>
            {
                ["customer_regular"] = $"{ArtRoot}/Characters/customer_regular.png",
                ["customer_impatient"] = $"{ArtRoot}/Characters/customer_impatient.png",
                ["customer_generous"] = $"{ArtRoot}/Characters/customer_generous.png",
                ["customer_patient"] = $"{ArtRoot}/Characters/customer_patient.png",
                ["customer_group"] = $"{ArtRoot}/Characters/customer_group.png",
                ["customer_vip"] = $"{ArtRoot}/Characters/customer_vip.png"
            };

            for (var i = 0; i < customers.Count; i++)
            {
                var customer = customers[i];
                if (customer == null || !map.TryGetValue(customer.CustomerId, out var path))
                {
                    continue;
                }

                customer.AssignSprite(LoadSprite(path));
                EditorUtility.SetDirty(customer);
            }
        }

        private static void ApplyLevelBackgrounds(List<LevelData> levels)
        {
            for (var i = 0; i < levels.Count; i++)
            {
                var level = levels[i];
                if (level == null)
                {
                    continue;
                }

                Sprite bg;
                switch (level.LevelNumber)
                {
                    case 1:
                    case 2:
                        bg = LoadSprite($"{ArtRoot}/Environment/bg_fair.png");
                        break;
                    case 3:
                        bg = LoadSprite($"{ArtRoot}/Environment/bg_food.png");
                        break;
                    case 5:
                        bg = LoadSprite($"{ArtRoot}/Environment/bg_sports.png");
                        break;
                    case 6:
                    case 7:
                    case 8:
                    case 10:
                        bg = LoadSprite($"{ArtRoot}/Environment/bg_festival.png");
                        break;
                    case 9:
                        bg = LoadSprite($"{ArtRoot}/Environment/bg_expo.png");
                        break;
                    default:
                        bg = LoadSprite($"{ArtRoot}/Environment/bg_fair.png");
                        break;
                }

                level.AssignBackground(bg);
                EditorUtility.SetDirty(level);
            }
        }

        private static void UpdatePrefabs()
        {
            UpdateCustomerPrefab();
            UpdateDevicePrefab();
            UpdatePortPrefab();
        }

        private static void UpdateCustomerPrefab()
        {
            var path = "Assets/Prefabs/Characters/Customer.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            var sr = root.GetComponent<SpriteRenderer>();
            var sprite = LoadSprite($"{ArtRoot}/Characters/customer_regular.png");
            if (sr != null && sprite != null)
            {
                sr.sprite = sprite;
                sr.color = Color.white;
                root.transform.localScale = Vector3.one * 1.35f;
            }

            var col = root.GetComponent<BoxCollider2D>();
            if (col != null)
            {
                col.size = new Vector2(1.1f, 1.8f);
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void UpdateDevicePrefab()
        {
            var path = "Assets/Prefabs/Devices/Device.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            var sr = root.GetComponent<SpriteRenderer>();
            var sprite = LoadSprite($"{ArtRoot}/Devices/device_basic_phone.png");
            if (sr != null && sprite != null)
            {
                sr.sprite = sprite;
                sr.color = Color.white;
                root.transform.localScale = Vector3.one * 0.85f;
            }

            var badge = root.transform.Find("ConnectorBadge");
            if (badge != null)
            {
                var badgeSr = badge.GetComponent<SpriteRenderer>();
                var badgeSprite = LoadSprite($"{ArtRoot}/Connectors/badge_powerlink_a.png");
                if (badgeSr != null && badgeSprite != null)
                {
                    badgeSr.sprite = badgeSprite;
                    badgeSr.color = Color.white;
                    badge.localScale = Vector3.one * 0.35f;
                }
            }

            var progress = root.transform.Find("Progress");
            if (progress != null)
            {
                var fill = LoadSprite($"{ArtRoot}/Effects/charge_fill.png");
                var psr = progress.GetComponent<SpriteRenderer>();
                if (psr != null && fill != null)
                {
                    psr.sprite = fill;
                    psr.color = Color.white;
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void UpdatePortPrefab()
        {
            var path = "Assets/Prefabs/Stations/ChargingPort.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            var sr = root.GetComponent<SpriteRenderer>();
            var sprite = LoadSprite($"{ArtRoot}/Environment/port_base.png");
            if (sr != null && sprite != null)
            {
                sr.sprite = sprite;
                sr.color = Color.white;
                root.transform.localScale = Vector3.one * 0.9f;
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void UpdateGameplayScene()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/3_Gameplay.unity");
            var bg = GameObject.Find("Background");
            if (bg != null)
            {
                var sr = bg.GetComponent<SpriteRenderer>();
                var sprite = LoadSprite($"{ArtRoot}/Environment/bg_fair.png");
                if (sr != null && sprite != null)
                {
                    sr.sprite = sprite;
                    sr.color = Color.white;
                    sr.sortingOrder = -20;
                    bg.transform.localScale = new Vector3(1f, 1f, 1f);
                }
            }

            var counter = GameObject.Find("Counter");
            if (counter != null)
            {
                var sr = counter.GetComponent<SpriteRenderer>();
                var sprite = LoadSprite($"{ArtRoot}/Environment/counter_final.png");
                if (sr != null && sprite != null)
                {
                    sr.sprite = sprite;
                    sr.color = Color.white;
                    counter.transform.localScale = new Vector3(1.8f, 1.8f, 1f);
                    counter.transform.position = new Vector3(0f, -2.35f, 0f);
                }
            }

            EditorSceneManager.SaveScene(scene);
        }

        private static void UpdateMainMenuScene()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/1_MainMenu.unity");
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorSceneManager.SaveScene(scene);
                return;
            }

            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1
                | AdditionalCanvasShaderChannels.Normal
                | AdditionalCanvasShaderChannels.Tangent;

            var safe = canvas.transform.Find("SafeArea");
            if (safe == null)
            {
                EditorSceneManager.SaveScene(scene);
                return;
            }

            EnsureImage(safe, "MenuBackground", LoadSprite($"{ArtRoot}/Environment/bg_menu.png"), new Vector2(0f, 0f), new Vector2(1920f, 1080f), 0, false);
            EnsureImage(safe, "Logo", LoadSprite($"{ArtRoot}/UI/logo_chargerush.png"), new Vector2(0f, 280f), new Vector2(720f, 220f), 1);

            StyleButton(safe.Find("Play"), LoadSprite($"{ArtRoot}/UI/button_primary.png"));
            StyleButton(safe.Find("LevelSelect"), LoadSprite($"{ArtRoot}/UI/button_primary.png"));
            StyleButton(safe.Find("Challenge"), LoadSprite($"{ArtRoot}/UI/button_primary.png"));
            StyleButton(safe.Find("Endless"), LoadSprite($"{ArtRoot}/UI/button_primary.png"));
            StyleButton(safe.Find("Achievements"), LoadSprite($"{ArtRoot}/UI/button_primary.png"));
            StyleButton(safe.Find("Settings"), LoadSprite($"{ArtRoot}/UI/button_primary.png"));
            StyleButton(safe.Find("Upgrades"), LoadSprite($"{ArtRoot}/UI/button_primary.png"));

            var title = safe.Find("Title");
            if (title != null)
            {
                title.gameObject.SetActive(false);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void UpdateHudChrome()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/3_Gameplay.unity");
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                return;
            }

            var safe = canvas.transform.Find("SafeArea");
            if (safe == null)
            {
                return;
            }

            EnsureImage(safe, "CreditIcon", LoadSprite($"{ArtRoot}/UI/credit_coin.png"), new Vector2(-520f, 200f), new Vector2(64f, 64f), 5);
            EnsureImage(safe, "MistakeIcon", LoadSprite($"{ArtRoot}/UI/mistake.png"), new Vector2(250f, 200f), new Vector2(56f, 56f), 5);
            StyleButton(safe.Find("Pause"), LoadSprite($"{ArtRoot}/UI/button_primary.png"));

            StylePanel(safe.Find("PausePanel"));
            StylePanel(safe.Find("CompletePanel"));
            StylePanel(safe.Find("FailPanel"));

            // Star row on complete panel
            var complete = safe.Find("CompletePanel");
            if (complete != null)
            {
                for (var i = 0; i < 3; i++)
                {
                    EnsureImage(complete, $"Star_{i}", LoadSprite($"{ArtRoot}/UI/star.png"), new Vector2(-70f + i * 70f, -55f), new Vector2(56f, 56f), 6);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void StylePanel(Transform panel)
        {
            if (panel == null)
            {
                return;
            }

            var image = panel.GetComponent<Image>();
            var sprite = LoadSprite($"{ArtRoot}/UI/panel_final.png");
            if (image != null && sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }
        }

        private static void StyleButton(Transform buttonTransform, Sprite sprite)
        {
            if (buttonTransform == null || sprite == null)
            {
                return;
            }

            var image = buttonTransform.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }
        }

        private static void EnsureImage(Transform parent, string name, Sprite sprite, Vector2 pos, Vector2 size, int siblingIndex, bool raycastTarget = true)
        {
            if (parent == null || sprite == null)
            {
                return;
            }

            var existing = parent.Find(name);
            GameObject go;
            if (existing == null)
            {
                go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(parent, false);
            }
            else
            {
                go = existing.gameObject;
                if (go.GetComponent<Image>() == null)
                {
                    go.AddComponent<Image>();
                }
            }

            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = raycastTarget;
            go.transform.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, parent.childCount - 1));
        }

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static List<T> LoadAll<T>(string folder) where T : Object
        {
            var list = new List<T>();
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return list;
            }

            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder });
            for (var i = 0; i < guids.Length; i++)
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (asset != null)
                {
                    list.Add(asset);
                }
            }

            return list;
        }
    }
}

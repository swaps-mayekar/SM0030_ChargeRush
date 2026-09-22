using System.Collections.Generic;
using System.IO;
using ChargeRush.Charging;
using ChargeRush.Core;
using ChargeRush.Customers;
using ChargeRush.Data;
using ChargeRush.Devices;
using ChargeRush.Economy;
using ChargeRush.Gameplay;
using ChargeRush.Input;
using ChargeRush.Progression;
using ChargeRush.Save;
using ChargeRush.Tutorial;
using ChargeRush.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ChargeRush.Editor
{
    /// <summary>Idempotent generator for ChargeRush placeholder content, prefabs, data, and scenes.</summary>
    public static class ChargeRushContentBuilder
    {
        private const string Root = "Assets";
        private const string ArtRoot = "Assets/Art";
        private const string DataRoot = "Assets/ScriptableObjects";
        private const string PrefabRoot = "Assets/Prefabs";
        private const string SceneRoot = "Assets/Scenes";
        private const string ResourcesRoot = "Assets/Resources/GameData";

        [MenuItem("ChargeRush/Build All Content")]
        public static void BuildAll()
        {
            ChargeRushFontBuilder.EnsureFontAssets();
            EnsureFolders();
            var sprites = BuildSprites();
            var devices = BuildDevices(sprites);
            var customers = BuildCustomers(sprites);
            var upgrades = BuildUpgrades();
            var achievements = BuildAchievements();
            var levels = BuildLevels(devices, customers);
            var challenges = BuildChallenges(levels);
            var catalog = BuildCatalog(devices, customers, levels, upgrades, achievements, challenges);
            var prefabs = BuildPrefabs(sprites);
            BuildScenes(catalog, prefabs, sprites);
            ConfigureProjectSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (File.Exists("Assets/Art/Characters/customer_regular.png"))
            {
                ChargeRushArtIntegrator.ApplyFinalArt();
            }

            ChargeRushFontBuilder.BuildUiFonts();
            Debug.Log("ChargeRush content build complete.");
        }

        [MenuItem("ChargeRush/Validate Content")]
        public static void ValidateContent()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>($"{ResourcesRoot}/GameCatalog.asset");
            if (catalog == null)
            {
                Debug.LogError("Missing GameCatalog.");
                return;
            }

            var errors = 0;
            var ids = new HashSet<string>();
            ValidateList("Device", catalog.Devices, d => d.DeviceId, d => d.IdleSprite == null, ids, ref errors);
            ids.Clear();
            ValidateList("Customer", catalog.Customers, c => c.CustomerId, c => c.PortraitSprite == null, ids, ref errors);
            ids.Clear();
            ValidateList("Level", catalog.StoryLevels, l => l.LevelId, l => l.TargetEarnings <= 0 || l.TwoStarThreshold < l.TargetEarnings || l.ThreeStarThreshold < l.TwoStarThreshold, ids, ref errors);
            ids.Clear();
            ValidateList("Achievement", catalog.Achievements, a => a.AchievementId, a => a.TargetValue <= 0, ids, ref errors);

            for (var i = 0; i < catalog.StoryLevels.Count; i++)
            {
                var level = catalog.StoryLevels[i];
                if (level.AvailableDevices.Count == 0 || level.AvailableCustomers.Count == 0)
                {
                    Debug.LogError($"Level {level.LevelId} missing devices/customers.");
                    errors++;
                }

                if (level.ChargingPortCount <= 0)
                {
                    Debug.LogError($"Level {level.LevelId} has invalid ports.");
                    errors++;
                }
            }

            Debug.Log(errors == 0 ? "ChargeRush validation passed." : $"ChargeRush validation found {errors} issue(s).");
        }

        private static void ValidateList<T>(
            string label,
            IReadOnlyList<T> list,
            System.Func<T, string> idSelector,
            System.Func<T, bool> invalidPredicate,
            HashSet<string> ids,
            ref int errors) where T : Object
        {
            for (var i = 0; i < list.Count; i++)
            {
                var item = list[i];
                if (item == null)
                {
                    Debug.LogError($"{label} entry {i} is null.");
                    errors++;
                    continue;
                }

                var id = idSelector(item);
                if (string.IsNullOrEmpty(id) || !ids.Add(id))
                {
                    Debug.LogError($"{label} duplicate/missing id: {id}");
                    errors++;
                }

                if (invalidPredicate(item))
                {
                    Debug.LogError($"{label} invalid data: {id}");
                    errors++;
                }
            }
        }

        private static void EnsureFolders()
        {
            CreateFolder("Assets/Art/Characters");
            CreateFolder("Assets/Art/Devices");
            CreateFolder("Assets/Art/Environment");
            CreateFolder("Assets/Art/UI");
            CreateFolder("Assets/Art/Effects");
            CreateFolder("Assets/Prefabs/Characters");
            CreateFolder("Assets/Prefabs/Devices");
            CreateFolder("Assets/Prefabs/Stations");
            CreateFolder("Assets/Prefabs/UI");
            CreateFolder("Assets/Scenes");
            CreateFolder("Assets/ScriptableObjects/Levels");
            CreateFolder("Assets/ScriptableObjects/Devices");
            CreateFolder("Assets/ScriptableObjects/Customers");
            CreateFolder("Assets/ScriptableObjects/Upgrades");
            CreateFolder("Assets/ScriptableObjects/Achievements");
            CreateFolder("Assets/ScriptableObjects/Challenges");
            CreateFolder("Assets/Resources/GameData");
        }

        private static void CreateFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                CreateFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }

        private static Dictionary<string, Sprite> BuildSprites()
        {
            var map = new Dictionary<string, Sprite>();
            map["customer"] = CreateSprite($"{ArtRoot}/Characters/customer_body.png", 64, 96, new Color(0.95f, 0.75f, 0.55f), DrawCustomer);
            map["device_phone"] = CreateSprite($"{ArtRoot}/Devices/device_phone.png", 48, 72, new Color(0.25f, 0.45f, 0.85f), DrawPhone);
            map["device_phone_b"] = CreateSprite($"{ArtRoot}/Devices/device_phone_b.png", 48, 72, new Color(0.2f, 0.7f, 0.45f), DrawPhone);
            map["device_tablet"] = CreateSprite($"{ArtRoot}/Devices/device_tablet.png", 72, 96, new Color(0.35f, 0.55f, 0.75f), DrawTablet);
            map["device_bank"] = CreateSprite($"{ArtRoot}/Devices/device_bank.png", 64, 40, new Color(0.2f, 0.2f, 0.25f), DrawBank);
            map["device_camera"] = CreateSprite($"{ArtRoot}/Devices/device_camera.png", 72, 48, new Color(0.55f, 0.55f, 0.6f), DrawCamera);
            map["device_game"] = CreateSprite($"{ArtRoot}/Devices/device_game.png", 80, 48, new Color(0.75f, 0.35f, 0.45f), DrawGame);
            map["device_notebook"] = CreateSprite($"{ArtRoot}/Devices/device_notebook.png", 96, 64, new Color(0.45f, 0.45f, 0.5f), DrawNotebook);
            map["device_charged"] = CreateSprite($"{ArtRoot}/Devices/device_charged.png", 48, 72, new Color(0.35f, 0.9f, 0.45f), DrawPhone);
            map["port"] = CreateSprite($"{ArtRoot}/Environment/port.png", 48, 48, new Color(0.3f, 0.3f, 0.35f), DrawPort);
            map["counter"] = CreateSprite($"{ArtRoot}/Environment/counter.png", 256, 64, new Color(0.45f, 0.32f, 0.22f), DrawRect);
            map["bg"] = CreateSprite($"{ArtRoot}/Environment/bg.png", 32, 32, new Color(0.55f, 0.78f, 0.95f), DrawRect);
            map["ui_panel"] = CreateSprite($"{ArtRoot}/UI/panel.png", 32, 32, new Color(0.12f, 0.16f, 0.22f, 0.85f), DrawRect);
            return map;
        }

        private delegate void SpritePainter(Color[] pixels, int w, int h, Color color);

        private static Sprite CreateSprite(string path, int width, int height, Color color, SpritePainter painter)
        {
            // Preserve production art if a larger authored file already exists.
            if (File.Exists(path))
            {
                var info = new FileInfo(path);
                if (info.Length > 2048)
                {
                    AssetDatabase.ImportAsset(path);
                    return AssetDatabase.LoadAssetAtPath<Sprite>(path);
                }
            }

            var pixels = new Color[width * height];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }

            painter(pixels, width, height, color);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 64f;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void FillRect(Color[] px, int w, int h, int x, int y, int rw, int rh, Color c)
        {
            for (var yy = y; yy < y + rh; yy++)
            {
                for (var xx = x; xx < x + rw; xx++)
                {
                    if (xx >= 0 && yy >= 0 && xx < w && yy < h)
                    {
                        px[yy * w + xx] = c;
                    }
                }
            }
        }

        private static void DrawRect(Color[] px, int w, int h, Color c) => FillRect(px, w, h, 0, 0, w, h, c);
        private static void DrawCustomer(Color[] px, int w, int h, Color c)
        {
            FillRect(px, w, h, 18, 60, 28, 28, c);
            FillRect(px, w, h, 12, 10, 40, 50, c * 0.9f);
        }

        private static void DrawPhone(Color[] px, int w, int h, Color c)
        {
            FillRect(px, w, h, 8, 4, w - 16, h - 8, c);
            FillRect(px, w, h, 12, 12, w - 24, h - 28, Color.white * 0.85f);
        }

        private static void DrawTablet(Color[] px, int w, int h, Color c)
        {
            FillRect(px, w, h, 4, 4, w - 8, h - 8, c);
            FillRect(px, w, h, 10, 10, w - 20, h - 20, Color.white * 0.8f);
        }

        private static void DrawBank(Color[] px, int w, int h, Color c)
        {
            FillRect(px, w, h, 4, 8, w - 8, h - 16, c);
            FillRect(px, w, h, 10, h / 2 - 4, w - 20, 8, new Color(0.3f, 0.9f, 0.4f));
        }

        private static void DrawCamera(Color[] px, int w, int h, Color c)
        {
            FillRect(px, w, h, 8, 8, w - 16, h - 16, c);
            FillRect(px, w, h, w / 2 - 10, h / 2 - 10, 20, 20, Color.black);
        }

        private static void DrawGame(Color[] px, int w, int h, Color c)
        {
            FillRect(px, w, h, 4, 8, w - 8, h - 16, c);
            FillRect(px, w, h, 12, 16, 24, 16, Color.white * 0.8f);
        }

        private static void DrawNotebook(Color[] px, int w, int h, Color c)
        {
            FillRect(px, w, h, 4, 8, w - 8, h / 2, c);
            FillRect(px, w, h, 8, h / 2 + 4, w - 16, h / 2 - 12, c * 0.8f);
        }

        private static void DrawPort(Color[] px, int w, int h, Color c)
        {
            FillRect(px, w, h, 4, 4, w - 8, h - 8, c);
            FillRect(px, w, h, 14, 14, w - 28, h - 28, Color.black);
        }

        private static List<DeviceData> BuildDevices(Dictionary<string, Sprite> sprites)
        {
            var list = new List<DeviceData>
            {
                CreateDevice("device_basic_phone", "Basic Smartphone", DeviceCategory.BasicSmartphone, ConnectorType.PowerLinkA, 100, 5f, 20, new Color(0.25f, 0.45f, 0.85f), sprites["device_phone"], sprites["device_phone"], sprites["device_charged"]),
                CreateDevice("device_advanced_phone", "Advanced Smartphone", DeviceCategory.AdvancedSmartphone, ConnectorType.PowerLinkB, 120, 6f, 30, new Color(0.2f, 0.7f, 0.45f), sprites["device_phone_b"], sprites["device_phone_b"], sprites["device_charged"]),
                CreateDevice("device_tablet", "Tablet", DeviceCategory.Tablet, ConnectorType.PowerLinkB, 180, 8f, 40, new Color(0.35f, 0.55f, 0.75f), sprites["device_tablet"], sprites["device_tablet"], sprites["device_charged"]),
                CreateDevice("device_powerbank", "Power Bank", DeviceCategory.PowerBank, ConnectorType.MiniPower, 200, 7f, 35, new Color(0.2f, 0.2f, 0.25f), sprites["device_bank"], sprites["device_bank"], sprites["device_charged"]),
                CreateDevice("device_camera", "Digital Camera", DeviceCategory.DigitalCamera, ConnectorType.MiniPower, 140, 7.5f, 45, new Color(0.55f, 0.55f, 0.6f), sprites["device_camera"], sprites["device_camera"], sprites["device_charged"]),
                CreateDevice("device_handheld", "Handheld Game Device", DeviceCategory.HandheldGameDevice, ConnectorType.ProPower, 160, 9f, 50, new Color(0.75f, 0.35f, 0.45f), sprites["device_game"], sprites["device_game"], sprites["device_charged"]),
                CreateDevice("device_notebook", "Notebook Computer", DeviceCategory.NotebookComputer, ConnectorType.UniversalPower, 300, 12f, 80, new Color(0.45f, 0.45f, 0.5f), sprites["device_notebook"], sprites["device_notebook"], sprites["device_charged"])
            };
            return list;
        }

        private static DeviceData CreateDevice(
            string id, string name, DeviceCategory category, ConnectorType connector,
            int capacity, float duration, int price, Color tint, Sprite idle, Sprite charging, Sprite charged)
        {
            var path = $"{DataRoot}/Devices/{id}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<DeviceData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<DeviceData>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.Configure(id, name, category, connector, capacity, duration, price, tint);
            asset.AssignSprites(idle, charging, charged);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static List<CustomerData> BuildCustomers(Dictionary<string, Sprite> sprites)
        {
            return new List<CustomerData>
            {
                CreateCustomer("customer_regular", "Regular Guest", CustomerType.Regular, 48f, 1f, 1, new Color(0.95f, 0.75f, 0.55f), sprites["customer"]),
                CreateCustomer("customer_impatient", "Impatient Guest", CustomerType.Impatient, 30f, 1.15f, 1, new Color(0.95f, 0.55f, 0.45f), sprites["customer"]),
                CreateCustomer("customer_generous", "Generous Guest", CustomerType.Generous, 45f, 1.4f, 1, new Color(0.95f, 0.85f, 0.45f), sprites["customer"]),
                CreateCustomer("customer_patient", "Patient Guest", CustomerType.Patient, 70f, 1f, 1, new Color(0.7f, 0.85f, 0.95f), sprites["customer"]),
                CreateCustomer("customer_group", "Group Guest", CustomerType.Group, 55f, 1.1f, 2, new Color(0.75f, 0.65f, 0.95f), sprites["customer"]),
                CreateCustomer("customer_vip", "Premium Guest", CustomerType.Vip, 35f, 1.75f, 1, new Color(0.95f, 0.9f, 0.7f), sprites["customer"])
            };
        }

        private static CustomerData CreateCustomer(
            string id, string name, CustomerType type, float patience, float multiplier, int devices, Color tint, Sprite sprite)
        {
            var path = $"{DataRoot}/Customers/{id}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<CustomerData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<CustomerData>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.Configure(id, name, type, patience, multiplier, devices, tint);
            asset.AssignSprite(sprite);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static List<UpgradeData> BuildUpgrades()
        {
            return new List<UpgradeData>
            {
                CreateUpgrade("upgrade_capacity", "Charging Capacity", UpgradeType.ChargingCapacity, new[] { (100, 1f, "+1 port"), (250, 2f, "+2 ports") }),
                CreateUpgrade("upgrade_speed", "Charging Speed", UpgradeType.ChargingSpeed, new[] { (120, 1.15f, "Faster charge"), (280, 1.35f, "Much faster") }),
                CreateUpgrade("upgrade_counter", "Counter Size", UpgradeType.CounterSize, new[] { (100, 1f, "More counter space"), (220, 2f, "Wide counter") }),
                CreateUpgrade("upgrade_queue", "Queue Capacity", UpgradeType.QueueCapacity, new[] { (90, 1f, "+1 queue"), (200, 2f, "+2 queue") }),
                CreateUpgrade("upgrade_quality", "Service Quality", UpgradeType.ServiceQuality, new[] { (110, 1.15f, "More patience"), (260, 1.3f, "Calm service") }),
                CreateUpgrade("upgrade_pro", "Professional Equipment", UpgradeType.ProfessionalEquipment, new[] { (300, 1f, "Unlock premium kit") })
            };
        }

        private static UpgradeData CreateUpgrade(string id, string name, UpgradeType type, (int cost, float value, string desc)[] tiers)
        {
            var path = $"{DataRoot}/Upgrades/{id}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<UpgradeData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<UpgradeData>();
                AssetDatabase.CreateAsset(asset, path);
            }

            var list = new List<UpgradeTier>();
            for (var i = 0; i < tiers.Length; i++)
            {
                list.Add(new UpgradeTier { CostCredits = tiers[i].cost, Value = tiers[i].value, Description = tiers[i].desc });
            }

            asset.Configure(id, name, type, list);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static List<AchievementData> BuildAchievements()
        {
            return new List<AchievementData>
            {
                CreateAchievement("first_charge", "First Charge", "Complete first customer transaction.", 1, "customers_served"),
                CreateAchievement("getting_started", "Getting Started", "Earn 500 Credits.", 500, "lifetime_earnings"),
                CreateAchievement("busy_counter", "Busy Counter", "Serve 25 customers.", 25, "customers_served"),
                CreateAchievement("perfect_service", "Perfect Service", "Complete a level with zero mistakes.", 1, "perfect_levels"),
                CreateAchievement("fully_charged", "Fully Charged", "Successfully charge 100 devices.", 100, "devices_charged"),
                CreateAchievement("speed_service", "Speed Service", "Complete 10 successful transactions without losing a customer.", 10, "best_streak"),
                CreateAchievement("device_expert", "Device Expert", "Successfully charge every device category.", 7, "device_categories"),
                CreateAchievement("three_star_service", "Three Star Service", "Earn three stars on 5 levels.", 5, "three_star_levels"),
                CreateAchievement("charging_professional", "Charging Professional", "Complete all story levels.", 10, "story_levels"),
                CreateAchievement("charging_tycoon", "Charging Tycoon", "Earn 100,000 total Credits.", 100000, "lifetime_earnings")
            };
        }

        private static AchievementData CreateAchievement(string id, string name, string desc, int target, string key)
        {
            var path = $"{DataRoot}/Achievements/{id}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<AchievementData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<AchievementData>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.Configure(id, name, desc, target, key);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static List<LevelData> BuildLevels(List<DeviceData> devices, List<CustomerData> customers)
        {
            DeviceData D(string id) => devices.Find(x => x.DeviceId == id);
            CustomerData C(string id) => customers.Find(x => x.CustomerId == id);

            var levels = new List<LevelData>
            {
                CreateLevel("level_01", 1, "Local Fair", "Neighborhood Fair", "Learn the charging counter.", 120, 180, 240, 3, 9f, 2, 2, 1.35f, 1.1f, true, new Color(0.55f, 0.78f, 0.95f),
                    new[] { (D("device_basic_phone"), 1f) }, new[] { (C("customer_regular"), 1f), (C("customer_patient"), 0.4f) }),
                CreateLevel("level_02", 2, "Community Gathering", "Community Gathering", "Two smartphone styles arrive.", 200, 280, 360, 3, 7.5f, 3, 2, 1.2f, 1.05f, false, new Color(0.6f, 0.82f, 0.7f),
                    new[] { (D("device_basic_phone"), 1f), (D("device_advanced_phone"), 0.8f) }, new[] { (C("customer_regular"), 1f), (C("customer_generous"), 0.4f) }),
                CreateLevel("level_03", 3, "Food Festival", "Food Festival", "Faster flow and a new connector.", 280, 380, 500, 3, 6.5f, 3, 3, 1.1f, 1f, false, new Color(0.95f, 0.75f, 0.55f),
                    new[] { (D("device_basic_phone"), 1f), (D("device_advanced_phone"), 1f) }, new[] { (C("customer_regular"), 1f), (C("customer_impatient"), 0.5f) }),
                CreateLevel("level_04", 4, "College Festival", "College Festival", "Tablets join the counter.", 350, 480, 620, 3, 6f, 3, 3, 1.05f, 1f, false, new Color(0.65f, 0.7f, 0.95f),
                    new[] { (D("device_basic_phone"), 0.8f), (D("device_advanced_phone"), 0.8f), (D("device_tablet"), 0.7f) }, new[] { (C("customer_regular"), 1f), (C("customer_patient"), 0.5f), (C("customer_impatient"), 0.4f) }),
                CreateLevel("level_05", 5, "Sports Event", "Sports Event", "Power banks and longer queues.", 420, 580, 760, 3, 5.5f, 4, 3, 1f, 1f, false, new Color(0.45f, 0.7f, 0.55f),
                    new[] { (D("device_basic_phone"), 0.7f), (D("device_advanced_phone"), 0.7f), (D("device_tablet"), 0.5f), (D("device_powerbank"), 0.8f) }, new[] { (C("customer_regular"), 1f), (C("customer_impatient"), 0.6f), (C("customer_group"), 0.3f) }),
                CreateLevel("level_06", 6, "Music Festival", "Music Festival", "Cameras and shorter patience.", 500, 700, 900, 3, 5f, 4, 4, 0.9f, 1f, false, new Color(0.7f, 0.45f, 0.85f),
                    new[] { (D("device_advanced_phone"), 0.8f), (D("device_tablet"), 0.5f), (D("device_powerbank"), 0.5f), (D("device_camera"), 0.8f) }, new[] { (C("customer_impatient"), 0.8f), (C("customer_regular"), 0.7f), (C("customer_generous"), 0.4f) }),
                CreateLevel("level_07", 7, "Cultural Gathering", "Cultural Gathering", "Many device categories at once.", 600, 820, 1050, 3, 4.8f, 5, 4, 0.95f, 1f, false, new Color(0.9f, 0.7f, 0.5f),
                    new[] { (D("device_basic_phone"), 0.5f), (D("device_advanced_phone"), 0.6f), (D("device_tablet"), 0.6f), (D("device_powerbank"), 0.5f), (D("device_camera"), 0.5f) }, new[] { (C("customer_regular"), 1f), (C("customer_patient"), 0.5f), (C("customer_group"), 0.4f) }),
                CreateLevel("level_08", 8, "Gaming Convention", "Gaming Convention", "Handhelds and notebooks arrive.", 720, 980, 1250, 3, 4.5f, 5, 5, 0.9f, 0.95f, false, new Color(0.4f, 0.5f, 0.85f),
                    new[] { (D("device_advanced_phone"), 0.5f), (D("device_tablet"), 0.5f), (D("device_handheld"), 0.8f), (D("device_notebook"), 0.7f) }, new[] { (C("customer_regular"), 0.8f), (C("customer_impatient"), 0.6f), (C("customer_generous"), 0.4f) }),
                CreateLevel("level_09", 9, "Business Expo", "Business Expo", "Premium guests and high-value notebooks.", 850, 1150, 1450, 3, 4.2f, 5, 5, 0.85f, 0.95f, false, new Color(0.35f, 0.4f, 0.5f),
                    new[] { (D("device_advanced_phone"), 0.5f), (D("device_tablet"), 0.5f), (D("device_notebook"), 1f), (D("device_handheld"), 0.4f) }, new[] { (C("customer_vip"), 0.8f), (C("customer_generous"), 0.5f), (C("customer_impatient"), 0.5f) }),
                CreateLevel("level_10", 10, "Mega City Festival", "Mega City Festival", "All systems under pressure.", 1000, 1400, 1800, 3, 3.8f, 5, 6, 0.8f, 0.9f, false, new Color(0.25f, 0.3f, 0.45f),
                    new[] { (D("device_basic_phone"), 0.4f), (D("device_advanced_phone"), 0.5f), (D("device_tablet"), 0.5f), (D("device_powerbank"), 0.4f), (D("device_camera"), 0.4f), (D("device_handheld"), 0.5f), (D("device_notebook"), 0.6f) }, new[] { (C("customer_regular"), 0.7f), (C("customer_impatient"), 0.7f), (C("customer_generous"), 0.4f), (C("customer_patient"), 0.3f), (C("customer_group"), 0.4f), (C("customer_vip"), 0.5f) })
            };
            return levels;
        }

        private static LevelData CreateLevel(
            string id, int number, string name, string eventName, string description,
            int target, int two, int three, int mistakes, float spawn, int queue, int ports,
            float patience, float charge, bool tutorial, Color tint,
            (DeviceData device, float weight)[] devices,
            (CustomerData customer, float weight)[] customers)
        {
            var path = $"{DataRoot}/Levels/{id}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<LevelData>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.ConfigureBasics(id, number, name, eventName, description, target, two, three, mistakes, spawn, queue, ports, patience, charge, tutorial, tint);
            var deviceEntries = new List<WeightedDeviceEntry>();
            for (var i = 0; i < devices.Length; i++)
            {
                if (devices[i].device != null)
                {
                    deviceEntries.Add(new WeightedDeviceEntry { Device = devices[i].device, Weight = devices[i].weight });
                }
            }

            var customerEntries = new List<WeightedCustomerEntry>();
            for (var i = 0; i < customers.Length; i++)
            {
                if (customers[i].customer != null)
                {
                    customerEntries.Add(new WeightedCustomerEntry { Customer = customers[i].customer, Weight = customers[i].weight });
                }
            }

            asset.SetContent(deviceEntries, customerEntries);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static List<ChallengeData> BuildChallenges(List<LevelData> levels)
        {
            LevelData L(int n) => levels.Find(x => x.LevelNumber == n);
            return new List<ChallengeData>
            {
                CreateChallenge("challenge_earn", "Credit Rush", "Earn a high credit total.", ChallengeObjectiveType.EarnCredits, L(3), 450, 0f),
                CreateChallenge("challenge_serve", "Service Sprint", "Serve many guests quickly.", ChallengeObjectiveType.ServeCustomers, L(4), 12, 0f),
                CreateChallenge("challenge_perfect", "Flawless Counter", "Reach the target with zero mistakes.", ChallengeObjectiveType.ZeroMistakes, L(5), 1, 0f),
                CreateChallenge("challenge_timed", "Timed Boost", "Hit the target before time runs out.", ChallengeObjectiveType.FinishWithinTime, L(6), 500, 120f)
            };
        }

        private static ChallengeData CreateChallenge(
            string id, string name, string desc, ChallengeObjectiveType type, LevelData level, int target, float time)
        {
            var path = $"{DataRoot}/Challenges/{id}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<ChallengeData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ChallengeData>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.Configure(id, name, desc, type, level, target, time);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static GameCatalog BuildCatalog(
            List<DeviceData> devices,
            List<CustomerData> customers,
            List<LevelData> levels,
            List<UpgradeData> upgrades,
            List<AchievementData> achievements,
            List<ChallengeData> challenges)
        {
            var path = $"{ResourcesRoot}/GameCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(path);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<GameCatalog>();
                AssetDatabase.CreateAsset(catalog, path);
            }

            catalog.AssignAll(devices, customers, levels, upgrades, achievements, challenges);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private sealed class PrefabSet
        {
            public CustomerInstance Customer;
            public DeviceInstance Device;
            public ChargingPort Port;
        }

        private static PrefabSet BuildPrefabs(Dictionary<string, Sprite> sprites)
        {
            var set = new PrefabSet
            {
                Customer = BuildCustomerPrefab(sprites["customer"]),
                Device = BuildDevicePrefab(sprites["device_phone"]),
                Port = BuildPortPrefab(sprites["port"])
            };
            return set;
        }

        private static CustomerInstance BuildCustomerPrefab(Sprite sprite)
        {
            var path = $"{PrefabRoot}/Characters/Customer.prefab";
            var go = new GameObject("Customer", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(CustomerInstance));
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 5;
            var col = go.GetComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 1.5f);
            var patience = new GameObject("Patience", typeof(SpriteRenderer));
            patience.transform.SetParent(go.transform, false);
            patience.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            patience.transform.localScale = new Vector3(1f, 0.12f, 1f);
            var mood = new GameObject("Mood", typeof(SpriteRenderer));
            mood.transform.SetParent(go.transform, false);
            mood.transform.localPosition = new Vector3(0.45f, 0.9f, 0f);
            mood.transform.localScale = Vector3.one * 0.25f;
            var anchor = new GameObject("DeviceAnchor");
            anchor.transform.SetParent(go.transform, false);
            anchor.transform.localPosition = new Vector3(0.55f, 0.1f, 0f);
            var customer = go.GetComponent<CustomerInstance>();
            var so = new SerializedObject(customer);
            so.FindProperty("spriteRenderer").objectReferenceValue = sr;
            so.FindProperty("patienceFill").objectReferenceValue = patience.transform;
            so.FindProperty("deviceAnchor").objectReferenceValue = anchor.transform;
            so.FindProperty("moodBadge").objectReferenceValue = mood.GetComponent<SpriteRenderer>();
            so.ApplyModifiedPropertiesWithoutUndo();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<CustomerInstance>();
        }

        private static DeviceInstance BuildDevicePrefab(Sprite sprite)
        {
            var path = $"{PrefabRoot}/Devices/Device.prefab";
            var go = new GameObject("Device", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(DeviceInstance));
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 8;
            var col = go.GetComponent<BoxCollider2D>();
            col.size = new Vector2(0.7f, 1f);
            var progress = new GameObject("Progress");
            progress.transform.SetParent(go.transform, false);
            progress.transform.localPosition = new Vector3(0f, -0.7f, 0f);
            progress.transform.localScale = new Vector3(0f, 0.1f, 1f);
            var progressSr = progress.AddComponent<SpriteRenderer>();
            progressSr.color = new Color(0.3f, 0.9f, 0.4f);
            progressSr.sortingOrder = 9;
            var badge = new GameObject("ConnectorBadge", typeof(SpriteRenderer));
            badge.transform.SetParent(go.transform, false);
            badge.transform.localPosition = new Vector3(0.35f, 0.45f, 0f);
            badge.transform.localScale = Vector3.one * 0.2f;
            badge.GetComponent<SpriteRenderer>().sortingOrder = 10;
            var device = go.GetComponent<DeviceInstance>();
            var so = new SerializedObject(device);
            so.FindProperty("spriteRenderer").objectReferenceValue = sr;
            so.FindProperty("progressFill").objectReferenceValue = progress.transform;
            so.FindProperty("connectorBadge").objectReferenceValue = badge.GetComponent<SpriteRenderer>();
            so.ApplyModifiedPropertiesWithoutUndo();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<DeviceInstance>();
        }

        private static ChargingPort BuildPortPrefab(Sprite sprite)
        {
            var path = $"{PrefabRoot}/Stations/ChargingPort.prefab";
            var go = new GameObject("ChargingPort", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(ChargingPort));
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 3;
            var col = go.GetComponent<BoxCollider2D>();
            col.size = new Vector2(0.9f, 0.9f);
            var highlight = new GameObject("Highlight", typeof(SpriteRenderer));
            highlight.transform.SetParent(go.transform, false);
            highlight.transform.localScale = Vector3.one * 1.2f;
            var hsr = highlight.GetComponent<SpriteRenderer>();
            hsr.sprite = sprite;
            hsr.color = new Color(1f, 1f, 1f, 0.25f);
            hsr.enabled = false;
            hsr.sortingOrder = 2;
            var socket = new GameObject("Socket");
            socket.transform.SetParent(go.transform, false);
            socket.transform.localPosition = Vector3.up * 0.35f;
            var badge = new GameObject("ConnectorBadge", typeof(SpriteRenderer));
            badge.transform.SetParent(go.transform, false);
            badge.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            badge.transform.localScale = Vector3.one * 0.42f;
            var badgeSr = badge.GetComponent<SpriteRenderer>();
            badgeSr.sortingOrder = 6;
            var port = go.GetComponent<ChargingPort>();
            var so = new SerializedObject(port);
            so.FindProperty("portRenderer").objectReferenceValue = sr;
            so.FindProperty("highlightRenderer").objectReferenceValue = hsr;
            so.FindProperty("connectorBadge").objectReferenceValue = badgeSr;
            so.FindProperty("socketAnchor").objectReferenceValue = socket.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<ChargingPort>();
        }

        private static void BuildScenes(GameCatalog catalog, PrefabSet prefabs, Dictionary<string, Sprite> sprites)
        {
            BuildBootScene(catalog);
            BuildMenuScene(catalog);
            BuildLevelSelectScene();
            BuildAchievementsScene();
            BuildGameplayScene(catalog, prefabs, sprites);
            ConfigureBuildSettings();
        }

        private static void BuildBootScene(GameCatalog catalog)
        {
            if (ChargeRushAuthoredScenes.TryPreserve(ChargeRushAuthoredScenes.Boot, "rebuild"))
            {
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = CreateCamera();
            var bootstrap = new GameObject("Bootstrap");
            bootstrap.AddComponent<GameBootstrap>();
            bootstrap.AddComponent<SceneLoader>();
            bootstrap.AddComponent<SaveManager>();
            bootstrap.AddComponent<InputManager>();
            var so = new SerializedObject(bootstrap.GetComponent<GameBootstrap>());
            so.FindProperty("catalog").objectReferenceValue = catalog;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, ChargeRushAuthoredScenes.Boot);
        }

        private static void BuildMenuScene(GameCatalog catalog)
        {
            if (ChargeRushAuthoredScenes.TryPreserve(ChargeRushAuthoredScenes.MainMenu, "rebuild"))
            {
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();
            CreateEventSystem();
            var canvas = CreateCanvas("MainMenuCanvas");
            var safe = CreateSafeArea(canvas.transform);
            var title = CreateTMP(safe, "Title", "ChargeRush", 64, new Vector2(0f, 220f), UiTextRole.Heading);
            var career = CreateTMP(safe, "Career", "Unemployed", 28, new Vector2(0f, 150f), UiTextRole.Body);
            var credits = CreateTMP(safe, "Credits", "0 CR", 28, new Vector2(0f, 110f), UiTextRole.Body);
            var play = CreateButton(safe, "Play", "Play", new Vector2(0f, 40f));
            var levels = CreateButton(safe, "LevelSelect", "Level Select", new Vector2(0f, -20f));
            var challenge = CreateButton(safe, "Challenge", "Challenge Mode", new Vector2(0f, -80f));
            var endless = CreateButton(safe, "Endless", "Endless Mode", new Vector2(0f, -140f));
            var achievements = CreateButton(safe, "Achievements", "Achievements", new Vector2(-160f, -210f));
            var upgrades = CreateButton(safe, "Upgrades", "Upgrades", new Vector2(160f, -210f));
            var upgradesPanel = CreatePanel(safe, "UpgradesPanel", new Vector2(0f, 0f), new Vector2(700f, 420f));
            upgradesPanel.SetActive(false);
            CreateTMP(upgradesPanel.transform, "UpgradesTitle", "Upgrades", 36, new Vector2(0f, 170f), UiTextRole.Heading);
            var upgradesContent = CreateListContent(upgradesPanel.transform, new Vector2(640f, 280f));
            upgradesContent.anchoredPosition = new Vector2(0f, 40f);
            var closeUpgrades = CreateButton(upgradesPanel.transform, "CloseUpgrades", "Close", new Vector2(0f, -170f));
            var controller = canvas.AddComponent<MainMenuController>();
            var so = new SerializedObject(controller);
            so.FindProperty("careerText").objectReferenceValue = career;
            so.FindProperty("creditsText").objectReferenceValue = credits;
            so.FindProperty("playButton").objectReferenceValue = play;
            so.FindProperty("levelSelectButton").objectReferenceValue = levels;
            so.FindProperty("challengeButton").objectReferenceValue = challenge;
            so.FindProperty("endlessButton").objectReferenceValue = endless;
            so.FindProperty("achievementsButton").objectReferenceValue = achievements;
            so.FindProperty("upgradesButton").objectReferenceValue = upgrades;
            so.FindProperty("upgradesPanel").objectReferenceValue = upgradesPanel;
            so.FindProperty("upgradesContent").objectReferenceValue = upgradesContent;
            so.FindProperty("closeUpgradesButton").objectReferenceValue = closeUpgrades;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, ChargeRushAuthoredScenes.MainMenu);
        }

        [MenuItem("ChargeRush/Force Rebuild All Scenes")]
        public static void ForceRebuildAllScenes()
        {
            if (!EditorUtility.DisplayDialog(
                    "Force rebuild all scenes?",
                    "This will overwrite every scene under Assets/Scenes/ with generated placeholder layouts.\n\nAll hand-authored UI will be lost.",
                    "Overwrite All",
                    "Cancel"))
            {
                return;
            }

            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>($"{ResourcesRoot}/GameCatalog.asset");
            if (catalog == null)
            {
                Debug.LogError("ChargeRush: Missing GameCatalog; cannot force-rebuild scenes.");
                return;
            }

            ChargeRushAuthoredScenes.DeleteAllForForceRebuild();
            ChargeRushFontBuilder.EnsureFontAssets();

            var sprites = BuildSprites();
            var prefabs = BuildPrefabs(sprites);
            BuildBootScene(catalog);
            BuildMenuScene(catalog);
            BuildLevelSelectScene();
            BuildAchievementsScene();
            BuildGameplayScene(catalog, prefabs, sprites);
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("ChargeRush: All scenes force-rebuilt.");
        }

        private static void BuildLevelSelectScene()
        {
            if (ChargeRushAuthoredScenes.TryPreserve(ChargeRushAuthoredScenes.LevelSelect, "rebuild"))
            {
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();
            CreateEventSystem();
            var canvas = CreateCanvas("LevelSelectCanvas");
            var safe = CreateSafeArea(canvas.transform);
            CreateTMP(safe, "Title", "Level Select", 48, new Vector2(0f, 220f), UiTextRole.Heading);
            var content = CreateListContent(safe, new Vector2(900f, 380f));
            var back = CreateButton(safe, "Back", "Back", new Vector2(0f, -230f));
            var controller = canvas.AddComponent<LevelSelectController>();
            var so = new SerializedObject(controller);
            so.FindProperty("contentRoot").objectReferenceValue = content;
            so.FindProperty("backButton").objectReferenceValue = back;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, ChargeRushAuthoredScenes.LevelSelect);
        }

        private static void BuildAchievementsScene()
        {
            if (ChargeRushAuthoredScenes.TryPreserve(ChargeRushAuthoredScenes.Achievements, "rebuild"))
            {
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();
            CreateEventSystem();
            var canvas = CreateCanvas("AchievementsCanvas");
            var safe = CreateSafeArea(canvas.transform);
            CreateTMP(safe, "Title", "Achievements", 48, new Vector2(0f, 220f), UiTextRole.Heading);
            var content = CreateListContent(safe, new Vector2(900f, 380f));
            var back = CreateButton(safe, "Back", "Back", new Vector2(0f, -230f));
            var controller = canvas.AddComponent<AchievementsScreenController>();
            var so = new SerializedObject(controller);
            so.FindProperty("contentRoot").objectReferenceValue = content;
            so.FindProperty("backButton").objectReferenceValue = back;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, ChargeRushAuthoredScenes.Achievements);
        }

        private static RectTransform CreateListContent(Transform parent, Vector2 size)
        {
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(parent, false);
            var rt = content.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
            return rt;
        }

        private static void BuildGameplayScene(GameCatalog catalog, PrefabSet prefabs, Dictionary<string, Sprite> sprites)
        {
            if (ChargeRushAuthoredScenes.TryPreserve(ChargeRushAuthoredScenes.Gameplay, "rebuild"))
            {
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = CreateCamera();
            CreateEventSystem();

            var bg = new GameObject("Background", typeof(SpriteRenderer));
            bg.GetComponent<SpriteRenderer>().sprite = sprites["bg"];
            bg.GetComponent<SpriteRenderer>().color = new Color(0.55f, 0.78f, 0.95f);
            bg.transform.localScale = new Vector3(40f, 24f, 1f);
            bg.GetComponent<SpriteRenderer>().sortingOrder = -10;

            var counter = new GameObject("Counter", typeof(SpriteRenderer));
            counter.GetComponent<SpriteRenderer>().sprite = sprites["counter"];
            counter.transform.position = new Vector3(0f, -2.2f, 0f);
            counter.transform.localScale = new Vector3(5f, 1f, 1f);
            counter.GetComponent<SpriteRenderer>().sortingOrder = 1;

            var queueGo = new GameObject("CustomerQueue", typeof(CustomerQueue));
            queueGo.transform.position = new Vector3(0f, 0.5f, 0f);
            var stationGo = new GameObject("ChargingStation", typeof(ChargingStation));
            stationGo.transform.position = new Vector3(0f, -1.4f, 0f);

            var systems = new GameObject("Systems");
            var economy = systems.AddComponent<EconomyManager>();
            var upgrades = systems.AddComponent<UpgradeManager>();
            var achievements = systems.AddComponent<AchievementManager>();
            var drag = systems.AddComponent<DragDropController>();
            var tutorial = systems.AddComponent<TutorialDirector>();
            var session = systems.AddComponent<LevelSession>();

            var arrow = new GameObject("TutorialArrow", typeof(SpriteRenderer));
            arrow.GetComponent<SpriteRenderer>().sprite = sprites["port"];
            arrow.GetComponent<SpriteRenderer>().color = new Color(1f, 0.9f, 0.2f, 0.8f);
            arrow.SetActive(false);

            var canvas = CreateCanvas("GameplayCanvas");
            var safe = CreateSafeArea(canvas.transform);
            var earnings = CreateTMP(safe, "Earnings", "Credits: 0", 28, new Vector2(-420f, 200f), UiTextRole.Body);
            earnings.alignment = TextAlignmentOptions.Left;
            var target = CreateTMP(safe, "Target", "Target: 0", 28, new Vector2(-420f, 160f), UiTextRole.Body);
            target.alignment = TextAlignmentOptions.Left;
            var mistakes = CreateTMP(safe, "Mistakes", "Mistakes: 0 / 3", 28, new Vector2(320f, 200f), UiTextRole.Body);
            mistakes.alignment = TextAlignmentOptions.Left;
            var active = CreateTMP(safe, "Active", "Charging: 0", 24, new Vector2(320f, 160f), UiTextRole.Body);
            active.alignment = TextAlignmentOptions.Left;
            var tutorialText = CreateTMP(safe, "Tutorial", "", 32, new Vector2(0f, 120f), UiTextRole.Body);
            var payment = CreateTMP(safe, "PaymentPopup", "", 40, new Vector2(0f, 40f), UiTextRole.Heading);
            payment.gameObject.SetActive(false);
            var pauseBtn = CreateButton(safe, "Pause", "Pause", new Vector2(520f, 200f));

            var pausePanel = CreatePanel(safe, "PausePanel", Vector2.zero, new Vector2(480f, 280f));
            pausePanel.SetActive(false);
            CreateTMP(pausePanel.transform, "PauseTitle", "Paused", 40, new Vector2(0f, 80f), UiTextRole.Heading);
            var resume = CreateButton(pausePanel.transform, "Resume", "Resume", new Vector2(0f, 0f));
            var pauseLevels = CreateButton(pausePanel.transform, "PauseLevels", "Level Select", new Vector2(0f, -70f));

            var completePanel = CreatePanel(safe, "CompletePanel", Vector2.zero, new Vector2(520f, 340f));
            completePanel.SetActive(false);
            CreateTMP(completePanel.transform, "CompleteTitle", "Level Complete", 40, new Vector2(0f, 120f), UiTextRole.Heading);
            var completeStats = CreateTMP(completePanel.transform, "CompleteStats", "", 28, new Vector2(0f, 40f), UiTextRole.Body);
            var stars = CreateTMP(completePanel.transform, "Stars", "Stars: 0 / 3", 30, new Vector2(0f, -20f), UiTextRole.Heading);
            var cont = CreateButton(completePanel.transform, "Continue", "Continue", new Vector2(-110f, -110f));
            var replay = CreateButton(completePanel.transform, "Replay", "Replay", new Vector2(110f, -110f));

            var failPanel = CreatePanel(safe, "FailPanel", Vector2.zero, new Vector2(520f, 300f));
            failPanel.SetActive(false);
            CreateTMP(failPanel.transform, "FailTitle", "Level Failed", 40, new Vector2(0f, 100f), UiTextRole.Heading);
            var failReason = CreateTMP(failPanel.transform, "FailReason", "", 28, new Vector2(0f, 30f), UiTextRole.Body);
            var failRetry = CreateButton(failPanel.transform, "FailRetry", "Retry", new Vector2(-110f, -90f));
            var failLevels = CreateButton(failPanel.transform, "FailLevels", "Level Select", new Vector2(110f, -90f));

            var hud = canvas.AddComponent<GameplayHud>();
            var hudSo = new SerializedObject(hud);
            hudSo.FindProperty("earningsText").objectReferenceValue = earnings;
            hudSo.FindProperty("targetText").objectReferenceValue = target;
            hudSo.FindProperty("mistakesText").objectReferenceValue = mistakes;
            hudSo.FindProperty("activeChargesText").objectReferenceValue = active;
            hudSo.FindProperty("tutorialText").objectReferenceValue = tutorialText;
            hudSo.FindProperty("paymentPopupText").objectReferenceValue = payment;
            hudSo.FindProperty("pauseButton").objectReferenceValue = pauseBtn;
            hudSo.FindProperty("pausePanel").objectReferenceValue = pausePanel;
            hudSo.FindProperty("completePanel").objectReferenceValue = completePanel;
            hudSo.FindProperty("failPanel").objectReferenceValue = failPanel;
            hudSo.FindProperty("completeStatsText").objectReferenceValue = completeStats;
            hudSo.FindProperty("failReasonText").objectReferenceValue = failReason;
            hudSo.FindProperty("starsText").objectReferenceValue = stars;
            hudSo.FindProperty("resumeButton").objectReferenceValue = resume;
            hudSo.FindProperty("retryButton").objectReferenceValue = replay;
            hudSo.FindProperty("failRetryButton").objectReferenceValue = failRetry;
            hudSo.FindProperty("levelSelectButton").objectReferenceValue = pauseLevels;
            hudSo.FindProperty("failLevelSelectButton").objectReferenceValue = failLevels;
            hudSo.FindProperty("continueButton").objectReferenceValue = cont;
            hudSo.ApplyModifiedPropertiesWithoutUndo();

            var sessionSo = new SerializedObject(session);
            sessionSo.FindProperty("catalog").objectReferenceValue = catalog;
            sessionSo.FindProperty("customerPrefab").objectReferenceValue = prefabs.Customer;
            sessionSo.FindProperty("devicePrefab").objectReferenceValue = prefabs.Device;
            sessionSo.FindProperty("portPrefab").objectReferenceValue = prefabs.Port;
            sessionSo.FindProperty("customerQueue").objectReferenceValue = queueGo.GetComponent<CustomerQueue>();
            sessionSo.FindProperty("chargingStation").objectReferenceValue = stationGo.GetComponent<ChargingStation>();
            sessionSo.FindProperty("economy").objectReferenceValue = economy;
            sessionSo.FindProperty("upgrades").objectReferenceValue = upgrades;
            sessionSo.FindProperty("achievements").objectReferenceValue = achievements;
            sessionSo.FindProperty("dragDrop").objectReferenceValue = drag;
            sessionSo.FindProperty("tutorial").objectReferenceValue = tutorial;
            sessionSo.FindProperty("backgroundRenderer").objectReferenceValue = bg.GetComponent<SpriteRenderer>();
            sessionSo.FindProperty("counterRoot").objectReferenceValue = counter.transform;
            sessionSo.ApplyModifiedPropertiesWithoutUndo();

            var tutorialSo = new SerializedObject(tutorial);
            tutorialSo.FindProperty("highlightArrow").objectReferenceValue = arrow;
            tutorialSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ChargeRushAuthoredScenes.Gameplay);
        }

        private static void ConfigureBuildSettings()
        {
            var scenes = ChargeRushAuthoredScenes.All;

            var list = new EditorBuildSettingsScene[scenes.Length];
            for (var i = 0; i < scenes.Length; i++)
            {
                list[i] = new EditorBuildSettingsScene(scenes[i], true);
            }

            EditorBuildSettings.scenes = list;
        }

        private static void ConfigureProjectSettings()
        {
            PlayerSettings.companyName = "ChargeRush Studio";
            PlayerSettings.productName = "ChargeRush";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS, "com.chargerush.studio.chargerush");
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone, "com.chargerush.studio.chargerush");
            PlayerSettings.iOS.targetOSVersionString = "15.0";
        }

        private static Camera CreateCamera()
        {
            var camGo = new GameObject("Main Camera", typeof(Camera));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.backgroundColor = new Color(0.15f, 0.18f, 0.22f);
            cam.transform.position = new Vector3(0f, 0f, -10f);
            return cam;
        }

        private static void CreateEventSystem()
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private static GameObject CreateCanvas(string name)
        {
            var canvasGo = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvasGo;
        }

        private static Transform CreateSafeArea(Transform parent)
        {
            var safe = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaFitter));
            safe.transform.SetParent(parent, false);
            var rt = safe.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return safe.transform;
        }

        private static TextMeshProUGUI CreateTMP(Transform parent, string name, string text, float size, Vector2 pos, UiTextRole role = UiTextRole.Body)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(700f, 80f);
            rt.anchoredPosition = pos;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            UiFonts.Apply(tmp, role);
            return tmp;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(260f, 56f);
            rt.anchoredPosition = pos;
            go.GetComponent<Image>().color = new Color(0.18f, 0.45f, 0.62f);
            var text = CreateTMP(go.transform, "Label", label, 26, Vector2.zero, UiTextRole.Heading);
            text.rectTransform.sizeDelta = new Vector2(240f, 50f);
            return go.GetComponent<Button>();
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            go.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.92f);
            return go;
        }
    }
}

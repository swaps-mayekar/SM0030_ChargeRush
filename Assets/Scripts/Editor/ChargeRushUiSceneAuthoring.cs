using System.Collections.Generic;
using ChargeRush.Data;
using ChargeRush.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ChargeRush.Editor
{
    /// <summary>Materializes runtime UI into editable scene objects.</summary>
    public static class ChargeRushUiSceneAuthoring
    {
        private const string CatalogPath = "Assets/Resources/GameData/GameCatalog.asset";
        private const string PanelPath = "Assets/Resources/Art/UI/result_panel_opaque.png";
        private static readonly Color DarkBrown = new Color(0.2f, 0.1f, 0.045f, 1f);

        [MenuItem("ChargeRush/Author Achievements and Level Select UI")]
        public static void AuthorUiScenes()
        {
            EditorSceneManager.SaveOpenScenes();
            var restorePath = SceneManager.GetActiveScene().path;
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogError($"ChargeRush: Missing catalog at {CatalogPath}.");
                return;
            }

            AuthorAchievements(catalog);
            AuthorLevelSelect(catalog);

            if (!string.IsNullOrEmpty(restorePath))
            {
                EditorSceneManager.OpenScene(restorePath, OpenSceneMode.Single);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("ChargeRush: Achievements and Level Select are now authored, editable scene UI.");
        }

        private static void AuthorAchievements(GameCatalog catalog)
        {
            var scene = EditorSceneManager.OpenScene(ChargeRushAuthoredScenes.Achievements, OpenSceneMode.Single);
            var controller = Object.FindFirstObjectByType<AchievementsScreenController>();
            var safe = controller != null ? controller.transform.Find("SafeArea") : null;
            if (controller == null || safe == null)
            {
                Debug.LogError("ChargeRush: Achievements scene is missing its controller or SafeArea.");
                return;
            }

            var content = PreparePanel(safe);
            var progress = CreateLabel(content, "Progress", "0 / 10  UNLOCKED", 19f, UiTextRole.Body);
            progress.color = DarkBrown;
            progress.fontStyle = FontStyles.Bold;
            SetRect(progress.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(360f, 30f), new Vector2(0.5f, 1f));

            var grid = CreateGrid(content, "AchievementGrid");
            var cards = new List<AchievementCardView>();
            for (var i = 0; i < catalog.Achievements.Count; i++)
            {
                cards.Add(CreateAchievementCard(grid, catalog.Achievements[i]));
            }

            var serialized = new SerializedObject(controller);
            serialized.FindProperty("backButton").objectReferenceValue = safe.Find("Back")?.GetComponent<Button>();
            serialized.FindProperty("progressLabel").objectReferenceValue = progress;
            AssignArray(serialized.FindProperty("cards"), cards);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void AuthorLevelSelect(GameCatalog catalog)
        {
            var scene = EditorSceneManager.OpenScene(ChargeRushAuthoredScenes.LevelSelect, OpenSceneMode.Single);
            var controller = Object.FindFirstObjectByType<LevelSelectController>();
            var safe = controller != null ? controller.transform.Find("SafeArea") : null;
            if (controller == null || safe == null)
            {
                Debug.LogError("ChargeRush: Level Select scene is missing its controller or SafeArea.");
                return;
            }

            var content = PreparePanel(safe);
            var subtitle = CreateLabel(content, "Subtitle", "Choose your next event", 19f, UiTextRole.Body);
            subtitle.color = DarkBrown;
            subtitle.fontStyle = FontStyles.Bold;
            SetRect(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(360f, 30f), new Vector2(0.5f, 1f));

            var grid = CreateGrid(content, "LevelGrid");
            var cards = new List<LevelSelectCardView>();
            for (var i = 0; i < catalog.StoryLevels.Count; i++)
            {
                cards.Add(CreateLevelCard(grid, catalog.StoryLevels[i]));
            }

            var serialized = new SerializedObject(controller);
            serialized.FindProperty("backButton").objectReferenceValue = safe.Find("Back")?.GetComponent<Button>();
            AssignArray(serialized.FindProperty("cards"), cards);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static Transform PreparePanel(Transform safe)
        {
            var content = safe.Find("Content");
            if (content == null)
            {
                var contentObject = new GameObject("Content", typeof(RectTransform));
                contentObject.transform.SetParent(safe, false);
                content = contentObject.transform;
            }

            for (var i = content.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(content.GetChild(i).gameObject);
            }

            var rect = content.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -4f);
            rect.sizeDelta = new Vector2(960f, 430f);

            var image = GetOrAdd<Image>(content.gameObject);
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PanelPath);
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            image.raycastTarget = true;

            RemoveIfPresent<ScrollRect>(content.gameObject);
            RemoveIfPresent<RectMask2D>(content.gameObject);
            return content;
        }

        private static Transform CreateGrid(Transform parent, string name)
        {
            var gridObject = new GameObject(name, typeof(RectTransform), typeof(GridLayoutGroup));
            gridObject.transform.SetParent(parent, false);
            var rect = gridObject.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -22f), new Vector2(876f, 308f));

            var layout = gridObject.GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(168.8f, 149f);
            layout.spacing = new Vector2(8f, 10f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 5;
            layout.startAxis = GridLayoutGroup.Axis.Horizontal;
            layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            layout.childAlignment = TextAnchor.UpperCenter;
            return gridObject.transform;
        }

        private static AchievementCardView CreateAchievementCard(Transform parent, AchievementData data)
        {
            var cardObject = CreateCardObject(parent, data.AchievementId);
            var background = cardObject.GetComponent<Image>();

            var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(cardObject.transform, false);
            var icon = iconObject.GetComponent<Image>();
            icon.sprite = LoadFirstSprite($"Assets/Resources/Art/Achievements/{data.AchievementId}.png");
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            SetRect(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -5f), new Vector2(60f, 60f), new Vector2(0.5f, 1f));

            var title = CreateLabel(cardObject.transform, "Title", data.DisplayName, 14f, UiTextRole.Heading);
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.enableAutoSizing = true;
            title.fontSizeMin = 11f;
            title.fontSizeMax = 14f;
            title.textWrappingMode = TextWrappingModes.Normal;
            title.overflowMode = TextOverflowModes.Ellipsis;
            SetStretchXFromTop(title.rectTransform, 5f, 5f, 64f, 30f);

            var objective = CreateLabel(cardObject.transform, "Objective", CompactObjective(data), 10.5f, UiTextRole.Body);
            objective.alignment = TextAlignmentOptions.Center;
            objective.textWrappingMode = TextWrappingModes.Normal;
            objective.overflowMode = TextOverflowModes.Ellipsis;
            SetStretchXFromTop(objective.rectTransform, 5f, 5f, 94f, 27f);

            var status = CreateLabel(cardObject.transform, "Status", "LOCKED", 10f, UiTextRole.Body);
            status.fontStyle = FontStyles.Bold;
            status.alignment = TextAlignmentOptions.Center;
            SetRect(status.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(120f, 15f), new Vector2(0.5f, 0f));

            var view = cardObject.AddComponent<AchievementCardView>();
            view.Assign(data.AchievementId, background, icon, title, objective, status);
            view.SetUnlocked(false);
            return view;
        }

        private static LevelSelectCardView CreateLevelCard(Transform parent, LevelData level)
        {
            var cardObject = CreateCardObject(parent, level.LevelId);
            var background = cardObject.GetComponent<Image>();
            var button = cardObject.AddComponent<Button>();
            button.targetGraphic = background;

            var thumbnailObject = new GameObject("Thumbnail", typeof(RectTransform), typeof(Image));
            thumbnailObject.transform.SetParent(cardObject.transform, false);
            var thumbnail = thumbnailObject.GetComponent<Image>();
            thumbnail.sprite = level.BackgroundSprite;
            thumbnail.color = level.BackgroundSprite != null ? Color.white : level.BackgroundTint;
            thumbnail.raycastTarget = false;
            SetStretchXFromTop(thumbnail.rectTransform, 6f, 6f, 6f, 54f);

            var number = CreateLabel(cardObject.transform, "LevelNumber", level.LevelNumber.ToString(), 19f, UiTextRole.Heading);
            number.fontStyle = FontStyles.Bold;
            number.color = Color.white;
            number.alignment = TextAlignmentOptions.Center;
            SetRect(number.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -8f), new Vector2(28f, 28f), new Vector2(0f, 1f));

            var title = CreateLabel(cardObject.transform, "Title", level.LevelName, 14f, UiTextRole.Heading);
            title.fontStyle = FontStyles.Bold;
            title.color = DarkBrown;
            title.alignment = TextAlignmentOptions.Center;
            title.enableAutoSizing = true;
            title.fontSizeMin = 11f;
            title.fontSizeMax = 14f;
            SetStretchXFromTop(title.rectTransform, 5f, 5f, 63f, 24f);

            var target = CreateLabel(cardObject.transform, "Target", $"Target: {level.TargetEarnings} Credits", 10.5f, UiTextRole.Body);
            target.color = new Color(0.28f, 0.16f, 0.08f, 1f);
            target.alignment = TextAlignmentOptions.Center;
            SetStretchXFromTop(target.rectTransform, 4f, 4f, 88f, 22f);

            var stars = CreateLabel(cardObject.transform, "Stars", "0 / 3 STARS", 10f, UiTextRole.Body);
            stars.fontStyle = FontStyles.Bold;
            stars.color = new Color(0.1f, 0.42f, 0.2f, 1f);
            stars.alignment = TextAlignmentOptions.Center;
            SetRect(stars.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 5f), new Vector2(130f, 18f), new Vector2(0.5f, 0f));

            var overlayObject = new GameObject("LockedOverlay", typeof(RectTransform), typeof(Image));
            overlayObject.transform.SetParent(cardObject.transform, false);
            var overlayRect = overlayObject.GetComponent<RectTransform>();
            StretchFull(overlayRect);
            overlayObject.GetComponent<Image>().color = new Color(0.08f, 0.07f, 0.06f, 0.72f);
            var locked = CreateLabel(overlayObject.transform, "Label", "LOCKED", 18f, UiTextRole.Heading);
            locked.fontStyle = FontStyles.Bold;
            locked.color = new Color(1f, 0.68f, 0.25f, 1f);
            locked.alignment = TextAlignmentOptions.Center;
            StretchFull(locked.rectTransform);
            overlayObject.SetActive(false);

            var view = cardObject.AddComponent<LevelSelectCardView>();
            view.Assign(level.LevelId, button, background, overlayObject, stars);
            return view;
        }

        private static GameObject CreateCardObject(Transform parent, string name)
        {
            var card = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline));
            card.transform.SetParent(parent, false);
            var image = card.GetComponent<Image>();
            image.color = new Color(1f, 0.87f, 0.58f, 0.98f);
            var outline = card.GetComponent<Outline>();
            outline.effectColor = new Color(0.5f, 0.25f, 0.07f, 0.75f);
            outline.effectDistance = new Vector2(2f, -2f);
            return card;
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string name, string text, float size, UiTextRole role)
        {
            var labelObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.raycastTarget = false;
            UiFonts.Apply(label, role);
            return label;
        }

        private static Sprite LoadFirstSprite(string path)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (var i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                {
                    return sprite;
                }
            }

            return null;
        }

        private static string CompactObjective(AchievementData achievement)
        {
            switch (achievement.AchievementId)
            {
                case "first_charge": return "Serve your first customer";
                case "getting_started": return "Earn 500 Credits";
                case "busy_counter": return "Serve 25 customers";
                case "perfect_service": return "Finish with 0 mistakes";
                case "fully_charged": return "Charge 100 devices";
                case "speed_service": return "Reach a 10-service streak";
                case "device_expert": return "Charge all 7 device types";
                case "three_star_service": return "3 stars on 5 levels";
                case "charging_professional": return "Complete all story levels";
                case "charging_tycoon": return "Earn 100,000 Credits";
                default: return achievement.Description;
            }
        }

        private static void AssignArray<T>(SerializedProperty property, List<T> values) where T : Object
        {
            property.arraySize = values.Count;
            for (var i = 0; i < values.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static void RemoveIfPresent<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            if (component != null)
            {
                Object.DestroyImmediate(component);
            }
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position,
            Vector2 size,
            Vector2? pivot = null)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetStretchXFromTop(RectTransform rect, float left, float right, float top, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChargeRush.UI
{
    /// <summary>
    /// Builds a top-aligned, vertically stacked scroll list under an existing content root.
    /// </summary>
    public static class UiScrollList
    {
        public static Transform Ensure(RectTransform contentRoot, Vector2? viewportSize = null, Vector2? anchoredPosition = null)
        {
            if (contentRoot == null)
            {
                return null;
            }

            contentRoot.anchorMin = new Vector2(0.5f, 0.5f);
            contentRoot.anchorMax = new Vector2(0.5f, 0.5f);
            contentRoot.pivot = new Vector2(0.5f, 0.5f);
            contentRoot.anchoredPosition = anchoredPosition ?? Vector2.zero;
            contentRoot.sizeDelta = viewportSize ?? new Vector2(900f, 380f);

            var list = contentRoot.Find("List") as RectTransform;
            if (list == null)
            {
                var listGo = new GameObject("List", typeof(RectTransform));
                list = listGo.GetComponent<RectTransform>();
                list.SetParent(contentRoot, false);
            }

            list.anchorMin = new Vector2(0f, 1f);
            list.anchorMax = new Vector2(1f, 1f);
            list.pivot = new Vector2(0.5f, 1f);
            list.anchoredPosition = Vector2.zero;
            list.sizeDelta = new Vector2(0f, 0f);
            list.offsetMin = new Vector2(0f, list.offsetMin.y);
            list.offsetMax = new Vector2(0f, list.offsetMax.y);

            var layout = list.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layout.padding = new RectOffset(24, 24, 8, 8);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = list.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = list.gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            if (contentRoot.GetComponent<RectMask2D>() == null)
            {
                contentRoot.gameObject.AddComponent<RectMask2D>();
            }

            var scroll = contentRoot.GetComponent<ScrollRect>();
            if (scroll == null)
            {
                scroll = contentRoot.gameObject.AddComponent<ScrollRect>();
            }

            scroll.content = list;
            scroll.viewport = contentRoot;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            return list;
        }

        public static void ClearRows(Transform listRoot)
        {
            if (listRoot == null)
            {
                return;
            }

            for (var i = listRoot.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(listRoot.GetChild(i).gameObject);
            }
        }

        public static TextMeshProUGUI CreateTextRow(
            Transform listRoot,
            string name,
            string text,
            float fontSize,
            float height,
            UiTextRole role = UiTextRole.Body)
        {
            var row = new GameObject(name, typeof(RectTransform), typeof(LayoutElement), typeof(TextMeshProUGUI));
            row.transform.SetParent(listRoot, false);

            var layoutElement = row.GetComponent<LayoutElement>();
            layoutElement.minHeight = height;
            layoutElement.preferredHeight = height;
            layoutElement.flexibleWidth = 1f;

            var label = row.GetComponent<TextMeshProUGUI>();
            label.fontSize = fontSize;
            label.text = text;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = true;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = true;
            UiFonts.Apply(label, role);
            return label;
        }
    }
}

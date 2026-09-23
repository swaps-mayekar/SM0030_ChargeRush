using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChargeRush.UI
{
    /// <summary>Shared visual treatment for every opaque modal panel.</summary>
    public static class PanelTheme
    {
        private const string PanelResourcePath = "Art/UI/result_panel_opaque";

        public static readonly Color TitleColor = new Color(0.24f, 0.105f, 0.035f, 1f);
        public static readonly Color FailureTitleColor = new Color(0.55f, 0.09f, 0.055f, 1f);
        public static readonly Color BodyColor = new Color(0.19f, 0.12f, 0.07f, 1f);
        public static readonly Color MutedColor = new Color(0.39f, 0.27f, 0.16f, 1f);

        private static Sprite panelSprite;

        public static Sprite PanelSprite
        {
            get
            {
                if (panelSprite == null)
                {
                    panelSprite = Resources.Load<Sprite>(PanelResourcePath);
                }

                return panelSprite;
            }
        }

        public static void Apply(GameObject panel, Vector2 size)
        {
            if (panel == null)
            {
                return;
            }

            var rect = panel.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = size;
            }

            Apply(panel.GetComponent<Image>());
        }

        public static void Apply(Image image)
        {
            if (image == null || PanelSprite == null)
            {
                return;
            }

            image.sprite = PanelSprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            image.preserveAspect = false;
        }

        public static void ApplyText(TextMeshProUGUI text, Color color)
        {
            if (text == null)
            {
                return;
            }

            text.color = color;
            text.raycastTarget = false;
        }
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChargeRush.UI
{
    /// <summary>Formats credit amounts and places the credit coin icon next to TMP labels.</summary>
    public static class CreditUi
    {
        public const string CoinResourcePath = "Art/UI/credit_coin";
        private const string IconObjectName = "InlineCreditIcon";

        private static Sprite _coin;

        public static Sprite CoinSprite
        {
            get
            {
                if (_coin == null)
                {
                    _coin = Resources.Load<Sprite>(CoinResourcePath);
                    if (_coin == null)
                    {
                        var sprites = Resources.LoadAll<Sprite>(CoinResourcePath);
                        if (sprites != null && sprites.Length > 0)
                        {
                            _coin = sprites[0];
                        }
                    }
                }

                return _coin;
            }
        }

        public static string Format(int amount) => amount.ToString();

        public static string FormatSigned(int amount) => amount >= 0 ? $"+{amount}" : amount.ToString();

        /// <summary>
        /// Places a coin just to the right of the rendered glyphs (works for left/center/right alignment).
        /// Call after layout has sized the label so character bounds are valid.
        /// </summary>
        public static void EnsureIconAfterText(TMP_Text text, float size = 44f, float gap = 8f)
        {
            var icon = EnsureChildIcon(text, size);
            if (icon == null)
            {
                return;
            }

            text.ForceMeshUpdate();

            icon.anchorMin = new Vector2(0.5f, 0.5f);
            icon.anchorMax = new Vector2(0.5f, 0.5f);
            icon.pivot = new Vector2(0f, 0.5f);
            icon.sizeDelta = new Vector2(size, size);

            var maxX = GetRenderedTextMaxX(text);
            if (float.IsNegativeInfinity(maxX))
            {
                var preferred = text.GetPreferredValues(text.text);
                var width = preferred.x;
                var rectWidth = text.rectTransform.rect.width;
                var alignment = text.horizontalAlignment;
                if (alignment == HorizontalAlignmentOptions.Right)
                {
                    maxX = rectWidth * 0.5f;
                }
                else if (alignment == HorizontalAlignmentOptions.Left)
                {
                    maxX = -rectWidth * 0.5f + width;
                }
                else
                {
                    maxX = width * 0.5f;
                }
            }

            icon.anchoredPosition = new Vector2(maxX + gap, 0f);
        }

        private static float GetRenderedTextMaxX(TMP_Text text)
        {
            var maxX = float.NegativeInfinity;
            var textInfo = text.textInfo;
            if (textInfo == null)
            {
                return maxX;
            }

            for (var i = 0; i < textInfo.characterCount; i++)
            {
                var ch = textInfo.characterInfo[i];
                if (!ch.isVisible)
                {
                    continue;
                }

                maxX = Mathf.Max(maxX, ch.topRight.x, ch.bottomRight.x);
            }

            return maxX;
        }

        /// <summary>Places a coin on the trailing edge inside a right-aligned currency label.</summary>
        public static void EnsureTrailingIcon(TMP_Text text, float size = 44f, float gap = 6f)
        {
            var icon = EnsureChildIcon(text, size);
            if (icon == null)
            {
                return;
            }

            icon.anchorMin = new Vector2(1f, 0.5f);
            icon.anchorMax = new Vector2(1f, 0.5f);
            icon.pivot = new Vector2(1f, 0.5f);
            icon.anchoredPosition = Vector2.zero;
            icon.sizeDelta = new Vector2(size, size);

            var margin = text.margin;
            text.margin = new Vector4(margin.x, margin.y, size + gap, margin.w);
        }

        private static RectTransform EnsureChildIcon(TMP_Text text, float size)
        {
            if (text == null || CoinSprite == null)
            {
                return null;
            }

            var existing = text.transform.Find(IconObjectName) as RectTransform;
            RectTransform icon;
            if (existing == null)
            {
                var go = new GameObject(IconObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                icon = go.GetComponent<RectTransform>();
                icon.SetParent(text.transform, false);
            }
            else
            {
                icon = existing;
            }

            icon.sizeDelta = new Vector2(size, size);
            var image = icon.GetComponent<Image>();
            image.sprite = CoinSprite;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
            return icon;
        }
    }
}

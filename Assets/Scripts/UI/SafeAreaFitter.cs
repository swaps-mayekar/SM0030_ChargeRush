using UnityEngine;

namespace ChargeRush.UI
{
    /// <summary>Applies device safe area insets to a RectTransform.</summary>
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Rect lastSafeArea;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea != lastSafeArea)
            {
                Apply();
            }
        }

        private void Apply()
        {
            if (rectTransform == null)
            {
                return;
            }

            lastSafeArea = Screen.safeArea;
            var anchorMin = lastSafeArea.position;
            var anchorMax = lastSafeArea.position + lastSafeArea.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
        }
    }
}

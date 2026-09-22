using TMPro;
using UnityEngine;

namespace ChargeRush.UI
{
    /// <summary>
    /// Production UI font pair: Outfit Bold for headings/buttons, DM Sans for body.
    /// </summary>
    public static class UiFonts
    {
        public const string HeadingResourcePath = "Fonts/OutfitBold SDF";
        public const string BodyResourcePath = "Fonts/DMSansRegular SDF";

        private static TMP_FontAsset _heading;
        private static TMP_FontAsset _body;

        public static TMP_FontAsset Heading
        {
            get
            {
                if (_heading == null)
                {
                    _heading = Resources.Load<TMP_FontAsset>(HeadingResourcePath);
                }

                return _heading;
            }
        }

        public static TMP_FontAsset Body
        {
            get
            {
                if (_body == null)
                {
                    _body = Resources.Load<TMP_FontAsset>(BodyResourcePath);
                }

                return _body;
            }
        }

        public static TMP_FontAsset Get(UiTextRole role)
        {
            return role == UiTextRole.Heading ? Heading : Body;
        }

        public static void Apply(TMP_Text text, UiTextRole role)
        {
            if (text == null)
            {
                return;
            }

            var font = Get(role);
            if (font != null)
            {
                text.font = font;
            }
        }

#if UNITY_EDITOR
        public static void SetEditorOverrides(TMP_FontAsset heading, TMP_FontAsset body)
        {
            _heading = heading;
            _body = body;
        }

        public static void ClearEditorOverrides()
        {
            _heading = null;
            _body = null;
        }
#endif
    }
}

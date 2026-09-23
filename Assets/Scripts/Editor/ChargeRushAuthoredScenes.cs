using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ChargeRush.Editor
{
    /// <summary>
    /// Existing game scenes are treated as hand-authored and must not be
    /// overwritten by content / art / font rebuild pipelines.
    /// </summary>
    internal static class ChargeRushAuthoredScenes
    {
        public const string Boot = "Assets/Scenes/0_Boot.unity";
        public const string MainMenu = "Assets/Scenes/1_MainMenu.unity";
        public const string LevelSelect = "Assets/Scenes/2_LevelSelect.unity";
        public const string Gameplay = "Assets/Scenes/3_Gameplay.unity";
        public const string Achievements = "Assets/Scenes/4_Achievements.unity";

        public static readonly string[] All =
        {
            Boot,
            MainMenu,
            LevelSelect,
            Gameplay,
            Achievements
        };

        private static readonly HashSet<string> ProtectedPaths = new HashSet<string>(All);

        public static bool IsProtected(string scenePath)
        {
            return !string.IsNullOrEmpty(scenePath) && ProtectedPaths.Contains(scenePath);
        }

        /// <summary>True when the scene exists on disk (skip overwrite).</summary>
        public static bool ShouldPreserve(string scenePath)
        {
            return IsProtected(scenePath) && File.Exists(scenePath);
        }

        /// <summary>Logs and returns true when the scene should be left alone.</summary>
        public static bool TryPreserve(string scenePath, string pipeline)
        {
            if (!ShouldPreserve(scenePath))
            {
                return false;
            }

            Debug.Log($"ChargeRush: Preserving authored scene at {scenePath} (skip {pipeline}).");
            return true;
        }

        public static void DeleteForForceRebuild(string scenePath)
        {
            if (scenePath == Gameplay)
            {
                Debug.Log($"ChargeRush: Preserving manually authored gameplay scene at {Gameplay}.");
                return;
            }

            if (File.Exists(scenePath))
            {
                AssetDatabase.DeleteAsset(scenePath);
            }
        }

        public static void DeleteAllForForceRebuild()
        {
            for (var i = 0; i < All.Length; i++)
            {
                DeleteForForceRebuild(All[i]);
            }
        }
    }
}

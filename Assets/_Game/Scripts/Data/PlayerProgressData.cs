using System.Collections.Generic;
using UnityEngine;

namespace BulletRoute.Data
{
    [System.Serializable]
    public class LevelProgress
    {
        public int LevelIndex;
        public bool IsCompleted;
        public int BestStars;
        public int BestMoveCount;
    }

    public static class PlayerProgressData
    {
        private const string SAVE_KEY = "BulletRoute_Progress";
        private const string CURRENT_LEVEL_KEY = "BulletRoute_CurrentLevel";

        public static void SaveLevelProgress(int levelIndex, int stars, int moveCount)
        {
            string key = $"{SAVE_KEY}_{levelIndex}";
            int savedStars = PlayerPrefs.GetInt($"{key}_stars", 0);
            int savedMoves = PlayerPrefs.GetInt($"{key}_moves", 999);

            if (stars > savedStars)
                PlayerPrefs.SetInt($"{key}_stars", stars);
            if (moveCount < savedMoves)
                PlayerPrefs.SetInt($"{key}_moves", moveCount);

            PlayerPrefs.SetInt($"{key}_completed", 1);
            PlayerPrefs.Save();
        }

        public static LevelProgress GetLevelProgress(int levelIndex)
        {
            string key = $"{SAVE_KEY}_{levelIndex}";
            return new LevelProgress
            {
                LevelIndex = levelIndex,
                IsCompleted = PlayerPrefs.GetInt($"{key}_completed", 0) == 1,
                BestStars = PlayerPrefs.GetInt($"{key}_stars", 0),
                BestMoveCount = PlayerPrefs.GetInt($"{key}_moves", 999)
            };
        }

        public static int GetCurrentLevel()
        {
            return PlayerPrefs.GetInt(CURRENT_LEVEL_KEY, 0);
        }

        public static void SetCurrentLevel(int level)
        {
            PlayerPrefs.SetInt(CURRENT_LEVEL_KEY, level);
            PlayerPrefs.Save();
        }

        public static void ClearAllProgress()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Stores local stage progression and score data as JSON in Unity's
/// per-user persistent data directory.
/// </summary>
public static class PlayerProgressSave
{
    private const int CurrentSchemaVersion = 2;
    private const string SaveFileName = "sheron-progress.json";
    private static SaveData saveData;
    private static bool isLoaded;
    private static bool canWrite = true;

    [Serializable]
    private sealed class SaveData
    {
        public int schemaVersion = CurrentSchemaVersion;
        public int highestUnlockedStage = 1;
        public int totalScore;
        public List<int> completedStages = new List<int>();
        public List<StageScore> stageScores = new List<StageScore>();
    }

    [Serializable]
    private sealed class StageScore
    {
        public int stageNumber;
        public int bestScore;
        public float bestCompletionTimeSeconds;
    }

    public static string SaveFilePath
    {
        get { return Path.Combine(Application.persistentDataPath, SaveFileName); }
    }

    public static int HighestUnlockedStage
    {
        get
        {
            EnsureLoaded();
            return saveData.highestUnlockedStage;
        }
    }

    public static int TotalScore
    {
        get
        {
            EnsureLoaded();
            return saveData.totalScore;
        }
    }

    public static void Load()
    {
        EnsureLoaded();
    }

    public static bool IsStageUnlocked(int stageNumber)
    {
        EnsureLoaded();
        return stageNumber > 0 && stageNumber <= saveData.highestUnlockedStage;
    }

    public static bool IsStageCompleted(int stageNumber)
    {
        EnsureLoaded();
        return saveData.completedStages.Contains(stageNumber);
    }

    public static int GetBestScore(int stageNumber)
    {
        EnsureLoaded();
        StageScore score = FindStageScore(stageNumber);
        return score != null ? score.bestScore : 0;
    }

    public static float GetBestCompletionTimeSeconds(int stageNumber)
    {
        EnsureLoaded();
        StageScore score = FindStageScore(stageNumber);
        return score != null ? score.bestCompletionTimeSeconds : 0f;
    }

    /// <summary>
    /// Records a completion and unlocks the next stage. Score is the best
    /// result for that stage; TotalScore is the sum of each stage's best.
    /// Pass zero until a scoring rule is added.
    /// </summary>
    public static void CompleteStage(int stageNumber, int scorePoints)
    {
        CompleteStage(stageNumber, scorePoints, 0f);
    }

    public static void CompleteStage(int stageNumber, int scorePoints, float completionTimeSeconds)
    {
        EnsureLoaded();
        if (!canWrite || stageNumber < 1 || stageNumber > saveData.highestUnlockedStage)
        {
            return;
        }

        if (!saveData.completedStages.Contains(stageNumber))
        {
            saveData.completedStages.Add(stageNumber);
        }

        saveData.highestUnlockedStage = Mathf.Max(saveData.highestUnlockedStage, stageNumber + 1);

        int safeScore = Mathf.Max(0, scorePoints);
        float safeCompletionTime = Mathf.Max(0f, completionTimeSeconds);
        StageScore stageScore = FindStageScore(stageNumber);
        if (stageScore == null)
        {
            stageScore = new StageScore
            {
                stageNumber = stageNumber,
                bestScore = safeScore,
                bestCompletionTimeSeconds = safeCompletionTime
            };
            saveData.stageScores.Add(stageScore);
        }
        else
        {
            stageScore.bestScore = Mathf.Max(stageScore.bestScore, safeScore);
            if (safeCompletionTime > 0f && (stageScore.bestCompletionTimeSeconds <= 0f
                || safeCompletionTime < stageScore.bestCompletionTimeSeconds))
            {
                stageScore.bestCompletionTimeSeconds = safeCompletionTime;
            }
        }

        RecalculateTotalScore();
        SaveToDisk();
    }

    private static void EnsureLoaded()
    {
        if (isLoaded)
        {
            return;
        }

        isLoaded = true;
        string path = SaveFilePath;
        if (!File.Exists(path))
        {
            saveData = new SaveData();
            SaveToDisk();
            return;
        }

        try
        {
            saveData = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            if (saveData == null)
            {
                throw new InvalidDataException("The save file did not contain valid progress data.");
            }

            if (saveData.schemaVersion < 1)
            {
                throw new InvalidDataException("The save file is missing a supported schema version.");
            }

            if (saveData.schemaVersion > CurrentSchemaVersion)
            {
                Debug.LogError("The Sheron save file was created by a newer game version. "
                    + "It will be preserved and not overwritten: " + path);
                canWrite = false;
                saveData = new SaveData();
                return;
            }

            saveData.schemaVersion = CurrentSchemaVersion;
            if (saveData.highestUnlockedStage < 1)
            {
                saveData.highestUnlockedStage = 1;
            }
            if (saveData.completedStages == null)
            {
                saveData.completedStages = new List<int>();
            }
            if (saveData.stageScores == null)
            {
                saveData.stageScores = new List<StageScore>();
            }

            RecalculateTotalScore();
        }
        catch (Exception exception)
        {
            PreserveCorruptSave(path);
            Debug.LogWarning("Sheron progress could not be read. A fresh save will be created. "
                + exception.Message);
            saveData = new SaveData();
            SaveToDisk();
        }
    }

    private static StageScore FindStageScore(int stageNumber)
    {
        for (int i = 0; i < saveData.stageScores.Count; i++)
        {
            StageScore score = saveData.stageScores[i];
            if (score != null && score.stageNumber == stageNumber)
            {
                return score;
            }
        }

        return null;
    }

    private static void RecalculateTotalScore()
    {
        long total = 0;
        for (int i = 0; i < saveData.stageScores.Count; i++)
        {
            StageScore score = saveData.stageScores[i];
            if (score != null)
            {
                total += Mathf.Max(0, score.bestScore);
            }
        }

        saveData.totalScore = (int)Math.Min(int.MaxValue, total);
    }

    private static void SaveToDisk()
    {
        if (!canWrite || saveData == null)
        {
            return;
        }

        try
        {
            string directory = Application.persistentDataPath;
            Directory.CreateDirectory(directory);
            File.WriteAllText(SaveFilePath, JsonUtility.ToJson(saveData, true));
        }
        catch (Exception exception)
        {
            Debug.LogError("Could not save Sheron progress to " + SaveFilePath + ". "
                + exception.Message);
        }
    }

    private static void PreserveCorruptSave(string path)
    {
        try
        {
            string backupPath = path + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            File.Copy(path, backupPath, false);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Could not preserve a copy of the unreadable progress save. "
                + exception.Message);
        }
    }
}

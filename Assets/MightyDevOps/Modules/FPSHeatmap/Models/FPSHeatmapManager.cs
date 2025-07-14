#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static Mighty.MightyCoreData;
using static Mighty.MightyCoreData.SceneData;
using static MightyFPSHeatmap.FPSHeatmapData;

namespace MightyFPSHeatmap
{
    public class FPSHeatmapManager : ScriptableObject
    {
        private static FPSHeatmapManager instance;

        public static FPSHeatmapManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = CreateInstance<FPSHeatmapManager>();
                }
                return instance;
            }
        }

        public void AddHeatmapData(string run_id, HeatmapTracking.HeatmapTracker data)
        {
            string filePath = GetPlaythroughFilePath(run_id);
            List<HeatmapTracking.HeatmapTracker> trackingData = LoadTrackingData(run_id);
            trackingData.Add(data);
            SaveTrackingData(run_id, trackingData);
        }

        public List<HeatmapTracking.HeatmapTracker> GetTrackingData(string run_id)
        {
            return LoadTrackingData(run_id);
        }

        public List<HeatmapTracking.HeatmapTracker> GetFilteredTrackers(string run_id, long runPlaybackSelectedMin, long runPlaybackSelectedMax)
        {
            List<HeatmapTracking.HeatmapTracker> trackingData = LoadTrackingData(run_id);
            return trackingData.FindAll(t => t.timeStamp >= runPlaybackSelectedMin && t.timeStamp <= runPlaybackSelectedMax);
        }

        private string GetPlaythroughFilePath(string run_id)
        {
            string directoryPath = Path.Combine(Application.persistentDataPath, "PlaythroughData");
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            return Path.Combine(directoryPath, $"playthrough_{run_id}.json");
        }

        private void SaveTrackingData(string run_id, List<HeatmapTracking.HeatmapTracker> trackingData)
        {
            string filePath = GetPlaythroughFilePath(run_id);
            string json = JsonUtility.ToJson(new TrackingDataContainer { TrackingData = trackingData });
            File.WriteAllText(filePath, json);
        }

        private List<HeatmapTracking.HeatmapTracker> LoadTrackingData(string run_id)
        {
            string filePath = GetPlaythroughFilePath(run_id);
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                return JsonUtility.FromJson<TrackingDataContainer>(json).TrackingData;
            }
            return new List<HeatmapTracking.HeatmapTracker>();
        }

        [Serializable]
        private class TrackingDataContainer
        {
            public List<HeatmapTracking.HeatmapTracker> TrackingData;
        }
    }
}
#endif
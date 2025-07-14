#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static Mighty.MightyCoreData;
using static Mighty.MightyCoreData.SceneData;
using static MightyTracking.TrackingData;

namespace MightyTracking
{
    public class TrackingManager : ScriptableObject
    {
        private static TrackingManager instance;

        public static TrackingManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = CreateInstance<TrackingManager>();
                }
                return instance;
            }
        }

        public void AddTrackingData(string run_id, Tracking.TransformTracker data)
        {
            string filePath = GetPlaythroughFilePath(run_id);
            List<Tracking.TransformTracker> trackingData = LoadTrackingData(run_id);
            trackingData.Add(data);
            SaveTrackingData(run_id, trackingData);
        }

        public List<Tracking.TransformTracker> GetTrackingData(string run_id)
        {
            return LoadTrackingData(run_id);
        }

        public List<Tracking.TransformTracker> GetFilteredTrackers(string run_id, long runPlaybackSelectedMin, long runPlaybackSelectedMax)
        {
            List<Tracking.TransformTracker> trackingData = LoadTrackingData(run_id);
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

        private void SaveTrackingData(string run_id, List<Tracking.TransformTracker> trackingData)
        {
            string filePath = GetPlaythroughFilePath(run_id);
            string json = JsonUtility.ToJson(new TrackingDataContainer { TrackingData = trackingData });
            File.WriteAllText(filePath, json);
        }

        private List<Tracking.TransformTracker> LoadTrackingData(string run_id)
        {
            string filePath = GetPlaythroughFilePath(run_id);
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                return JsonUtility.FromJson<TrackingDataContainer>(json).TrackingData;
            }
            return new List<Tracking.TransformTracker>();
        }

        [Serializable]
        private class TrackingDataContainer
        {
            public List<Tracking.TransformTracker> TrackingData;
        }
    }
}
#endif
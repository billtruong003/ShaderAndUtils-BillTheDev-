/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Diagnostics;
using static ProceduralPixels.BakeAO.Editor.BakeAOUtils;

namespace ProceduralPixels.BakeAO.Editor
{
    internal class BakingManager : ScriptableSingleton<BakingManager>
    {
        public enum State
        {
            Idle, Baking, Finalizing, Paused
        }

        [InitializeOnLoadMethod]
        private static void InitializeBakingManager()
        {
            EditorApplication.update += instance.OnEditorUpdate;
            AssemblyReloadEvents.beforeAssemblyReload += instance.AssemblyReloadEvents_beforeAssemblyReload;
        }

        private State state;
        [SerializeField] private List<BakeTask> bakingQueue = new List<BakeTask>();
        [SerializeField] private List<BakeTask> completedBakes = new List<BakeTask>();
        [SerializeField] private int bakeIteration;
        private List<BakeTask> waitingForComplete = new List<BakeTask>();

        private int AllProcessesCount => bakingQueue.Count + completedBakes.Count; 
        private int AllProcessesFromCurrentIteration => bakingQueue.Count + completedBakes.Count(b => b.bakeIteration == bakeIteration);
        public State CurrentState => state;
        private BakeTask ActiveBakingTask = null;

        [SerializeField] private int editorProgressID = -1;

        private void AssemblyReloadEvents_beforeAssemblyReload()
        {
            FinalizeAllWaitingProcesses();
        }
  
        private void FinalizeAllWaitingProcesses()
        {
            if (waitingForComplete.Count == 0)
                return;

            for (int i = waitingForComplete.Count - 1; i >= 0; i--)
            {
                var waitingTask = waitingForComplete[i];
                if (waitingTask == null)
                {
                    waitingForComplete.RemoveAt(i);
                    continue;
                }

                if (waitingTask.process == null)
                {
                    waitingForComplete.RemoveAt(i);
                    continue;
                }

                while (!waitingTask.process.IsComplete)
                    waitingTask.process.OnUpdate();

                if (bakingQueue.Remove(waitingTask))
                {
                    completedBakes.Add(waitingTask);
                    temporaryDataIsDirty = true;
                }

                if (waitingTask.process.Result != null)
                {
                    if (waitingTask.process.Result is UnityEngine.Object unityObject)
                        waitingTask.result = unityObject;
                }

                waitingForComplete.RemoveAt(i);
            }

            Resources.UnloadUnusedAssets(); // This cleans up some garbage, and fixes issue of out of memory on extremely long baking queues in 2021.3
            GC.Collect();
        }
         
        internal long EstimatedReservedMemory
        {
            get
            {
                waitingForComplete.RemoveAll(t => t == null);
                waitingForComplete.RemoveAll(t => t.process == null);
                long current = ActiveBakingTask?.process?.EstimatedMemorySize ?? 0;
                return waitingForComplete.Sum(task => task.process.EstimatedMemorySize) + current;
            }
        }

        private bool temporaryDataIsDirty = true;

        private void OnEditorUpdate()
        {
            UpdateProgress();

            if (temporaryDataIsDirty)
            {
                var allNeededMeshes = bakingQueue.SelectMany(b => b.GetAllMeshes());
                BakeAOTemporaryData.TryClear(allNeededMeshes);
                temporaryDataIsDirty = false;
            }

            if (bakingQueue.Count == 0)
            { 
                if (editorProgressID != -1) 
                    Progress.Remove(editorProgressID);

                ActiveBakingTask = null;
                editorProgressID = -1;
                if (state != State.Idle)
                    bakeIteration++;
                state = State.Idle;
                FinalizeAllWaitingProcesses();
                return;
            }

            if (editorProgressID == -1)
            {
                editorProgressID = Progress.Start("Baking AO", null, Progress.Options.Managed);
                Progress.RegisterPauseCallback(editorProgressID, OnPauseCallback);
                Progress.RegisterCancelCallback(editorProgressID, OnCancelCallback);
            }

            // Unity is marking the progress as cancelled or failed during assembly reload, but BakeAO survives assembly reload, so here it recreates the progress.
            var progressStatus = Progress.GetStatus(editorProgressID);
            if (progressStatus == Progress.Status.Failed || progressStatus == Progress.Status.Canceled)
            {
                Progress.Remove(editorProgressID);
                editorProgressID = Progress.Start("Baking AO", null, Progress.Options.Managed);
                Progress.RegisterPauseCallback(editorProgressID, OnPauseCallback); 
                Progress.RegisterCancelCallback(editorProgressID, OnCancelCallback);
            }
             
            if (state == State.Idle)
                state = State.Baking;

            if (state == State.Baking)
            {
                if (EstimatedReservedMemory > BakeAOPreferences.instance.maxMemoryUsageInMB * 1024L * 1024L)
                    FinalizeAllWaitingProcesses();

                if (ActiveBakingTask == null) 
                    ActiveBakingTask = bakingQueue.FirstOrDefault(t => t.process == null);
                 
                if (ActiveBakingTask == null)
                {
                    FinalizeAllWaitingProcesses();
                    return;
                }

                if (!bakingQueue.Contains(ActiveBakingTask)) // When after assembly reload thete is an active task, but the object is different than the task in baking queue, just dump the active task.
                {
                    ActiveBakingTask = null;
                    return;
                }

                if (ActiveBakingTask.process == null)
                    ActiveBakingTask.process = StartNewProcess(ActiveBakingTask);

                if (ActiveBakingTask.process.IsBaking)
                    ActiveBakingTask.process.OnUpdate();

                if (ActiveBakingTask.process.IsPostprocessing)
                {
                    waitingForComplete.Add(ActiveBakingTask);
                    ActiveBakingTask.bakeIteration = bakeIteration;
                    ActiveBakingTask = null;
                }
            }
            else if (state == State.Paused)
            {
                FinalizeAllWaitingProcesses();
            }
        }

        private void UpdateProgress()
        {
            if (state == State.Baking || state == State.Paused)
            {
                int finishedCount = bakingQueue.Count(t => t.isWaitingForFinalization) + completedBakes.Count(t => t.bakeIteration == bakeIteration);
                float activeTaskProgress = (ActiveBakingTask?.process?.Progress ?? 0.0f);
                float singleOperationProgress = activeTaskProgress / (float)AllProcessesFromCurrentIteration;
                float progress = finishedCount / (float)AllProcessesFromCurrentIteration + singleOperationProgress;
                string info = state == State.Baking ? "Baking progress" : "Paused";

                progressBarData.progress = progress;
                progressBarData.description = $"({finishedCount + 1}/{AllProcessesFromCurrentIteration}) {info}: {activeTaskProgress * 100.0f:#.}%";
                Progress.Report(editorProgressID, progressBarData.progress, progressBarData.description);
                Progress.SetTimeDisplayMode(editorProgressID, Progress.TimeDisplayMode.ShowRemainingTime);
            }
            if (state == State.Finalizing)
            {
                progressBarData.description = "Postprocessing baked textures";
            }
            if (state == State.Idle)
            {
                progressBarData.progress = 0.0f;
                progressBarData.description = "";
            }
        }

        private bool OnCancelCallback()
        {
            FinalizeAllWaitingProcesses();
            bakingQueue.Clear();
            temporaryDataIsDirty = true;
            ActiveBakingTask?.process?.Stop();
            ActiveBakingTask = null;
             
            return true;
        }

        private bool OnPauseCallback(bool shouldPause)
        {
            if (shouldPause && state == State.Baking) 
            {
                state = State.Paused;
                return true;
            }
            else if (!shouldPause && state == State.Paused)
            {
                state = State.Baking;
                return true;
            }

            return false;
        }

        private BakeAOProcess StartNewProcess(BakeTask bakeTask)
        {
            var process = BakeAOProcess.Create();
            process.Bake(bakeTask.bakingSetup.GetBakingSetup(), bakeTask.bakePostprocessor);
            process.Context = bakeTask.context;
            return process;
        }

        public void Enqueue(BakingSetup bakingSetup, BakePostprocessor postprocessor, UnityEngine.Object context = null)
        {
            BakeTask bakeTask = BakeTask.Create(bakingSetup, postprocessor, context);
            bakingQueue.Add(bakeTask);
            temporaryDataIsDirty = true;
            if (state == State.Paused)
                state = State.Baking;
        }

        [System.Serializable]
        internal class BakeTask : IDisposable
        {
            [SerializeField] public string name;
            [SerializeField] public SerializableBakingSetup bakingSetup;
            [SerializeField] public BakePostprocessor bakePostprocessor;
            [SerializeField] public UnityEngine.Object context;
            [SerializeField] public UnityEngine.Object result;
            [SerializeField] public int bakeIteration = 0; 

            [NonSerialized] public BakeAOProcess process;

            public bool isBaking
            {
                get
                {
                    if (process == null)
                        return false;

                    return process.IsBaking;
                }
            }

            public bool isWaitingForFinalization
            {
                get
                {
                    if (process == null)
                        return false;

                    return process.IsPostprocessing;
                }
            }

            public static BakeTask Create(BakingSetup bakingSetup, BakePostprocessor postprocessor, UnityEngine.Object context = null)
            {
                BakeTask instance = new BakeTask();
                instance.Setup(bakingSetup, postprocessor, context);
                return instance; 
            }

            public void Setup(BakingSetup bakingSetup, BakePostprocessor postprocessor, UnityEngine.Object context = null)
            {
                this.bakingSetup = new SerializableBakingSetup(bakingSetup);
                this.bakePostprocessor = postprocessor;
                this.context = context;
                this.name = context != null ? context.name : bakingSetup.meshesToBake[0].mesh.name;
                SerializeTemporaryMeshes();
            }

            private void SerializeTemporaryMeshes()
            {
                for (int i = 0; i < bakingSetup.meshesToBake.Count; i++)
                {
                    var meshContext = bakingSetup.meshesToBake[i];
                    meshContext.SerializeTemporaryMesh();
                    bakingSetup.meshesToBake[i] = meshContext;
                }

                for (int i = 0; i < bakingSetup.occluders.Count; i++)
                {
                    var meshContext = bakingSetup.occluders[i];
                    meshContext.SerializeTemporaryMesh();
                    bakingSetup.occluders[i] = meshContext;
                }
            }

            public bool IsValid()
            {
                return bakePostprocessor.IsValid();
            }

            public void Dispose()
            {
                DestroyImmediate(bakePostprocessor);
            }

            public IEnumerable<Mesh> GetAllMeshes()
            {
                var allContexts = bakingSetup.meshesToBake.Concat(bakingSetup.occluders);
                return allContexts.Select(c => c.GetMesh()).Distinct();
            }
        }

        internal enum FilterMode
        {
            All, Completed, Queued
        }

        private FilterMode filterMode = FilterMode.All;
        [SerializeField] private ProgressBarData progressBarData = new ProgressBarData();

        [System.Serializable]
        struct ProgressBarData
        {
            public string description;
            public float progress;
        }

        internal void DrawGUI(Rect content)
        {
            if (selectedStyle == null)
                CreateGUIResources();

            var progressBarRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            EditorGUI.ProgressBar(progressBarRect, progressBarData.progress, progressBarData.description);
            progressBarRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            if (ActiveBakingTask != null)
            {
                if (ActiveBakingTask.process != null)
                    EditorGUI.ProgressBar(progressBarRect, ActiveBakingTask.process.Progress, ActiveBakingTask.process.ActiveOperationDescription);
            }
            else
                EditorGUI.ProgressBar(progressBarRect, 1.0f, "No active tasks");

            var bakingControlButtonRect = new HorizontalRectLayout(EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight));
            if (state == State.Baking)
            {
                GUIUtility.HelpButton(bakingControlButtonRect.GetFromRight(21), "https://proceduralpixels.com/BakeAO/Documentation/ActiveTasks");
                if (GUI.Button(bakingControlButtonRect.GetReminder(), "Pause baking"))
                    state = State.Paused;
            }
            else if (state == State.Paused)
            {
                GUIUtility.HelpButton(bakingControlButtonRect.GetFromRight(21), "https://proceduralpixels.com/BakeAO/Documentation/ActiveTasks");
                if (GUI.Button(bakingControlButtonRect.GetReminder(), "Resume baking"))
                    state = State.Baking;
            }
            else if (state == State.Idle)
            {
                EditorGUILayout.Space(3);
            }

            EditorGUILayout.Space();
            filterMode = (FilterMode)EditorGUILayout.EnumPopup("Filter mode", filterMode);

            if (listOfProcesses == null)
                listOfProcesses = new GUIRecycledList(EditorGUIUtility.singleLineHeight * 1.0f, DrawTaskElementGUI, GetNumberOfProcessGUIElements);

            float height = content.height - 110 - EditorGUIUtility.singleLineHeight;
            var listAreaRect = EditorGUILayout.GetControlRect(false, height);
            listOfProcesses.Draw(listAreaRect);

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginDisabledGroup(waitingForComplete.Count == 0);
            if (GUILayout.Button("Import textures immediately"))
            {
                FinalizeAllWaitingProcesses();
            } 
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Clear completed"))
            {
                foreach (var task in completedBakes)
                    task.Dispose();
                completedBakes.Clear();
                temporaryDataIsDirty = true;
            }

            if (GUILayout.Button("Cancel all and clear"))
            {
                foreach (var task in completedBakes)
                    task.Dispose();
                completedBakes.Clear();

                if (ActiveBakingTask != null)
                    ActiveBakingTask.process?.Stop();
                bakingQueue.Clear();
                temporaryDataIsDirty = true; 
            }
            EditorGUILayout.EndHorizontal();
        }

        private int GetNumberOfProcessGUIElements()
        {
            switch (filterMode)
            {
                case FilterMode.All:
                    return bakingQueue.Count + completedBakes.Count;
                case FilterMode.Completed:
                    return completedBakes.Count;
                case FilterMode.Queued:
                    return bakingQueue.Count;
                default:
                    throw new Exception("Unexpected filter mode in GetNumberOfProcessGUIElements()");
            }
        }

        GUIRecycledList listOfProcesses;

        private (List<BakeTask>, int) RemapTask(int guiListIndex)
        {
            switch (filterMode)
            {
                case FilterMode.All:
                    if (guiListIndex < completedBakes.Count)
                        return (completedBakes, guiListIndex);
                    guiListIndex -= completedBakes.Count;
                    return (bakingQueue, guiListIndex);
                case FilterMode.Completed:
                    return (completedBakes, guiListIndex);
                case FilterMode.Queued:
                    return (bakingQueue, guiListIndex);
            }

            throw new Exception("RemapProcess error");
        }

        private BakeTask GetTaskFromGUIList(int guiListIndex)
        {
            var (list, index) = RemapTask(guiListIndex);
            return list[index];
        }

        private void RemoveTaskFromGUIList(int guiListIndex)
        {
            var (list, index) = RemapTask(guiListIndex);

            var bakingTask = list[index];
            if (bakingTask == ActiveBakingTask)
                ActiveBakingTask?.process?.Stop();

            bakingTask?.Dispose();
            temporaryDataIsDirty = true;

            list.RemoveAt(index);
        }

        private void DrawTaskElementGUI(int guiListIndex, Rect rect)
        {
            var (processList, _) = RemapTask(guiListIndex);
            if (processList == completedBakes)
                DrawCompletedProcessGUI(guiListIndex, rect);
            else
                DrawQueuedProcessGUI(guiListIndex, rect); 
        }

        private void DrawCompletedProcessGUI(int guiListIndex, Rect rect)
        {
            EnsureGUIResourcesExist();

            if (guiListIndex < 0 || guiListIndex >= completedBakes.Count)
                return;

            var underline = rect;
            underline.yMin = underline.yMin + underline.height - 1;
            underline.height = 1;
            GUI.DrawTexture(underline, underlineTexture);

            var completedTask = GetTaskFromGUIList(guiListIndex);

            rect.height -= 1.0f;
            var labelRect = rect;
            labelRect.width -= 60;
            GUI.Label(labelRect, completedTask.name);

            var buttonRect = rect;
            buttonRect.xMin += rect.width - 180;
            buttonRect.width = 60;

            EditorGUI.BeginDisabledGroup(completedTask.result == null);
            if (GUI.Button(buttonRect, "Result"))
                Selection.SetActiveObjectWithContext(completedBakes[guiListIndex].result, null);
            EditorGUI.EndDisabledGroup();

            buttonRect = rect;
            buttonRect.xMin += rect.width - 120;
            buttonRect.width = 60;
            EditorGUI.BeginDisabledGroup(completedTask.context == null);
            if (GUI.Button(buttonRect, "Context"))
            {
                var context = completedBakes[guiListIndex].context;
                Selection.SetActiveObjectWithContext(completedTask.context, null);
                if (context is Component component)
                    SceneView.lastActiveSceneView.FrameSelected(component);
            }
            EditorGUI.EndDisabledGroup();

            buttonRect = rect;
            buttonRect.xMin += rect.width - 60;
            buttonRect.width = 60;
            if (GUI.Button(buttonRect, "Remove"))
            {
                RemoveTaskFromGUIList(guiListIndex);
            }
        }

        GUIStyle selectedStyle;
        Texture2D selectedTexture;

        GUIStyle waitingStyle;
        Texture2D waitingTexture;

        GUIStyle underlineStyle;
        Texture2D underlineTexture;

        private void EnsureGUIResourcesExist()
        {
            if (underlineTexture == null || waitingTexture == null || selectedTexture == null)
                CreateGUIResources(true);
        }

        private void CreateGUIResources(bool forceRecreation = false)
        {
            if (selectedStyle == null | forceRecreation)
            {
                selectedStyle = new GUIStyle(GUI.skin.box);
                selectedTexture = new Texture2D(1, 1);
                selectedTexture.hideFlags = HideFlags.DontSaveInEditor;
                if (!EditorGUIUtility.isProSkin)
                    selectedTexture.SetPixel(0, 0, new Color(0.6f, 0.8f, 0.6f));
                else
                    selectedTexture.SetPixel(0, 0, new Color(0.3f, 0.4f, 0.3f));
                selectedTexture.Apply();
                selectedStyle.normal.background = selectedTexture;
            }

            if (waitingStyle == null | forceRecreation)
            {
                waitingStyle = new GUIStyle(GUI.skin.box);
                waitingTexture = new Texture2D(1, 1);
                waitingTexture.hideFlags = HideFlags.DontSaveInEditor;
                if (!EditorGUIUtility.isProSkin)
                    waitingTexture.SetPixel(0, 0, new Color(0.4f, 0.6f, 0.5f));
                else
                    waitingTexture.SetPixel(0, 0, new Color(0.2f, 0.3f, 0.25f));
                waitingTexture.Apply();
                waitingStyle.normal.background = waitingTexture;
            }

            if (underlineStyle == null | forceRecreation)
            {
                underlineStyle = new GUIStyle(GUI.skin.box);
                underlineTexture = new Texture2D(1, 1);
                underlineTexture.hideFlags = HideFlags.DontSaveInEditor;
                if (!EditorGUIUtility.isProSkin)
                    underlineTexture.SetPixel(0, 0, new Color(0.6f, 0.6f, 0.6f));
                else
                    underlineTexture.SetPixel(0, 0, new Color(0.3f, 0.3f, 0.3f));
                underlineTexture.Apply();
                underlineStyle.normal.background = underlineTexture;
            }
        }

        private void DrawQueuedProcessGUI(int guiListIndex, Rect rect)
        {
            EnsureGUIResourcesExist();

            var task = GetTaskFromGUIList(guiListIndex);
            if (task.isBaking)
                GUI.Box(rect, "", selectedStyle);
            if (task.isWaitingForFinalization)
                GUI.Box(rect, "", waitingStyle);

            if (underlineStyle == null)
                CreateGUIResources();

            var underline = rect;
            underline.yMin = underline.yMin + underline.height - 1;
            underline.height = 1;
            GUI.DrawTexture(underline, underlineTexture);

            rect.height -= 1.0f;
            var labelRect = rect;
            labelRect.width -= 60;
            string name = task.name;
            if (task.isBaking)
                name += $" ({task.process.Progress*100.0:#.}%)";
            if (task.isWaitingForFinalization)
                name += " (waiting for finalization)";
            GUI.Label(labelRect, name);

            var buttonRect = rect;
            buttonRect.xMin += rect.width - 120;
            buttonRect.width = 60;
            EditorGUI.BeginDisabledGroup(task.context == null);
            if (GUI.Button(buttonRect, "Context"))
            {
                var context = task.context;
                Selection.SetActiveObjectWithContext(context, null);
                if (context is Component component && SceneView.lastActiveSceneView != null)
                    SceneView.lastActiveSceneView.FrameSelected(component); 
            }
            EditorGUI.EndDisabledGroup();

            buttonRect = rect;
            buttonRect.xMin += rect.width - 60;
            buttonRect.width = 60;
            if (GUI.Button(buttonRect, "Remove"))
            {
                RemoveTaskFromGUIList(guiListIndex);
            }
        }
    }
}
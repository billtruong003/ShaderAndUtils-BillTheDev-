using BillTheDev.BillOutline.EdgeDetection;
using BillTheDev.Editor.BillOutline.Common.Utils;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Resolution = BillTheDev.BillOutline.EdgeDetection.Resolution;

namespace BillTheDev.Editor.BillOutline.EdgeDetection
{
    [CustomEditor(typeof(EdgeDetectionSettings))]
    public class EdgeDetectionSettingsEditor : UnityEditor.Editor
    {
        private SerializedProperty injectionPoint, showInSceneView, debugView, debugSectionsRaw;
        private SerializedProperty sectionMapPrecision, sectionMapClearValue, sectionRenderingLayer, maskRenderingLayer, maskInfluence, objectId, particles, sectionMapInput, sectionTexture, sectionTextureUvSet, sectionTextureChannel, vertexColorChannel, additionalSectionPasses;
        private SerializedProperty discontinuityInput, depthSensitivity, depthDistanceModulation, grazingAngleMaskPower, grazingAngleMaskHardness, normalSensitivity, luminanceSensitivity;
        private SerializedProperty kernel, outlineThickness, scaleWithResolution, referenceResolution, customReferenceResolution, backgroundColor, outlineColor, overrideColorInShadow, outlineColorShadow, fillColor, fadeByDistance, distanceFadeStart, distanceFadeDistance, distanceFadeColor, fadeByHeight, heightFadeStart, heightFadeDistance, heightFadeColor, blendMode;
        private SerializedProperty showSectionMapSection, showDiscontinuitySection, showOutlineSection;
        private ReorderableList additionalSectionPassesList;

        private void OnEnable()
        {
            showSectionMapSection = serializedObject.FindProperty(nameof(EdgeDetectionSettings.showSectionMapSection));
            showDiscontinuitySection = serializedObject.FindProperty(nameof(EdgeDetectionSettings.showDiscontinuitySection));
            showOutlineSection = serializedObject.FindProperty(nameof(EdgeDetectionSettings.showOutlineSection));

            injectionPoint = serializedObject.FindProperty("injectionPoint");
            showInSceneView = serializedObject.FindProperty("showInSceneView");
            debugView = serializedObject.FindProperty("debugView");
            debugSectionsRaw = serializedObject.FindProperty(nameof(EdgeDetectionSettings.debugSectionsRaw));

            sectionMapPrecision = serializedObject.FindProperty(nameof(EdgeDetectionSettings.sectionMapPrecision));
            sectionMapClearValue = serializedObject.FindProperty(nameof(EdgeDetectionSettings.sectionMapClearValue));
            sectionRenderingLayer = serializedObject.FindProperty(nameof(EdgeDetectionSettings.SectionRenderingLayer));
            maskRenderingLayer = serializedObject.FindProperty(nameof(EdgeDetectionSettings.SectionMaskRenderingLayer));
            maskInfluence = serializedObject.FindProperty(nameof(EdgeDetectionSettings.maskInfluence));
            objectId = serializedObject.FindProperty(nameof(EdgeDetectionSettings.objectId));
            particles = serializedObject.FindProperty(nameof(EdgeDetectionSettings.particles));
            sectionMapInput = serializedObject.FindProperty(nameof(EdgeDetectionSettings.sectionMapInput));
            sectionTexture = serializedObject.FindProperty(nameof(EdgeDetectionSettings.sectionTexture));
            sectionTextureUvSet = serializedObject.FindProperty(nameof(EdgeDetectionSettings.sectionTextureUvSet));
            sectionTextureChannel = serializedObject.FindProperty("sectionTextureChannel");
            vertexColorChannel = serializedObject.FindProperty(nameof(EdgeDetectionSettings.vertexColorChannel));
            additionalSectionPasses = serializedObject.FindProperty(nameof(EdgeDetectionSettings.additionalSectionPasses));

            discontinuityInput = serializedObject.FindProperty(nameof(EdgeDetectionSettings.discontinuityInput));
            depthSensitivity = serializedObject.FindProperty(nameof(EdgeDetectionSettings.depthSensitivity));
            depthDistanceModulation = serializedObject.FindProperty(nameof(EdgeDetectionSettings.depthDistanceModulation));
            grazingAngleMaskPower = serializedObject.FindProperty(nameof(EdgeDetectionSettings.grazingAngleMaskPower));
            grazingAngleMaskHardness = serializedObject.FindProperty(nameof(EdgeDetectionSettings.grazingAngleMaskHardness));
            normalSensitivity = serializedObject.FindProperty(nameof(EdgeDetectionSettings.normalSensitivity));
            luminanceSensitivity = serializedObject.FindProperty(nameof(EdgeDetectionSettings.luminanceSensitivity));

            kernel = serializedObject.FindProperty(nameof(EdgeDetectionSettings.kernel));
            outlineThickness = serializedObject.FindProperty(nameof(EdgeDetectionSettings.outlineThickness));
            scaleWithResolution = serializedObject.FindProperty(nameof(EdgeDetectionSettings.scaleWithResolution));
            referenceResolution = serializedObject.FindProperty(nameof(EdgeDetectionSettings.referenceResolution));
            customReferenceResolution = serializedObject.FindProperty(nameof(EdgeDetectionSettings.customResolution));
            backgroundColor = serializedObject.FindProperty(nameof(EdgeDetectionSettings.backgroundColor));
            outlineColor = serializedObject.FindProperty(nameof(EdgeDetectionSettings.outlineColor));
            overrideColorInShadow = serializedObject.FindProperty(nameof(EdgeDetectionSettings.overrideColorInShadow));
            outlineColorShadow = serializedObject.FindProperty(nameof(EdgeDetectionSettings.outlineColorShadow));
            fillColor = serializedObject.FindProperty(nameof(EdgeDetectionSettings.fillColor));
            fadeByDistance = serializedObject.FindProperty(nameof(EdgeDetectionSettings.fadeByDistance));
            distanceFadeStart = serializedObject.FindProperty(nameof(EdgeDetectionSettings.distanceFadeStart));
            distanceFadeDistance = serializedObject.FindProperty(nameof(EdgeDetectionSettings.distanceFadeDistance));
            distanceFadeColor = serializedObject.FindProperty(nameof(EdgeDetectionSettings.distanceFadeColor));
            fadeByHeight = serializedObject.FindProperty(nameof(EdgeDetectionSettings.fadeByHeight));
            heightFadeStart = serializedObject.FindProperty(nameof(EdgeDetectionSettings.heightFadeStart));
            heightFadeDistance = serializedObject.FindProperty(nameof(EdgeDetectionSettings.heightFadeDistance));
            heightFadeColor = serializedObject.FindProperty(nameof(EdgeDetectionSettings.heightFadeColor));
            blendMode = serializedObject.FindProperty(nameof(EdgeDetectionSettings.blendMode));

            additionalSectionPassesList = new ReorderableList(serializedObject, additionalSectionPasses, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Additional Section Passes"),
                drawElementCallback = (rect, index, _, _) => DrawAdditionalPass(rect, additionalSectionPasses.GetArrayElementAtIndex(index)),
                elementHeightCallback = _ => EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Edge Detection", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(injectionPoint, EditorUtils.CommonStyles.InjectionPoint);
            EditorGUILayout.PropertyField(showInSceneView, EditorUtils.CommonStyles.ShowInSceneView);
            EditorGUILayout.PropertyField(debugView, EditorUtils.CommonStyles.DebugStage);
            HandleDebugView();

            EditorGUILayout.Space();
            CoreEditorUtils.DrawSplitter();

            CoreEditorUtils.SectionGUI("Section Map", showSectionMapSection, DrawSectionMapGUI, serializedObject);
            CoreEditorUtils.SectionGUI("Edge Detection", showDiscontinuitySection, DrawEdgeDetectionGUI, serializedObject);
            CoreEditorUtils.SectionGUI("Outline", showOutlineSection, DrawOutlineGUI, serializedObject);

            serializedObject.ApplyModifiedProperties();
        }

        private void HandleDebugView()
        {
            var currentDiscontinuity = (DiscontinuityInput)discontinuityInput.intValue;
            switch ((DebugView)debugView.intValue)
            {
                case DebugView.Depth when !currentDiscontinuity.HasFlag(DiscontinuityInput.Depth):
                    EditorGUILayout.HelpBox("Depth is not an active source.", MessageType.Warning); break;
                case DebugView.Normals when !currentDiscontinuity.HasFlag(DiscontinuityInput.Normals):
                    EditorGUILayout.HelpBox("Normals are not an active source.", MessageType.Warning); break;
                case DebugView.Luminance when !currentDiscontinuity.HasFlag(DiscontinuityInput.Luminance):
                    EditorGUILayout.HelpBox("Luminance is not an active source.", MessageType.Warning); break;
                case DebugView.Sections:
                    if (!currentDiscontinuity.HasFlag(DiscontinuityInput.Sections))
                        EditorGUILayout.HelpBox("Sections are not an active source.", MessageType.Warning);
                    else
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(debugSectionsRaw, EditorUtils.CommonStyles.SectionsRawValues);
                        EditorGUI.indentLevel--;
                    }
                    break;
            }
        }

        private void DrawSectionMapGUI()
        {
            EditorGUILayout.PropertyField(sectionMapPrecision, EditorUtils.CommonStyles.SectionMapPrecision);
            EditorGUILayout.PropertyField(sectionMapClearValue, EditorUtils.CommonStyles.SectionMapClearValue);
            EditorGUILayout.PropertyField(sectionRenderingLayer, EditorUtils.CommonStyles.SectionLayer);
            EditorGUILayout.PropertyField(sectionMapInput, EditorUtils.CommonStyles.SectionMapInput);

            EditorGUI.indentLevel++;
            if ((SectionMapInput)sectionMapInput.intValue == SectionMapInput.VertexColors)
                EditorGUILayout.PropertyField(vertexColorChannel, EditorUtils.CommonStyles.VertexColorChannel);
            if ((SectionMapInput)sectionMapInput.intValue == SectionMapInput.SectionTexture)
            {
                EditorGUILayout.PropertyField(sectionTexture, EditorUtils.CommonStyles.SectionTexture);
                EditorGUILayout.PropertyField(sectionTextureUvSet, EditorUtils.CommonStyles.SectionTextureUVSet);
                EditorGUILayout.PropertyField(sectionTextureChannel, EditorUtils.CommonStyles.SectionTextureChannel);
            }
            EditorGUI.indentLevel--;

            using (new EditorGUI.DisabledScope((SectionMapInput)sectionMapInput.intValue == SectionMapInput.Custom))
            {
                EditorGUILayout.PropertyField(objectId, EditorUtils.CommonStyles.ObjectId);
                EditorGUILayout.PropertyField(particles, EditorUtils.CommonStyles.Particles);
            }
            if ((SectionMapInput)sectionMapInput.intValue == SectionMapInput.Custom)
                EditorGUILayout.HelpBox("Use the _SECTION_PASS keyword to render to the section map.", MessageType.Info);

            EditorGUILayout.Space();
            additionalSectionPassesList.DoLayoutList();
        }

        private void DrawEdgeDetectionGUI()
        {
            discontinuityInput.intValue = (int)(DiscontinuityInput)EditorGUILayout.EnumFlagsField(EditorUtils.CommonStyles.DiscontinuityInput, (DiscontinuityInput)discontinuityInput.intValue);
            EditorGUILayout.PropertyField(maskRenderingLayer, EditorUtils.CommonStyles.MaskLayer);
            EditorGUI.indentLevel++;
            maskInfluence.intValue = (int)(MaskInfluence)EditorGUILayout.EnumFlagsField(EditorUtils.CommonStyles.MaskInfluence, (MaskInfluence)maskInfluence.intValue);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(!((DiscontinuityInput)discontinuityInput.intValue).HasFlag(DiscontinuityInput.Depth)))
            {
                EditorGUILayout.LabelField("Depth", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(depthSensitivity, EditorUtils.CommonStyles.Sensitivity);
                EditorGUILayout.PropertyField(depthDistanceModulation, EditorUtils.CommonStyles.DepthDistanceModulation);
                EditorGUILayout.PropertyField(grazingAngleMaskPower, EditorUtils.CommonStyles.GrazingAngleMaskPower);
                EditorGUILayout.PropertyField(grazingAngleMaskHardness, EditorUtils.CommonStyles.GrazingAngleMaskHardness);
            }
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(!((DiscontinuityInput)discontinuityInput.intValue).HasFlag(DiscontinuityInput.Normals)))
            {
                EditorGUILayout.LabelField("Normals", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(normalSensitivity, EditorUtils.CommonStyles.Sensitivity);
            }
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(!((DiscontinuityInput)discontinuityInput.intValue).HasFlag(DiscontinuityInput.Luminance)))
            {
                EditorGUILayout.LabelField("Luminance", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(luminanceSensitivity, EditorUtils.CommonStyles.Sensitivity);
            }
        }

        private void DrawOutlineGUI()
        {
            EditorGUILayout.LabelField("Sampling", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(kernel, EditorUtils.CommonStyles.Kernel);
            EditorGUILayout.PropertyField(outlineThickness, EditorUtils.CommonStyles.OutlineThickness);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(scaleWithResolution, EditorUtils.CommonStyles.ScaleWithResolution);
            if (scaleWithResolution.boolValue)
            {
                EditorGUILayout.PropertyField(referenceResolution, GUIContent.none);
                if ((Resolution)referenceResolution.intValue == Resolution.Custom)
                    EditorGUILayout.PropertyField(customReferenceResolution, GUIContent.none, GUILayout.Width(100));
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Colors", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(outlineColor, EditorUtils.CommonStyles.EdgeColor);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(overrideColorInShadow, EditorUtils.CommonStyles.OverrideShadow);
            if (overrideColorInShadow.boolValue)
                EditorGUILayout.PropertyField(outlineColorShadow, GUIContent.none);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(backgroundColor, EditorUtils.CommonStyles.BackgroundColor);
            EditorGUILayout.PropertyField(fillColor, EditorUtils.CommonStyles.OutlineFillColor);

            EditorGUILayout.PropertyField(fadeByDistance, EditorUtils.CommonStyles.FadeByDistance);
            if (fadeByDistance.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(distanceFadeStart, EditorUtils.CommonStyles.FadeStart);
                EditorGUILayout.PropertyField(distanceFadeDistance, EditorUtils.CommonStyles.FadeDistance);
                EditorGUILayout.PropertyField(distanceFadeColor, EditorUtils.CommonStyles.FadeColor);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(fadeByHeight, EditorUtils.CommonStyles.FadeByHeight);
            if (fadeByHeight.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(heightFadeStart, EditorUtils.CommonStyles.FadeStart);
                EditorGUILayout.PropertyField(heightFadeDistance, EditorUtils.CommonStyles.FadeDistance);
                EditorGUILayout.PropertyField(heightFadeColor, EditorUtils.CommonStyles.FadeColor);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(blendMode, EditorUtils.CommonStyles.OutlineBlendMode);
        }

        private static void DrawAdditionalPass(Rect rect, SerializedProperty element)
        {
            var layerProp = element.FindPropertyRelative(nameof(SectionPass.RenderingLayer));
            var materialProp = element.FindPropertyRelative(nameof(SectionPass.customSectionMaterial));

            rect.height = EditorGUIUtility.singleLineHeight;
            var layerRect = new Rect(rect.x, rect.y, rect.width * 0.4f, rect.height);
            var materialRect = new Rect(layerRect.xMax + 5, rect.y, rect.width - layerRect.width - 5, rect.height);

            EditorGUI.PropertyField(layerRect, layerProp, GUIContent.none);
            EditorGUI.PropertyField(materialRect, materialProp, GUIContent.none);
        }
    }
}
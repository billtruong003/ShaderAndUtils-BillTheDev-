using System;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;
using static ProceduralPixels.BakeAO.Editor.BakeAOShaderEditorGUI;

namespace ProceduralPixels.BakeAO.Editor
{
    // Since UnityEditor.Rendering.Universal.ShaderGUI.SimpleLitShader is an internal class, for each URP version it needs to be fully copied instead of derived from.

    public class URPSimpleLitShaderGUI_BakeAOSupport : BaseShaderGUI
    {
        // Properties
        private SimpleLitGUI.SimpleLitProperties shadingModelProperties;


        // BakeAO add begin
        private BakeAOProperties bakeAOProperties;

        public override void FillAdditionalFoldouts(MaterialHeaderScopeList materialScopesList)
        {
            base.FillAdditionalFoldouts(materialScopesList);
            materialScopesList.RegisterHeaderScope(BakeAOHeader, (uint)(1 << 4), DrawBakeAOProperties); // (1 << 4), because last foldout in URP GUI has (1 << 3)
        }

        public void DrawBakeAOProperties(Material material)
        {
            bakeAOProperties.Draw(material, materialEditor, BakeAOProperties.DrawOptions.All);
        }
        // BakeAO add end

        // collect properties from the material properties
        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);
            shadingModelProperties = new SimpleLitGUI.SimpleLitProperties(properties);

            // BakeAO add begin
            bakeAOProperties = new BakeAOProperties(properties);
            // BakeAO add end
        }

        // material changed check
        public override void ValidateMaterial(Material material)
        {
            SetMaterialKeywords(material, SimpleLitGUI.SetMaterialKeywords);
        }

        // material main surface options
        public override void DrawSurfaceOptions(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;

            base.DrawSurfaceOptions(material);
        }

        // material main surface inputs
        public override void DrawSurfaceInputs(Material material)
        {
            base.DrawSurfaceInputs(material);
            SimpleLitGUI.Inputs(shadingModelProperties, materialEditor, material);
            DrawEmissionProperties(material, true);
            DrawTileOffset(materialEditor, baseMapProp);
        }

        public override void DrawAdvancedOptions(Material material)
        {
            SimpleLitGUI.Advanced(shadingModelProperties);
            base.DrawAdvancedOptions(material);
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            // _Emission property is lost after assigning Standard shader to the material
            // thus transfer it before assigning the new shader
            if (material.HasProperty("_Emission"))
            {
                material.SetColor("_EmissionColor", material.GetColor("_Emission"));
            }

            base.AssignNewShaderToMaterial(material, oldShader, newShader);

            if (oldShader == null || !oldShader.name.Contains("Legacy Shaders/"))
            {
                SetupMaterialBlendMode(material);
                return;
            }

            SurfaceType surfaceType = SurfaceType.Opaque;
            BlendMode blendMode = BlendMode.Alpha;
            if (oldShader.name.Contains("/Transparent/Cutout/"))
            {
                surfaceType = SurfaceType.Opaque;
                material.SetFloat("_AlphaClip", 1);
            }
            else if (oldShader.name.Contains("/Transparent/"))
            {
                // NOTE: legacy shaders did not provide physically based transparency
                // therefore Fade mode
                surfaceType = SurfaceType.Transparent;
                blendMode = BlendMode.Alpha;
            }
            material.SetFloat("_Surface", (float)surfaceType);
            material.SetFloat("_Blend", (float)blendMode);
        }
    }
}
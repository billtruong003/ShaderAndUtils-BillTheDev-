﻿/* Copyright (C) 2020 - Jose Ivan Lopez Romo - All rights reserved
 *
 * This file is part of the UnityOutlineEffect project found in the
 * following repository: https://github.com/Zhibade/unity-outline-effect
 *
 * Released under MIT license. Please see LICENSE file for details.
 */

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class OutlineEffectRenderPass : ScriptableRenderPass
{
    static readonly string COLOR_PROPERTY_NAME = "_FillColor";
    static readonly string COLOR_STRENGTH_PROPERTY_NAME = "_FillStrength";

    static readonly string OUTLINE_COLOR_PROPERTY_NAME = "_OutlineColor";
    static readonly string OUTLINE_STRENGTH_PROPERTY_NAME = "_OutlineStrength";
    static readonly string OUTLINE_WIDTH_PROPERTY_NAME = "_OutlineWidth";
    static readonly string OUTLINE_CUTOFF_PROPERTY_NAME = "_OutlineCutoff";
    static readonly string OUTLINE_FADEOUT_DISTANCES_PROPERTY_NAME = "_OutlineNearAndFarFadeOut";

    static readonly int MAIN_TEXTURE_PROPERTY_ID = Shader.PropertyToID("_MainTex");
    static readonly int TEMP_RENDER_TARGET_PROPERTY_ID = Shader.PropertyToID("_TempRenderTarget");

    static readonly string OUTLINE_EFFECT_TAG = "Outline Effect";

    Material material;
    OutlineEffect outlineEffect;
    RenderTargetIdentifier renderTargetIdentifier;

    public OutlineEffectRenderPass(RenderPassEvent renderPassEvent, Shader shader)
    {
        this.renderPassEvent = renderPassEvent;

        if (shader == null)
        {
            Debug.LogError("Outline effect shader not found");
            return;
        }

        material = CoreUtils.CreateEngineMaterial(shader);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (material == null || !renderingData.cameraData.postProcessEnabled)
        {
            return;
        }

        VolumeStack stack = VolumeManager.instance.stack;
        outlineEffect = stack.GetComponent<OutlineEffect>();

        if (outlineEffect == null || !outlineEffect.IsActive())
        {
            return;
        }

        CommandBuffer buffer = CommandBufferPool.Get(OUTLINE_EFFECT_TAG);
        Render(buffer, ref renderingData);

        context.ExecuteCommandBuffer(buffer);
        CommandBufferPool.Release(buffer);
    }

    public void Setup(in RenderTargetIdentifier targetIdentifier)
    {
        renderTargetIdentifier = targetIdentifier;
    }

    void Render(CommandBuffer buffer, ref RenderingData data)
    {
        ref CameraData cameraData = ref data.cameraData;
        RenderTargetIdentifier sourceRenderTarget = renderTargetIdentifier;
        int targetRenderTarget = TEMP_RENDER_TARGET_PROPERTY_ID;

        int screenWidth = cameraData.camera.scaledPixelWidth;
        int screenHeight = cameraData.camera.scaledPixelHeight;

        Vector4 fadeOutDistances = new Vector4(outlineEffect.outlineNearFadeOutLimits.value.x, outlineEffect.outlineNearFadeOutLimits.value.y,
                                               outlineEffect.outlineFarFadeOutLimits.value.x, outlineEffect.outlineFarFadeOutLimits.value.y);

        material.SetColor(OUTLINE_COLOR_PROPERTY_NAME, outlineEffect.outlineColor.value);
        material.SetFloat(OUTLINE_STRENGTH_PROPERTY_NAME, outlineEffect.outlineStrength.value);
        material.SetInt(OUTLINE_WIDTH_PROPERTY_NAME, outlineEffect.outlineWidth.value);
        material.SetFloat(OUTLINE_CUTOFF_PROPERTY_NAME, outlineEffect.outlineCutoffValue.value);
        material.SetVector(OUTLINE_FADEOUT_DISTANCES_PROPERTY_NAME, fadeOutDistances);

        material.SetColor(COLOR_PROPERTY_NAME, outlineEffect.fillColor.value);
        material.SetFloat(COLOR_STRENGTH_PROPERTY_NAME, outlineEffect.fillStrength.value);

        // Copy to temporary render target and render to actual render target

        buffer.SetGlobalTexture(MAIN_TEXTURE_PROPERTY_ID, sourceRenderTarget);
        buffer.GetTemporaryRT(targetRenderTarget, screenWidth, screenHeight, 0, FilterMode.Point, RenderTextureFormat.Default);
        buffer.Blit(sourceRenderTarget, targetRenderTarget);
        buffer.Blit(targetRenderTarget, sourceRenderTarget, material, 0);
    }
}

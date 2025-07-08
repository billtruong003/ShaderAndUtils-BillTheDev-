#ifndef ALLIN13DSHADER_FEATURES
#define ALLIN13DSHADER_FEATURES

#if defined(INSTANCING_ON) && !defined(PROCEDURAL_INSTANCING_ON)
	#define ALLIN13D_GPU_INSTANCING
#endif

#if defined(ALLIN13D_GPU_INSTANCING)
	#define BATCHING_BUFFER_START	UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
	#define BATCHING_BUFFER_END		UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)
#else
	#if defined(URP_PASS) && !defined(PROCEDURAL_INSTANCING_ON)
		#define BATCHING_BUFFER_START	CBUFFER_START(UnityPerMaterial)
		#define BATCHING_BUFFER_END		CBUFFER_END
	#else
		#define BATCHING_BUFFER_START
		#define BATCHING_BUFFER_END	
	#endif
#endif

#if defined(URP_PASS) && defined(_LIGHT_LAYERS)
	#define ALLIN1_USE_LIGHT_LAYERS
#endif

#ifdef URP_PASS
	#define DECLARE_TEX2D(texName) \
		TEXTURE2D(texName); \
		SAMPLER(sampler_##texName);

	#define SAMPLE_TEX2D(texName, uv)		SAMPLE_TEXTURE2D(texName, sampler_##texName, uv)
	#define SAMPLE_TEX2D_DERIVATIVES(texName, uv, ddx, ddy) SAMPLE_TEXTURE2D_GRAD(texName, sampler_##texName, uv, ddx, ddy)
	#define SAMPLE_TEX2D_LOD(texName, uv)	SAMPLE_TEXTURE2D_LOD(texName, sampler_##texName, uv.xy, 0)
	#define SAMPLE_TEX2D_PROJ(texName, uv)	SAMPLE_TEXTURE2D(texName, sampler##texName, uv.xy/uv.w)
#else
	#define DECLARE_TEX2D(texName)	sampler2D texName;

	#define SAMPLE_TEX2D(texName, uv)						tex2D(texName, uv)
	#define SAMPLE_TEX2D_DERIVATIVES(texName, uv, ddx, ddy) tex2D(texName, uv, ddx, ddy)
	#define SAMPLE_TEX2D_LOD(texName, uv)					tex2Dlod(texName, uv)
	#define SAMPLE_TEX2D_PROJ(texName, uv)					tex2Dproj(texName, uv)
#endif

#define STOCHASTIC_SAMPLING_NO_DEF_DD(texName, uv, stochasticOffset, res) \
	dx = ddx(uv);\
	dy = ddy(uv);\
	res = mul(SAMPLE_TEX2D_DERIVATIVES(texName, uv + hash2D2D(stochasticOffset[0].xy), dx, dy), stochasticOffset[3].x) + \
		mul(SAMPLE_TEX2D_DERIVATIVES(texName, uv + hash2D2D(stochasticOffset[1].xy), dx, dy), stochasticOffset[3].y) + \
		mul(SAMPLE_TEX2D_DERIVATIVES(texName, uv + hash2D2D(stochasticOffset[2].xy), dx, dy), stochasticOffset[3].z);
	
#define STOCHASTIC_SAMPLING(texName, uv, stochasticOffset, res) \
	float2 dx = 0;\
	float2 dy = 0;\
	STOCHASTIC_SAMPLING_NO_DEF_DD(texName, uv, stochasticOffset, res)
			
#define STOCHASTIC_SAMPLING_COMPLETE_NO_DEF_DD(texName, uv, stochasticOffsetName, res) \
	stochasticOffset = getStochasticOffsets(uv, ACCESS_PROP(_StochasticScale), ACCESS_PROP(_StochasticSkew));\
	STOCHASTIC_SAMPLING_NO_DEF_DD(texName, uv, stochasticOffsetName, res)
	
#define STOCHASTIC_SAMPLING_COMPLETE(texName, uv, stochasticOffset, res) \
	stochasticOffset = getStochasticOffsets(uv, ACCESS_PROP(_StochasticScale), ACCESS_PROP(_StochasticSkew));\
	STOCHASTIC_SAMPLING(texName, uv, stochasticOffsetName, res)

#if defined(PROCEDURAL_INSTANCING_ON)
	#define DECLARE_PROPERTY_FLOAT(name)	float name;
	#define DECLARE_PROPERTY_FLOAT2(name)	float2 name;
	#define DECLARE_PROPERTY_FLOAT3(name)	float3 name;
	#define DECLARE_PROPERTY_FLOAT4(name)	float4 name;
#else
	#if (defined(INSTANCING_ON) || defined(PROCEDURAL_INSTANCING_ON)) && !defined(URP_PASS)
		#define DECLARE_PROPERTY_FLOAT(name)	UNITY_DEFINE_INSTANCED_PROP(float, name)
		#define DECLARE_PROPERTY_FLOAT2(name)	UNITY_DEFINE_INSTANCED_PROP(float2, name)
		#define DECLARE_PROPERTY_FLOAT3(name)	UNITY_DEFINE_INSTANCED_PROP(float3, name)
		#define DECLARE_PROPERTY_FLOAT4(name)	UNITY_DEFINE_INSTANCED_PROP(float4, name)
	#else
		#define DECLARE_PROPERTY_FLOAT(name)	float name;
		#define DECLARE_PROPERTY_FLOAT2(name)	float2 name;
		#define DECLARE_PROPERTY_FLOAT3(name)	float3 name;
		#define DECLARE_PROPERTY_FLOAT4(name)	float4 name;
	#endif
#endif

#pragma shader_feature_local _ALBEDO_VERTEX_COLOR_ON
#pragma shader_feature_local _ALBEDOVERTEXCOLORMODE_MULTIPLY /*_ALBEDOVERTEXCOLORMODE_REPLACE*/

#pragma shader_feature_local _TEXTURE_BLENDING_ON
#pragma shader_feature_local _TEXTUREBLENDINGSOURCE_TEXTURE //_TEXTUREBLENDINGSOURCE_VERTEXCOLOR 
#pragma shader_feature_local _TEXTUREBLENDINGMODE_RGB /*_TEXTUREBLENDINGMODE_BLACKANDWHITE*/

#pragma shader_feature_local _SPHERIZE_NORMALS_ON

#pragma shader_feature_local _EMISSION_ON

#pragma shader_feature_local _SCREEN_SPACE_UV_ON

#pragma shader_feature_local _TRIPLANAR_MAPPING_ON
#pragma shader_feature_local _TRIPLANARNORMALSPACE_LOCAL /*_TRIPLANARNORMALSPACE_WORLD*/

#pragma shader_feature_local _LIGHTMODEL_NONE _LIGHTMODEL_CLASSIC _LIGHTMODEL_TOON _LIGHTMODEL_TOONRAMP _LIGHTMODEL_HALFLAMBERT _LIGHTMODEL_FAKEGI _LIGHTMODEL_FASTLIGHTING
#pragma shader_feature_local _SPECULARMODEL_NONE _SPECULARMODEL_CLASSIC _SPECULARMODEL_TOON _SPECULARMODEL_ANISOTROPIC _SPECULARMODEL_ANISOTROPICTOON
#pragma shader_feature_local _CUSTOM_SHADOW_COLOR_ON

#pragma shader_feature_local _AFFECTED_BY_LIGHTMAPS_ON
#pragma shader_feature_local _LIGHTMAP_COLOR_CORRECTION_ON

#pragma shader_feature_local _CUSTOM_AMBIENT_LIGHT_ON

#pragma shader_feature_local _CAST_SHADOWS_ON

#pragma shader_feature_local _RECEIVE_SHADOWS_ON

#pragma shader_feature_local _RIM_LIGHTING_ON
#pragma shader_feature_local _RIMLIGHTINGSTAGE_BEFORELIGHTING _RIMLIGHTINGSTAGE_BEFORELIGHTINGLAST _RIMLIGHTINGSTAGE_AFTERLIGHTING

#pragma shader_feature_local _GREYSCALE_ON
#pragma shader_feature_local _GREYSCALESTAGE_BEFORELIGHTING /*_GREYSCALESTAGE_AFTERLIGHTING*/

#pragma shader_feature_local _POSTERIZE_ON

#pragma shader_feature_local _NORMAL_MAP_ON
#pragma shader_feature_local _AOMAP_ON
#pragma shader_feature_local _HIGHLIGHTS_ON

#pragma shader_feature_local _USE_CUSTOM_TIME

#pragma shader_feature_local _FOG_ON

#if defined(_SPECULARMODEL_CLASSIC) || defined(_SPECULARMODEL_TOON) || defined(_SPECULARMODEL_ANISOTROPIC) || defined(_SPECULARMODEL_ANISOTROPICTOON)
	#define SPECULAR_ON
#endif

#if !defined(_LIGHTMODEL_NONE)
	#define LIGHT_ON
#endif

#if (!defined(FORWARD_ADD_PASS) && defined(BIRP_PASS)) || defined(URP_PASS)
	#define ADDITIONAL_LIGHT_LOOP
#endif

#pragma shader_feature_local _REFLECTIONS_NONE _REFLECTIONS_CLASSIC _REFLECTIONS_TOON

#if !defined(_REFLECTIONS_NONE)
	#define REFLECTIONS_ON
#endif

#pragma shader_feature_local _MATCAP_ON
#pragma shader_feature_local _MATCAPBLENDMODE_MULTIPLY //_MATCAPBLENDMODE_REPLACE

#pragma shader_feature_local _HEIGHT_GRADIENT_ON
#pragma shader_feature_local _HEIGHTGRADIENTPOSITIONSPACE_WORLD /*_HEIGHTGRADIENTPOSITIONSPACE_LOCAL*/

#pragma shader_feature_local _INTERSECTION_GLOW_ON

#pragma shader_feature_local _DEPTH_COLORING_ON

#pragma shader_feature_local _SUBSURFACE_SCATTERING_ON

#pragma shader_feature_local _ALPHA_CUTOFF_ON

#pragma shader_feature_local _COLOR_RAMP_ON
#pragma shader_feature_local _COLORRAMPLIGHTINGSTAGE_BEFORELIGHTING //_COLORRAMPLIGHTINGSTAGE_AFTERLIGHTING

#pragma shader_feature_local _HIT_ON

#pragma shader_feature_local _FADE_ON
#pragma shader_feature_local _FADE_BURN_ON

#pragma shader_feature_local _ALPHA_ROUND_ON

#pragma shader_feature_local _FADE_BY_CAM_DISTANCE_ON
#pragma shader_feature_local _FADE_BY_CAM_DISTANCE_NEAR_FADE

#pragma shader_feature_local _DITHER_ON

#pragma shader_feature_local _SHADINGMODEL_BASIC _SHADINGMODEL_PBR
#pragma shader_feature_local _METALLIC_MAP_ON
#pragma shader_feature_local _GLOSS_MAP_ON

#pragma shader_feature_local _CONTRAST_BRIGHTNESS_ON

#pragma shader_feature_local _VERTEX_SHAKE_ON
#pragma shader_feature_local _VERTEX_DISTORTION_ON

#pragma shader_feature_local _WIND_ON
#pragma shader_feature_local _USE_WIND_VERTICAL_MASK

#pragma shader_feature_local _VERTEX_INFLATE_ON
#pragma shader_feature_local _VOXELIZE_ON
#pragma shader_feature_local _GLITCH_ON
#pragma shader_feature_local _RECALCULATE_NORMALS_ON

#pragma shader_feature_local _HUE_SHIFT_ON
#pragma shader_feature_local _HOLOGRAM_ON

#pragma shader_feature_local _INTERSECTION_FADE_ON

#pragma shader_feature_local _SCROLL_TEXTURE_ON

#pragma shader_feature_local _WAVE_UV_ON

#pragma shader_feature_local _HAND_DRAWN_ON

#pragma shader_feature_local _UV_DISTORTION_ON

#pragma shader_feature_local _PIXELATE_ON

#pragma shader_feature_local _STOCHASTIC_SAMPLING_ON

#pragma shader_feature_local _OUTLINETYPE_NONE _OUTLINETYPE_SIMPLE _OUTLINETYPE_CONSTANT _OUTLINETYPE_FADEWITHDISTANCE

#if !defined(_OUTLINETYPE_NONE)
	#define OUTLINE_ON
#endif

#if defined(_INTERSECTION_GLOW_ON) || defined(_SCREEN_SPACE_UV_ON) || defined(_INTERSECTION_FADE_ON) || defined(_DEPTH_COLORING_ON) || defined(_DITHER_ON)
	#define REQUIRE_SCENE_DEPTH
#endif

#if defined(_SCREEN_SPACE_UV_ON) || defined(REQUIRE_SCENE_DEPTH) || defined(_FADE_BY_CAM_DISTANCE_ON)
	#define REQUIRE_CAM_DISTANCE
#endif

#if defined(_SPECULARMODEL_ANISOTROPIC) || defined(_SPECULARMODEL_ANISOTROPICTOON) || defined(_NORMAL_MAP_ON)
	#define REQUIRE_TANGENT_WS
#endif

#if defined(URP_PASS)
	#if defined(LIGHTMAP_ON) && defined(LIGHTMAP_SHADOW_MIXING)
		#define SUBTRACTIVE_LIGHTING
	#endif
#else
	#if defined(LIGHTMAP_ON) && defined(SHADOWS_SCREEN) && defined(LIGHTMAP_SHADOW_MIXING) && !defined(SHADOWS_SHADOWMASK)
		#define SUBTRACTIVE_LIGHTING
	#endif
#endif

//Dependencies
#if !defined(LIGHT_ON)
	#undef REFLECTIONS_ON
	#undef _NORMAL_MAP_ON
	#undef SPECULAR_ON
#endif

DECLARE_TEX2D(_MainTex)

#ifdef _TEXTURE_BLENDING_ON
	#ifdef _TEXTUREBLENDINGMODE_RGB
		DECLARE_TEX2D(_BlendingTextureG)
		DECLARE_TEX2D(_BlendingTextureB)
	#else
		DECLARE_TEX2D(_BlendingTextureWhite)
	#endif

	#ifdef _TEXTUREBLENDINGSOURCE_TEXTURE
		DECLARE_TEX2D(_TexBlendingMask)
	#endif

	#ifdef _NORMAL_MAP_ON
		#ifdef _TEXTUREBLENDINGMODE_RGB
			DECLARE_TEX2D(_BlendingNormalMapG)
			DECLARE_TEX2D(_BlendingNormalMapB)
		#else
			DECLARE_TEX2D(_BlendingNormalMapWhite)
		#endif
	#endif
#endif

#ifdef REQUIRE_TANGENT_WS
	DECLARE_TEX2D(_NormalMap)
#endif

#ifdef _COLOR_RAMP_ON
	DECLARE_TEX2D(_ColorRampTex)
#endif

// ----- Global Properties
float global_MinDepth;
float global_DepthZoneLength;
float global_DepthGradientFallOff;

#ifdef REQUIRE_SCENE_DEPTH
	DECLARE_TEX2D(global_DepthGradient)
#endif

#ifdef _CUSTOM_SHADOW_COLOR_ON
	float4 global_shadowColor;
#endif

float4 allIn13DShader_globalTime;

#ifdef _LIGHTMODEL_FASTLIGHTING
	float3 global_lightDirection;
	float4 global_lightColor;
#endif

#ifdef _WIND_ON 
	DECLARE_TEX2D(global_windNoiseTex)
	float global_windForce;
	float2 global_noiseSpeed;
	float global_useWindDir;
	float3 global_windDir;
	float global_minWindValue;
	float global_maxWindValue;
	float global_windWorldSize;
#endif
//------------------------

#if defined(_SHADINGMODEL_PBR)
	#if defined(_METALLIC_MAP_ON)
		DECLARE_TEX2D(_MetallicMap)
	#endif
	#if defined(_GLOSS_MAP_ON)
		DECLARE_TEX2D(_GlossMap)
	#endif
#endif

#ifdef SPECULAR_ON
	DECLARE_TEX2D(_SpecularMap)
#endif

#ifdef _LIGHTMODEL_TOONRAMP
	DECLARE_TEX2D(_ToonRamp)
#endif

#ifdef _AOMAP_ON
	DECLARE_TEX2D(_AOMap)
#endif

#ifdef _SUBSURFACE_SCATTERING_ON
	DECLARE_TEX2D(_SSSMap)
#endif

#ifdef _FADE_ON
	DECLARE_TEX2D(_FadeTex)
#endif

#if defined(_VERTEX_DISTORTION_ON) || defined(URP_PASS)
	DECLARE_TEX2D(_VertexDistortionNoiseTex)
#endif

#ifdef _MATCAP_ON
	DECLARE_TEX2D(_MatcapTex)
#endif

#ifdef _UV_DISTORTION_ON
	DECLARE_TEX2D(_DistortTex)
#endif

#ifdef _EMISSION_ON
	DECLARE_TEX2D(_EmissionMap)
#endif

#ifdef _TRIPLANAR_MAPPING_ON
	DECLARE_TEX2D(_TriplanarTopTex)
	DECLARE_TEX2D(_TriplanarTopNormalMap)
#endif

	BATCHING_BUFFER_START

	DECLARE_PROPERTY_FLOAT4(_MainTex_ST)
	DECLARE_PROPERTY_FLOAT4(_Color)
	DECLARE_PROPERTY_FLOAT(_GeneralAlpha)
	DECLARE_PROPERTY_FLOAT(_TimingSeed)

	#if defined(REQUIRE_TANGENT_WS) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT4(_NormalMap_ST)
		DECLARE_PROPERTY_FLOAT(_NormalStrength)
		#if defined(_TRIPLANAR_MAPPING_ON) || defined(URP_PASS)
			DECLARE_PROPERTY_FLOAT(_TopNormalStrength) 
		#endif
	#endif

	#if defined(_SHADINGMODEL_PBR) || defined(_SPECULARMODEL_ANISOTROPIC) || defined(_SPECULARMODEL_ANISOTROPICTOON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT(_Metallic)
		DECLARE_PROPERTY_FLOAT(_Smoothness)
	#endif

	#if defined(_LIGHTMODEL_TOON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT(_ToonCutoff)
		DECLARE_PROPERTY_FLOAT(_ToonSmoothness)
	#endif

	#if defined(_LIGHTMODEL_HALFLAMBERT) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT(_HalfLambertWrap)
	#endif

	#if defined(_LIGHTMODEL_FAKEGI) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT(_HardnessFakeGI)
	#endif

	#if defined(SPECULAR_ON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT(_SpecularAtten)
		DECLARE_PROPERTY_FLOAT(_Shininess)
		
		DECLARE_PROPERTY_FLOAT(_Anisotropy)
		DECLARE_PROPERTY_FLOAT(_AnisoShininess)
	#endif

	#if (defined(_AFFECTED_BY_LIGHTMAPS_ON) && defined(_LIGHTMAP_COLOR_CORRECTION_ON)) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT(_HueShiftLM)
		DECLARE_PROPERTY_FLOAT(_HueSaturationLM)
		DECLARE_PROPERTY_FLOAT(_HueBrightnessLM)
		DECLARE_PROPERTY_FLOAT(_ContrastLM)
		DECLARE_PROPERTY_FLOAT(_BrightnessLM)
	#endif

	#if defined(_CUSTOM_AMBIENT_LIGHT_ON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT4(_CustomAmbientColor)
	#endif

	#if defined(_SPECULARMODEL_TOON) || defined(_SPECULARMODEL_ANISOTROPICTOON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT(_SpecularToonCutoff)
		DECLARE_PROPERTY_FLOAT(_SpecularToonSmoothness)
	#endif

	#if defined(_AOMAP_ON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT4(_AOMap_ST)
		DECLARE_PROPERTY_FLOAT(_AOMapStrength)
		DECLARE_PROPERTY_FLOAT(_AOContrast)
		DECLARE_PROPERTY_FLOAT4(_AOColor)
	#endif

	#if defined(REFLECTIONS_ON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT(_ReflectionsAtten)
		#if defined(_REFLECTIONS_TOON) || defined(URP_PASS)
			DECLARE_PROPERTY_FLOAT(_ToonFactor)
		#endif 
	#endif

	#if defined(_CONTRAST_BRIGHTNESS_ON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT(_Contrast)
		DECLARE_PROPERTY_FLOAT(_Brightness)
	#endif

	#if defined(_HEIGHT_GRADIENT_ON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT(_MinGradientHeight)
		DECLARE_PROPERTY_FLOAT(_MaxGradientHeight)
		DECLARE_PROPERTY_FLOAT4(_GradientHeightColor01)
		DECLARE_PROPERTY_FLOAT4(_GradientHeightColor02)
	#endif

	#if defined(_INTERSECTION_GLOW_ON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT(_DepthGlowDist)
		DECLARE_PROPERTY_FLOAT(_DepthGlowPower)
		DECLARE_PROPERTY_FLOAT4(_DepthGlowColor)
		DECLARE_PROPERTY_FLOAT(_DepthGlowColorIntensity)
		DECLARE_PROPERTY_FLOAT(_DepthGlowGlobalIntensity)
	#endif

	#if defined(_INTERSECTION_FADE_ON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT(_IntersectionFadeFactor)
	#endif

	#if defined(_HIGHLIGHTS_ON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT4(_HighlightsColor)
		DECLARE_PROPERTY_FLOAT(_HighlightsStrength)
		DECLARE_PROPERTY_FLOAT(_HighlightCutoff)
		DECLARE_PROPERTY_FLOAT(_HighlightSmoothness)
		DECLARE_PROPERTY_FLOAT3(_HighlightOffset)
	#endif

	#if defined(_RIM_LIGHTING_ON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT4(_RimColor)
		DECLARE_PROPERTY_FLOAT(_RimAttenuation)
		DECLARE_PROPERTY_FLOAT(_MinRim)
		DECLARE_PROPERTY_FLOAT(_MaxRim)
		DECLARE_PROPERTY_FLOAT3(_RimOffset)
	#endif

	#if defined(_HUE_SHIFT_ON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT(_HueShift)
		DECLARE_PROPERTY_FLOAT(_HueSaturation)
		DECLARE_PROPERTY_FLOAT(_HueBrightness)
	#endif

	#if defined(_HIT_ON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT4(_HitColor)
		DECLARE_PROPERTY_FLOAT(_HitGlow)
		DECLARE_PROPERTY_FLOAT(_HitBlend)
	#endif

	#if defined(_STOCHASTIC_SAMPLING_ON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT(_StochasticScale)
		DECLARE_PROPERTY_FLOAT(_StochasticSkew)
	#endif

	#if defined(_VERTEX_SHAKE_ON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT4(_ShakeSpeed)
		DECLARE_PROPERTY_FLOAT(_ShakeSpeedMult)
		DECLARE_PROPERTY_FLOAT4(_ShakeMaxDisplacement)
		DECLARE_PROPERTY_FLOAT(_ShakeBlend)
	#endif

	#if defined(_FADE_ON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT4(_FadeTex_ST)
		DECLARE_PROPERTY_FLOAT(_FadeAmount)
		DECLARE_PROPERTY_FLOAT(_FadePower)
		DECLARE_PROPERTY_FLOAT(_FadeTransition)
		#if defined(_FADE_BURN_ON) || defined(URP_PASS)
			DECLARE_PROPERTY_FLOAT4(_FadeBurnColor)
			DECLARE_PROPERTY_FLOAT(_FadeBurnWidth)
			DECLARE_PROPERTY_FLOAT(_BurnCutoff)
			DECLARE_PROPERTY_FLOAT(_BurnSmoothness)
		#endif
	#endif

	#if defined(_FADE_BY_CAM_DISTANCE_ON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT(_MinDistanceToFade)
		DECLARE_PROPERTY_FLOAT(_MaxDistanceToFade)
	#endif

	#if defined(_DITHER_ON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT(_DitherScale)
	#endif

	#if defined(_VERTEX_INFLATE_ON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT(_MinInflate)
		DECLARE_PROPERTY_FLOAT(_MaxInflate)
		DECLARE_PROPERTY_FLOAT(_InflateBlend)
	#endif

	#if defined(_VERTEX_DISTORTION_ON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT4(_VertexDistortionNoiseTex_ST)
		DECLARE_PROPERTY_FLOAT(_VertexDistortionAmount)
		DECLARE_PROPERTY_FLOAT2(_VertexDistortionNoiseSpeed)
	#endif

	#if defined(_VOXELIZE_ON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT(_VoxelSize)
		DECLARE_PROPERTY_FLOAT(_VoxelBlend)
	#endif

	#if defined(_GLITCH_ON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT(_GlitchTiling)
		DECLARE_PROPERTY_FLOAT(_GlitchAmount)
		DECLARE_PROPERTY_FLOAT3(_GlitchOffset)
		DECLARE_PROPERTY_FLOAT(_GlitchSpeed)
		DECLARE_PROPERTY_FLOAT(_GlitchWorldSpace)
	#endif

	#if defined(_WIND_ON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT(_WindAttenuation)
		#if defined(_USE_WIND_VERTICAL_MASK) || defined(URP_PASS)
			DECLARE_PROPERTY_FLOAT(_WindVerticalMaskMinY)
			DECLARE_PROPERTY_FLOAT(_WindVerticalMaskMaxY)
		#endif
	#endif

	#if defined(_COLOR_RAMP_ON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT(_ColorRampLuminosity)
		DECLARE_PROPERTY_FLOAT(_ColorRampBlend)
		DECLARE_PROPERTY_FLOAT(_ColorRampScrollSpeed)
		DECLARE_PROPERTY_FLOAT(_ColorRampTiling)
	#endif

	#if defined(_ALBEDO_VERTEX_COLOR_ON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT(_VertexColorBlending)
	#endif

	//Texture blending properties declaration
	#if defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT4(_BlendingTextureG_ST)
		DECLARE_PROPERTY_FLOAT4(_BlendingTextureB_ST)

		DECLARE_PROPERTY_FLOAT4(_BlendingTextureWhite_ST)

		DECLARE_PROPERTY_FLOAT4(_TexBlendingMask_ST)
			
		DECLARE_PROPERTY_FLOAT(_BlendingMaskCutoffG)
		DECLARE_PROPERTY_FLOAT(_BlendingMaskSmoothnessG)

		DECLARE_PROPERTY_FLOAT(_BlendingMaskCutoffB)
		DECLARE_PROPERTY_FLOAT(_BlendingMaskSmoothnessB)

		DECLARE_PROPERTY_FLOAT(_BlendingMaskCutoffWhite)
		DECLARE_PROPERTY_FLOAT(_BlendingMaskSmoothnessWhite)
	#else
		#if defined(_TEXTURE_BLENDING_ON)
			#if defined(_TEXTUREBLENDINGMODE_RGB)
				DECLARE_PROPERTY_FLOAT4(_BlendingTextureG_ST)
				DECLARE_PROPERTY_FLOAT4(_BlendingTextureB_ST)
			#else
				DECLARE_PROPERTY_FLOAT4(_BlendingTextureWhite_ST)
			#endif

			DECLARE_PROPERTY_FLOAT(_BlendingMaskCutoffG)
			DECLARE_PROPERTY_FLOAT(_BlendingMaskSmoothnessG)

			DECLARE_PROPERTY_FLOAT(_BlendingMaskCutoffB)
			DECLARE_PROPERTY_FLOAT(_BlendingMaskSmoothnessB)

			#if defined(_TEXTUREBLENDINGSOURCE_TEXTURE)
				DECLARE_PROPERTY_FLOAT4(_TexBlendingMask_ST)
			#else
				DECLARE_PROPERTY_FLOAT(_BlendingMaskCutoffWhite)
				DECLARE_PROPERTY_FLOAT(_BlendingMaskSmoothnessWhite)
			#endif
		#endif
	#endif
	//

	#if defined(_GREYSCALE_ON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT(_GreyscaleLuminosity)
		DECLARE_PROPERTY_FLOAT4(_GreyscaleTintColor)
		DECLARE_PROPERTY_FLOAT(_GreyscaleBlend)
	#endif

	#if defined(_POSTERIZE_ON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT(_PosterizeNumColors)
		DECLARE_PROPERTY_FLOAT(_PosterizeGamma)
	#endif

	#if defined(_HAND_DRAWN_ON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT(_HandDrawnAmount)
		DECLARE_PROPERTY_FLOAT(_HandDrawnSpeed)
	#endif

	#if defined(_MATCAP_ON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT(_MatcapIntensity)
		DECLARE_PROPERTY_FLOAT(_MatcapBlend)
	#endif

	#if defined(_SCREEN_SPACE_UV_ON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT(_ScaleWithCameraDistance)
	#endif

	#if defined(_SCROLL_TEXTURE_ON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT(_ScrollTextureX)
		DECLARE_PROPERTY_FLOAT(_ScrollTextureY)
	#endif

	#if defined(_UV_DISTORTION_ON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT4(_DistortTex_ST)

		DECLARE_PROPERTY_FLOAT(_DistortAmount)
		DECLARE_PROPERTY_FLOAT(_DistortTexXSpeed)
		DECLARE_PROPERTY_FLOAT(_DistortTexYSpeed)
	#endif
	
	#if defined(_PIXELATE_ON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT(_PixelateSize)
		DECLARE_PROPERTY_FLOAT4(_MainTex_TexelSize)
	#endif

	#if defined(_EMISSION_ON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT(_EmissionSelfGlow)
		DECLARE_PROPERTY_FLOAT4(_EmissionMap_ST)
		DECLARE_PROPERTY_FLOAT4(_EmissionColor)
	#endif

	#if defined(_WAVE_UV_ON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT(_WaveAmount)
		DECLARE_PROPERTY_FLOAT(_WaveSpeed)
		DECLARE_PROPERTY_FLOAT(_WaveStrength)
		DECLARE_PROPERTY_FLOAT(_WaveX)
		DECLARE_PROPERTY_FLOAT(_WaveY)
	#endif

	#if defined(_HOLOGRAM_ON) || defined(URP_PASS)

		DECLARE_PROPERTY_FLOAT4(_HologramColor)
		DECLARE_PROPERTY_FLOAT3(_HologramLineDirection)
		DECLARE_PROPERTY_FLOAT(_HologramBaseAlpha)
		
		DECLARE_PROPERTY_FLOAT(_HologramScrollSpeed)
		DECLARE_PROPERTY_FLOAT(_HologramFrequency)
		DECLARE_PROPERTY_FLOAT(_HologramAlpha)

		DECLARE_PROPERTY_FLOAT(_HologramAccentSpeed)
		DECLARE_PROPERTY_FLOAT(_HologramAccentFrequency)
		DECLARE_PROPERTY_FLOAT(_HologramAccentAlpha)

		DECLARE_PROPERTY_FLOAT(_HologramLineCenter)
		DECLARE_PROPERTY_FLOAT(_HologramLineSpacing)
		DECLARE_PROPERTY_FLOAT(_HologramLineSmoothness)
	#endif

	#if defined(_TRIPLANAR_MAPPING_ON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT4(_TriplanarTopTex_ST)
		DECLARE_PROPERTY_FLOAT4(_TriplanarTopNormalMap_ST)

		DECLARE_PROPERTY_FLOAT(_FaceDownCutoff)
		DECLARE_PROPERTY_FLOAT(_TriplanarSharpness)
	#endif

	#if defined(_SUBSURFACE_SCATTERING_ON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT(_NormalInfluence)
		DECLARE_PROPERTY_FLOAT(_SSSPower)
		DECLARE_PROPERTY_FLOAT(_SSSFrontPower)
		DECLARE_PROPERTY_FLOAT(_SSSAtten)
		DECLARE_PROPERTY_FLOAT(_SSSFrontAtten)
		DECLARE_PROPERTY_FLOAT4(_SSSColor)
	#endif

	#if defined(_ALPHA_CUTOFF_ON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT(_AlphaCutoffValue)
	#endif

	#if defined(OUTLINE_ON) || defined(URP_PASS)
		DECLARE_PROPERTY_FLOAT4(_OutlineColor)
		DECLARE_PROPERTY_FLOAT(_OutlineThickness)
		DECLARE_PROPERTY_FLOAT(_MaxCameraDistance)
		DECLARE_PROPERTY_FLOAT(_MaxFadeDistance)
	#endif

	BATCHING_BUFFER_END

#if defined(ALLIN13D_GPU_INSTANCING)
	#define ACCESS_PROP(name) UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, name)
#else
	#define ACCESS_PROP(name) name
#endif

#endif
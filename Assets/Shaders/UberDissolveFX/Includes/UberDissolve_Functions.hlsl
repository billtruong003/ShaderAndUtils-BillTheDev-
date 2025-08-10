#ifndef SHMACKLE_UBER_DISSOLVE_FUNCTIONS_INCLUDED
#define SHMACKLE_UBER_DISSOLVE_FUNCTIONS_INCLUDED

float Hash11(float p)
{
    p = frac(p * 0.1031);
    p *= p + 33.33;
    p *= p + p;
    return frac(p);
}

float2 Hash12(float p)
{
	float3 p3  = frac(p.xxx * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.xx+p3.yz)*p3.zy);
}

float CalculatePatternValue(float2 uv, int patternType, float frequency)
{
    float pattern = 0.5h;
    switch(patternType)
    {
        case 0: pattern = sin(uv.x * frequency) * cos(uv.y * frequency) * 0.5h + 0.5h; break;
        case 1: float2 checker = floor(uv * frequency); pattern = fmod(checker.x + checker.y, 2.0h); break;
        case 2: float2 grid = frac(uv * frequency); pattern = 1.0h - (step(0.05h, grid.x) * step(0.05h, grid.y)); break;
    }
    return pattern;
}

float CalculateDissolveValue(float3 position, float2 uv, float perVertexNoise, int dissolveType)
{
    float dissolveValue = 0.5h;
    switch (dissolveType)
    {
        case 0: dissolveValue = perVertexNoise; break;
        case 1: dissolveValue = dot(position, normalize(_DissolveDirection.xyz)); break;
        case 2:
            float3 center = 0;
            #ifndef _DISSOLVE_LOCALSPACE_ON
                center = unity_ObjectToWorld[3].xyz;
            #endif
            dissolveValue = length(position - center) * _RadialDirection;
            break;
        case 3: dissolveValue = CalculatePatternValue(uv, _PatternType, _PatternFrequency); break;
        case 4: dissolveValue = perVertexNoise; break;
        case 5: dissolveValue = dot(position, normalize(_DissolveDirection.xyz)); break;
    }
    return dissolveValue;
}

float3 ApplyShatterEffect(float3 positionOS, float threshold, float perturbedDissolveValue, float perVertexNoise)
{
    float3 displacedPosition = positionOS;
    #if defined(_SHATTER_EFFECT_ON)
        float shatterActivation = 1.0h - smoothstep(threshold - _ShatterTriggerRange, threshold, perturbedDissolveValue);
        if (shatterActivation > 0.001h)
        {
            float randSeed = Hash11(perVertexNoise);
            float3 outwardPushDir = normalize(positionOS + (randSeed - 0.5h) * _ShatterOffsetStrength);
            float3 liftDir = normalize(_DissolveDirection.xyz + 0.0001h);
            float3 totalDisplacement = (outwardPushDir * _VertexDisplacement) + (liftDir * _ShatterLiftSpeed * _Time.y * randSeed);
            displacedPosition += totalDisplacement * shatterActivation * _ShatterStrength;
        }
    #endif
    return displacedPosition;
}

float3 ApplyStandardVertexDisplacement(float3 positionOS, float3 normalOS, float threshold, float perturbedDissolveValue)
{
    float3 displacedPosition = positionOS;
    #if defined(_VERTEX_DISPLACEMENT_ON)
        half waveCenter = threshold - _DissolveEdgeWidth * 0.5h;
        half waveStart = waveCenter - _DisplacementWaveWidth * 0.5h;
        half waveProgress = saturate((perturbedDissolveValue - waveStart) / (_DisplacementWaveWidth + 1e-6h));
        half displacementMagnitude = sin(waveProgress * PI);
        float3 displacementDirection = normalOS;
        displacedPosition += displacementDirection * displacementMagnitude * _VertexDisplacement;
    #endif
    return displacedPosition;
}

half3 ApplyDissolveToColor(half3 surfaceColor, float2 uv, float dissolveValue, out half finalAlpha)
{
    half noiseTexSample = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv * _NoiseScale).r;
    half pixelPerturbation = (noiseTexSample - 0.5h) * _NoiseStrength;
    half perturbedDissolveValue = dissolveValue + pixelPerturbation;
    
    half timeAnimOffset = _UseTimeAnimation > 0.5h ? sin(_Time.y * _TimeScale) * 0.05h : 0.0h;
    half threshold = _DissolveThreshold + timeAnimOffset;
    
    half edgeStart = threshold - _DissolveEdgeWidth;
    
    finalAlpha = 1.0h;
    #if defined(_DISSOLVETYPE_ALPHA_BLEND)
        half fadeStart = edgeStart - _AlphaFadeRange;
        finalAlpha = smoothstep(fadeStart, threshold, perturbedDissolveValue);
    #else
        finalAlpha = smoothstep(edgeStart, threshold, perturbedDissolveValue);
    #endif
    
    half edgeColorFactor = smoothstep(edgeStart, threshold, perturbedDissolveValue) - smoothstep(threshold, threshold + 0.001h, perturbedDissolveValue);
    
    return lerp(surfaceColor, _DissolveEdgeColor.rgb, edgeColorFactor);
}

half3 CalculateLighting_Unlit(half3 baseColor)
{
    return baseColor;
}

half3 CalculateLighting_StandardLit(half3 baseColor, float3 normalWS, float3 positionWS)
{
    Light mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS));
    half NdotL = saturate(dot(normalWS, mainLight.direction));
    half3 directDiffuse = mainLight.color * NdotL * mainLight.shadowAttenuation;
    half3 indirectDiffuse = SampleSH(normalWS);
    return baseColor * (directDiffuse + indirectDiffuse);
}

half3 CalculateLighting_BasicToon(half3 baseColor, float3 normalWS, float3 positionWS)
{
    Light mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS));
    half NdotL = dot(normalWS, mainLight.direction) * 0.5h + 0.5h;
    half lightFactor = smoothstep(_ToonRampOffset - _ToonRampSmoothness, _ToonRampOffset + _ToonRampSmoothness, NdotL);
    lightFactor *= mainLight.shadowAttenuation;
    half3 shadedColor = lerp(_ShadowTint.rgb, half3(1,1,1), lightFactor);
    return baseColor * shadedColor;
}

half3 CalculateLighting_StudioToon(half3 baseColor, float3 normalWS, float3 viewDirWS, float3 positionWS)
{
    Light mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS));
    half NdotL = saturate(dot(normalWS, mainLight.direction));

    half smoothness = _StudioToon_RampSmoothness * 0.5h;
    half highlightFactor = smoothstep(_StudioToon_HighlightThreshold - smoothness, _StudioToon_HighlightThreshold + smoothness, NdotL);
    half shadowFactor = smoothstep(_StudioToon_ShadowThreshold - smoothness, _StudioToon_ShadowThreshold + smoothness, NdotL);
    
    half3 rampColor = lerp(_StudioToon_ShadowColor.rgb, _StudioToon_MidtoneColor.rgb, shadowFactor);
    rampColor = lerp(rampColor, _StudioToon_HighlightColor.rgb, highlightFactor);
    
    half3 mainLightTerm = rampColor * mainLight.color;

    half3 ambientTerm = SampleSH(normalWS);
    #if defined(_STUDIO_GRADIENT_AMBIENT_ON)
        half ambientGradientFactor = pow(saturate(normalWS.y * 0.5 + 0.5), _StudioToon_AmbientGradientPower);
        ambientTerm = lerp(_StudioToon_GroundColor.rgb, _StudioToon_SkyColor.rgb, ambientGradientFactor);
    #endif

    half3 finalLighting = (mainLightTerm * mainLight.shadowAttenuation) + ambientTerm;
    half3 finalColor = finalLighting * baseColor;

    #if defined(_STUDIO_SPECULAR_ON)
        half3 halfDir = SafeNormalize(mainLight.direction + viewDirWS);
        half NdotH = saturate(dot(normalWS, halfDir));
        half specIntensity = smoothstep(_StudioToon_SpecularThreshold, _StudioToon_SpecularThreshold + _StudioToon_SpecularSmoothness, NdotH);
        finalColor += specIntensity * _StudioToon_SpecularColor.rgb * mainLight.color * mainLight.shadowAttenuation;
    #endif

    #if defined(_STUDIO_RIM_LIGHT_ON)
        half NdotV = 1.0h - saturate(dot(normalWS, viewDirWS));
        half rimFactor = pow(NdotV, _StudioToon_RimPower);
        rimFactor = smoothstep(_StudioToon_RimThreshold - 0.1h, _StudioToon_RimThreshold + 0.1h, rimFactor);
        finalColor += rimFactor * _StudioToon_RimColor.rgb * _StudioToon_RimColor.a;
    #endif

    return finalColor;
}

half3 CalculateLighting_ToonBling(half3 baseColor, float3 normalWS, float3 viewDirWS, float3 positionWS, float2 uv)
{
    Light mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS));
    half3 lightDir = mainLight.direction;
    half NdotL = dot(normalWS, lightDir) * 0.5h + 0.5h;

    half lightIntensity = smoothstep(_ToonRampOffset - _ToonRampSmoothness, _ToonRampOffset + _ToonRampSmoothness, NdotL);
    lightIntensity *= mainLight.shadowAttenuation;
    half3 lighting = lerp(_ShadowTint.rgb, half3(1,1,1), lightIntensity);

    half3 halfVec = SafeNormalize(lightDir + viewDirWS);
    half NdotH = saturate(dot(normalWS, halfVec));
    half specIntensity = smoothstep(_Bling_SpecOffset - _Bling_SpecSmoothness, _Bling_SpecOffset + _Bling_SpecSmoothness, NdotH);
    specIntensity *= lightIntensity;
    half3 specular = specIntensity * _Bling_SpecColor.rgb;

    half NdotV = saturate(dot(normalWS, viewDirWS));
    half rimDot = 1.0h - NdotV;
    half rimIntensity = smoothstep(_Bling_RimMin, _Bling_RimMax, rimDot);
    rimIntensity = pow(rimIntensity, _Bling_RimPower);
    half3 rim = rimIntensity * _Bling_RimColor.rgb;

    half3 finalColor = baseColor * lighting + specular + rim;

    #if defined(_BLING_EFFECT_ON)
        float2 blingCoords = uv;
        #if defined(_BLING_WORLDSPACE_ON)
            blingCoords = positionWS.xy;
        #endif
        blingCoords *= _BlingScale;

        float2 motion = float2(sin(_Time.y * _BlingSpeed), cos(_Time.y * _BlingSpeed));
        float noise = frac(Hash12(frac(blingCoords.x * 0.1031) + frac(blingCoords.y * 0.0973)).x + dot(motion, float2(0.1,0.3)));
        
        half fresnel = pow(1.0h - NdotV, _BlingFresnelPower);
        half blingStep = step(_BlingThreshold, noise) * fresnel;
        finalColor += blingStep * _BlingColor.rgb * _BlingIntensity;
    #endif

    return finalColor;
}
#endif
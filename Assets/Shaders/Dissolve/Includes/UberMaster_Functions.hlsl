#ifndef UBER_MASTER_FUNCTIONS_INCLUDED
#define UBER_MASTER_FUNCTIONS_INCLUDED

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

float CalculateSwapPattern(int type, float3 position, float2 uv)
{
    float value = 0.5h;
    switch(type)
    {
        case 0: // Noise
        {
            float2 noiseScroll = _Time.y * _SwapNoiseScrollSpeed;
            float3 blendWeights = saturate(pow(abs(normalize(position)), 4));
            blendWeights /= dot(blendWeights, 1.0h) + 1e-6h;

            half noiseSampleX = SAMPLE_TEXTURE2D(_SwapNoiseMap, sampler_SwapNoiseMap, position.yz * _SwapNoiseScale + noiseScroll).r;
            half noiseSampleY = SAMPLE_TEXTURE2D(_SwapNoiseMap, sampler_SwapNoiseMap, position.xz * _SwapNoiseScale + noiseScroll).r;
            half noiseSampleZ = SAMPLE_TEXTURE2D(_SwapNoiseMap, sampler_SwapNoiseMap, position.xy * _SwapNoiseScale + noiseScroll).r;
            value = dot(half3(noiseSampleX, noiseSampleY, noiseSampleZ), blendWeights);
            break;
        }
        case 1: // Linear
        {
            value = dot(position, normalize(_SwapDirection.xyz));
            break;
        }
        case 2: // Radial
        {
            value = length(position);
            break;
        }
        case 3: // Pattern
        {
            value = CalculatePatternValue(uv, _SwapPatternType, _SwapPatternFrequency);
            break;
        }
    }
    return value;
}

void ApplyTextureSwapAndGetEmission(
    float3 positionWS, float3 positionOS, float3 normalWS, float2 uv,
    out half3 finalAlbedo, out half3 emission)
{
    half4 primaryTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
    emission = 0.0h;

    #if defined(_TEXTURE_SWAP_ON)
        float3 effectCenterWS = _SwapWorldPosition;
        float3 positionForShape = positionWS;
        #if defined(_SWAP_LOCALSPACE_ON)
            effectCenterWS = TransformObjectToWorld(_SwapEffectCenter);
            positionForShape = positionOS;
        #endif

        float shapeMask = 1.0h;
        switch (_SwapShape)
        {
            case 0: // Sphere
            {
                float3 delta = positionForShape - _SwapEffectCenter;
                float distanceToCenter = length(delta);
                shapeMask = 1.0h - saturate(distanceToCenter / _SwapEffectRadius);
                break;
            }
            case 1: // Box
            {
                float3 delta = abs(positionForShape - _SwapEffectCenter);
                float3 boxDist = saturate(delta / (_SwapEffectExtents + 1e-6h));
                shapeMask = 1.0h - max(boxDist.x, max(boxDist.y, boxDist.z));
                break;
            }
            case 2: // Mask Texture
            {
                shapeMask = SAMPLE_TEXTURE2D(_SwapShapeMask, sampler_SwapShapeMask, uv).r;
                break;
            }
        }
        
        float3 positionForPattern = positionWS - effectCenterWS;
        #if defined(_SWAP_LOCALSPACE_ON)
            positionForPattern = positionOS - _SwapEffectCenter;
        #endif
        
        float patternValue = CalculateSwapPattern(_SwapType, positionForPattern, uv);
        
        float remappedMask = lerp(patternValue * shapeMask, shapeMask, _SwapNoiseStrength);
        
        float edgePosition = _SwapProgress;
        float halfWidth = 0.5h / max(_SwapTransitionHardness, 1.0h);
        float transitionValue = smoothstep(edgePosition - halfWidth, edgePosition + halfWidth, remappedMask);
        
        float lineFactor = 1.0h - smoothstep(0.0h, _SwapLineWidth, abs(remappedMask - edgePosition));
        lineFactor *= saturate(shapeMask * 100.0h);

        half4 secondaryTex = SAMPLE_TEXTURE2D(_SecondaryAlbedoMap, sampler_SecondaryAlbedoMap, uv);
        
        finalAlbedo = lerp(primaryTex.rgb, secondaryTex.rgb, transitionValue);
        emission = lineFactor * _SwapLineColor.rgb * transitionValue;
    #else
        finalAlbedo = primaryTex.rgb;
    #endif
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
        #if defined(_DISPLACEMENT_SATURATE_ON)
            half effectStart = threshold - _BounceWaveWidth;
            half effectEnd = threshold;
            half progress = saturate((perturbedDissolveValue - effectStart) / (effectEnd - effectStart + 1e-6h));
            half displacementMagnitude = (1.0h - progress) * _VertexDisplacement;
            displacedPosition += normalOS * displacementMagnitude;
        #else
            half waveCenter = threshold - _DissolveEdgeWidth * 0.5h;
            half waveStart = waveCenter - _BounceWaveWidth * 0.5h;
            half waveEnd = waveCenter + _BounceWaveWidth * 0.5h;
            half waveProgress = saturate((perturbedDissolveValue - waveStart) / (waveEnd - waveStart + 1e-6h));
            half pulse = sin(waveProgress * PI);
            half displacementMagnitude = pulse * _VertexDisplacement;
            displacedPosition += normalOS * displacementMagnitude;
        #endif
    #endif
    return displacedPosition;
}

half3 ApplyDissolveToFinalColor(half3 surfaceColor, float2 uv, float dissolveValue, out half finalAlpha)
{
    half noiseTexSample = SAMPLE_TEXTURE2D(_DissolveNoiseTex, sampler_DissolveNoiseTex, uv * _DissolveNoiseScale).r;
    half pixelPerturbation = (noiseTexSample - 0.5h) * _DissolveNoiseStrength;
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

half3 CalculateLighting_BasicToon(half3 baseColor, float3 normalWS, float3 viewDirWS, float3 positionWS)
{
    Light mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS));
    half3 lightDir = mainLight.direction;

    half NdotL = dot(normalWS, lightDir) * 0.5h + 0.5h;
    half diffuseRamp = smoothstep(_ToonRampOffset - _ToonRampSmoothness, _ToonRampOffset + _ToonRampSmoothness, NdotL);
    half3 diffuseColor = lerp(_ShadowTint.rgb, half3(1,1,1), diffuseRamp);
    half3 litDiffuse = baseColor * diffuseColor * mainLight.color;

    half3 halfVec = SafeNormalize(lightDir + viewDirWS);
    half NdotH = saturate(dot(normalWS, halfVec));
    half specRamp = smoothstep(_Toon_SpecOffset - _Toon_SpecSmoothness, _Toon_SpecOffset + _Toon_SpecSmoothness, NdotH);
    half3 specular = specRamp * _Toon_SpecColor.rgb * mainLight.color;

    half NdotV = saturate(dot(normalWS, viewDirWS));
    half rimDot = 1.0h - NdotV;
    half rimIntensity = smoothstep(_Toon_RimMin, _Toon_RimMax, rimDot);
    rimIntensity = pow(rimIntensity, _Toon_RimPower);
    half3 rim = rimIntensity * _Toon_RimColor.rgb;

    return (litDiffuse + specular) * mainLight.shadowAttenuation + rim;
}

half3 CalculateLighting_StyledMetal(half3 baseColor, float3 normalWS, float3 viewDirWS, float3 positionWS)
{
    Light mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS));
    half3 lightDir = mainLight.direction;
    half NdotL = dot(normalWS, lightDir);
    half3 rampSample = SAMPLE_TEXTURE2D(_Metal_Ramp, sampler_Metal_Ramp, float2(NdotL * 0.5 + 0.5, 0.5)).rgb;
    half3 H = SafeNormalize(lightDir + viewDirWS);
    half NdotH = saturate(dot(normalWS, H));
    half spec = pow(NdotH, _Metal_Brightness * 100);
    half specStep = smoothstep(_Metal_Offset, _Metal_Offset + 0.05, spec) * mainLight.shadowAttenuation;
    half3 specColor = specStep * _Metal_SpecuColor.rgb;
    half hiStep = smoothstep(_Metal_HighlightOffset, _Metal_HighlightOffset + 0.05, spec) * mainLight.shadowAttenuation;
    half3 hiColor = hiStep * _Metal_HiColor.rgb;
    half NdotV = saturate(dot(normalWS, viewDirWS));
    half rimFactor = pow(1.0 - NdotV, _Metal_RimPower);
    half3 rimColor = rimFactor * _Metal_RimColor.rgb;
    return baseColor * rampSample * mainLight.color * mainLight.shadowAttenuation + specColor + hiColor + rimColor;
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
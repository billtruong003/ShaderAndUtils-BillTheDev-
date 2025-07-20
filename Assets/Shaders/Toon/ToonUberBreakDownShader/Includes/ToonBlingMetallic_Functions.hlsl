#ifndef BILLS_TOON_BLING_ENHANCED_FUNCTIONS_INCLUDED
#define BILLS_TOON_BLING_ENHANCED_FUNCTIONS_INCLUDED

#include "Assets/Shaders/Toon/ToonUberBreakDownShader/Includes/MathUtils.hlsl"

half3 CalculateToonDiffuseContribution(half3 normalWS, Light light, half toonRampOffset, half toonRampSmoothness, half3 shadowTint)
{
    half NdotL = dot(normalWS, light.direction) * 0.5h + 0.5h;
    half toonRamp = smoothstep(toonRampOffset, toonRampOffset + toonRampSmoothness, NdotL);
    
    half3 attenuatedLightColor = light.color * light.distanceAttenuation;
    half3 finalLightColor = lerp(shadowTint * attenuatedLightColor, attenuatedLightColor, toonRamp);
    
    return finalLightColor * light.shadowAttenuation;
}

half3 CalculateToonSpecularContribution(half3 normalWS, half3 viewDirWS, Light light, half specOffset, half specSmoothness, half3 specColor)
{
    half3 halfVec = SafeNormalize(light.direction + viewDirWS);
    half NdotH = saturate(dot(normalWS, halfVec));
    half specularRamp = smoothstep(specOffset, specOffset + specSmoothness, NdotH);
    
    return specularRamp * specColor * light.color * light.shadowAttenuation;
}

half3 CalculateRimLight(half3 normalWS, half3 viewDirWS, half3 rimColor, half rimMin, half rimMax, half rimPower)
{
    half NdotV = 1.0h - saturate(dot(normalWS, viewDirWS));
    half rimRamp = smoothstep(rimMin, rimMax, pow(NdotV, rimPower));
    return rimRamp * rimColor;
}

half3 CalculateBlingEffect(float4 positionCS, float3 positionWS, half3 normalWS, half3 viewDirWS, half blingScale, half blingSpeed, half blingThreshold, half3 blingColor, half blingIntensity, half blingFresnelPower)
{
    float2 noiseUV;
    #if defined(_BLING_WORLDSPACE_ON)
        noiseUV = positionWS.xy * blingScale * 0.1h;
    #else
        noiseUV = (positionCS.xy / positionCS.w) * blingScale;
        noiseUV.x *= _ScreenParams.x / _ScreenParams.y;
    #endif

    noiseUV.y += _Time.y * blingSpeed;

    half NdotV = 1.0h - saturate(dot(normalWS, viewDirWS));
    half fresnel = pow(NdotV, blingFresnelPower);

    half noise = MU_SimplexNoise(noiseUV);
    half sparkle = smoothstep(blingThreshold, blingThreshold + 0.05h, noise);
    
    return sparkle * fresnel * blingColor * blingIntensity;
}

#endif
#ifndef TOON_AURA_CORE_INCLUDED
#define TOON_AURA_CORE_INCLUDED

// Cung cấp một nguồn sáng thay thế nếu nguồn sáng chính không tồn tại trong scene.
// Được sử dụng bởi ForwardLit pass để đảm bảo đối tượng luôn được chiếu sáng.
Light GetEffectiveMainLight(float3 positionWS)
{
    // Lấy thông tin ánh sáng chính từ URP
    Light mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS));
    
    // Nếu chế độ Fake Light được bật, hãy kiểm tra xem có ánh sáng thực hay không.
    #if defined(_FAKELIGHT_ON)
        bool hasRealLight = dot(mainLight.color, mainLight.color) > 0.001;
        // Nếu không có, sử dụng các thuộc tính fake light từ material.
        if (!hasRealLight)
        {
            mainLight.direction = normalize(_FakeLightDirection.xyz);
            mainLight.color = _FakeLightColor.rgb;
            mainLight.shadowAttenuation = 1.0; // Không có đổ bóng cho fake light
        }
    #endif
    return mainLight;
}

#endif // TOON_AURA_CORE_INCLUDED
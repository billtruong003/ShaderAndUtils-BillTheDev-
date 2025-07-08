#ifndef ALLIN13DSHADER_SHADOW_CASTER_PASS_INCLUDED
#define ALLIN13DSHADER_SHADOW_CASTER_PASS_INCLUDED

FragmentDataShadowCaster BasicVertexShadowCaster(VertexData v)
{
	FragmentDataShadowCaster o;

	UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); 

#ifdef _USE_CUSTOM_TIME
	float3 shaderTime = allIn13DShader_globalTime.xyz + ACCESS_PROP(_TimingSeed);
#else
	float3 shaderTime = _Time.xyz + ACCESS_PROP(_TimingSeed);
#endif

	v.vertex = ApplyVertexEffects(v.vertex, v.normal, shaderTime);
	
	

	o.mainUV.xy = SIMPLE_CUSTOM_TRANSFORM_TEX(v.uv, _MainTex);
	o.positionOS = v.vertex;
	o.positionWS = GetPositionWS(v.vertex);

	o = GetClipPosShadowCaster(v, o);

	return o;
}

float4 BasicFragmentShadowCaster(FragmentDataShadowCaster i) : SV_Target
{
	UNITY_SETUP_INSTANCE_ID(i);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);


#if !defined(_CAST_SHADOWS_ON)
	#if defined(BIRP_PASS)
		if (unity_LightShadowBias.z != 0.0) discard;
	#else
		discard;
	#endif
#endif

	float4 col = float4(0, 0, 0, 1);
	
	float camDistance = distance(i.positionWS, _WorldSpaceCameraPos);
	
	float4 screenPos = ComputeScreenPos(i.pos);
	col = ApplyAlphaEffects(col, i.mainUV, 1.0, camDistance, screenPos);

	col.a *= ACCESS_PROP(_GeneralAlpha);

	float alphaClip = saturate(col.a);
#ifdef _ALPHA_CUTOFF_ON
	alphaClip -= ACCESS_PROP(_AlphaCutoffValue);
#endif

	clip(alphaClip - 0.001);

	return 0;
}

#endif
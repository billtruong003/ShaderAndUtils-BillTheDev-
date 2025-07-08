#ifndef ALLIN13DSHADER_URP_DEPTH_NORMALS_PASS_INCLUDED
#define ALLIN13DSHADER_URP_DEPTH_NORMALS_PASS_INCLUDED

	struct DepthNormalsVertexData
	{
		float4 positionOS : POSITION;
		float3 normal : NORMAL;
		float2 uv : TEXCOORD0;
		UNITY_VERTEX_INPUT_INSTANCE_ID
	};

	struct DepthNormalsFragmentData
	{
		float4 positionCS : SV_POSITION;
		float3 normalWS : TEXCOORD1;
		float4 mainUV	: TEXCOORD2;
		float4 interpolator_01 : TEXCOORD3;

#ifdef REQUIRE_SCENE_DEPTH
		float4 projPos : TEXCOORD4;
#endif

		float3 positionWS : TEXCOORD5;

		UNITY_VERTEX_INPUT_INSTANCE_ID
		UNITY_VERTEX_OUTPUT_STEREO
	};

	DepthNormalsFragmentData DepthNormalsVertex(DepthNormalsVertexData input)
	{
		DepthNormalsFragmentData res;

		UNITY_SETUP_INSTANCE_ID(input);
		UNITY_TRANSFER_INSTANCE_ID(input, res);
		UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(res); 

		
		res.interpolator_01 = float4(0, 0, 0, 0);
		res.mainUV = float4(0, 0, 0, 0);
		SCALED_MAIN_UV(res) = CUSTOM_TRANSFORM_TEX(input.uv, UV_DIFF(res), _MainTex);

#ifdef _SPHERIZE_NORMALS_ON
		float3 normalOS = normalize(input.positionOS);
#else
		float3 normalOS = input.normal;
#endif
		
#ifdef _USE_CUSTOM_TIME
		float3 shaderTime = allIn13DShader_globalTime.xyz + ACCESS_PROP(_TimingSeed);
#else
		float3 shaderTime = _Time.xyz + ACCESS_PROP(_TimingSeed);
#endif
		
		input.positionOS = ApplyVertexEffects(input.positionOS, normalOS, shaderTime);
		
		res.positionCS = TransformObjectToHClip(input.positionOS.xyz);
		res.normalWS = GetNormalWS(input.normal);
		res.positionWS = GetPositionWS(input.positionOS);
	
		float4 projPos = 0;
#ifdef REQUIRE_SCENE_DEPTH
		res.projPos = ComputeScreenPos(res.positionCS);

		float3 positionWS = mul(unity_ObjectToWorld, input.positionOS).xyz;
		res.projPos.z = ComputeEyeDepth(positionWS);
		projPos = res.projPos;
#endif

		return res;
	}

	float4 DepthNormalsFragment(DepthNormalsFragmentData input) : SV_TARGET
	{
		UNITY_SETUP_INSTANCE_ID(input);
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

		float3 normalWS = normalize(input.normalWS);
		float4 res = float4(normalWS, 1.0);

		float camDistance = distance(input.positionWS, _WorldSpaceCameraPos);
		float4 screenPos = ComputeScreenPos(input.positionCS);
		res = ApplyAlphaEffects(res, SCALED_MAIN_UV(input), 1.0, camDistance, screenPos);

		float alphaClip = saturate(res.a);
		clip(alphaClip - 0.001);

		return res;
	}

#endif
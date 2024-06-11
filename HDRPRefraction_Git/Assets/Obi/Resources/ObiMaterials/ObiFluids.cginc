#ifndef OBIFLUIDS_INCLUDED
#define OBIFLUIDS_INCLUDED

#include "ObiUtils.cginc"
#include "UnityCG.cginc"

float3 _FarCorner;
float _ThicknessCutoff;

TEXTURE2D(_CameraOpaqueTexture); // background for refraction
TEXTURE2D(_Foam);    // foam color / opacity.
TEXTURE2D_HALF(_Normals); // normals
TEXTURE2D_FLOAT(_FluidSurface);  // depth
TEXTURE2D_FLOAT(_CameraDepthTexture);

SAMPLER(sampler_CameraOpaqueTexture);
SAMPLER(sampler_Foam);
SAMPLER(sampler_Normals);
SAMPLER(sampler_FluidSurface);
SAMPLER(sampler_CameraDepthTexture);

float Z2EyeDepth(float z) 
{
    if (unity_OrthoParams.w < 0.5)
        return LinearEyeDepth(z); // Unity's LinearEyeDepth only works for perspective cameras.
	else{

		// since we're not using LinearEyeDepth in orthographic, we must reverse depth direction ourselves:
		#if UNITY_REVERSED_Z 
			z = 1-z;
		#endif

        return ((_ProjectionParams.z - _ProjectionParams.y) * z + _ProjectionParams.y);
	}
}

// returns eye space position from linear eye depth.
float3 EyePosFromDepth(float2 uv,float eyeDepth){

	if (unity_OrthoParams.w < 0.5){
		float3 ray = (float3(-0.5f,-0.5f,0) + float3(uv,-1)) * _FarCorner;
		return ray * eyeDepth / _FarCorner.z;
	}else{
		return float3((uv-half2(0.5f,0.5f)) * _FarCorner.xy,-eyeDepth);
	}
}

void SetupEyeSpaceFragment(in float2 uv, out float3 eyePos, out float3 eyeNormal)
{
	float eyeZ = SAMPLE_TEXTURE2D(_FluidSurface, sampler_FluidSurface, uv).r; // we expect linear depth here.

	// reconstruct eye space position/direction from frustum corner and camera depth:
	eyePos = EyePosFromDepth(uv,eyeZ);

	// get normal from texture: 
	eyeNormal = (SAMPLE_TEXTURE2D(_Normals,sampler_Normals,uv)-0.5) * 2;
}

void GetWorldSpaceFragment(in float3 eyePos, in float3 eyeNormal, 
						   out float3 worldPos, out float3 worldNormal, out float3 worldView)
{
	// Get world space position, normal and view direction:
	worldPos 	= mul(_Camera_to_World,half4(eyePos,1)).xyz;
	worldNormal = mul((float3x3)_Camera_to_World,eyeNormal);
	worldView   = normalize(UnityWorldSpaceViewDir(worldPos.xyz));
}

float OutputFragmentDepth(in float3 eyePos)
{

	float4 clipPos = mul(unity_CameraProjection,float4(eyePos,1));
	float depth = clipPos.z/clipPos.w;

	depth = 0.5*depth + 0.5;

	// DX11 and some other APIs make use of reverse zbuffer since 5.5. Must inverse value before outputting.
	#if UNITY_REVERSED_Z 
		depth = 1-depth;
	#endif

    return depth;
}

#endif

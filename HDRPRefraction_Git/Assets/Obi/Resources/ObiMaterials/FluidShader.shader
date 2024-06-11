
Shader "Obi/Fluid/FluidShading"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Smoothness ("Smoothness", Range (0, 1)) = 0.8
        _AmbientMultiplier ("AmbientMultiplier", Range (0, 6)) = 1
         _Metalness ("Metalness", Range (0, 1)) = 0
         _Transparency ("Transparency", Range (0, 1)) = 1
         _ReflectionCoeff ("Reflection",Range(0,1)) = 0.2
		_RefractionCoeff ("Refraction", Range (-0.1, 0.1)) = 0.05
         _AbsorptionCoeff ("AbsorptionCoeff", Range (0, 30)) = 5
         _FoamAbsorptionCoeff ("FoamAbsorptionCoeff", Range (0, 30)) = 5

        _BlendSrc ("BlendSrc", Float) = 0
        _BlendDst ("BlendDst", Float) = 0
        _ZTest ("ZTest", Float) = 4
	}

	SubShader
	{

		Pass
		{

			Name "FluidShader"
            Blend [_BlendSrc] [_BlendDst]
            ZTest [_ZTest]

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
            
            #pragma multi_compile_local _ FLUID_LIGHTING
            #pragma multi_compile_local _ FLUID_REFLECTION
            #pragma multi_compile_local _ FLUID_REFRACTION
            #pragma multi_compile_local _ FLUID_FOAM
			
			#include "ObiFluids.cginc"
            #include "ObiLightingBuiltIn.cginc"
            #include "UnityStandardBRDF.cginc"
            #include "UnityImageBasedLighting.cginc"

			struct vin
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 pos : POSITION;
			};

            struct fout 
            {
                half4 color : SV_Target;
                #if (FLUID_LIGHTING | FLUID_REFLECTION | FLUID_REFRACTION) 
                    float depth : SV_Depth;
                #endif
            };

			v2f vert (vin v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.pos);
				o.uv = v.uv;

				return o;
			}
         
            TEXTURE2D(_MainTex); // rgb = color, a = thickness
            SAMPLER(sampler_MainTex);
            float _Smoothness;
            float _AmbientMultiplier;
            float _Metalness;
            float _Transparency;
            float _ReflectionCoeff;
            float _RefractionCoeff;
            float _AbsorptionCoeff;
            float _FoamAbsorptionCoeff;

            #if (FLUID_LIGHTING)
			UNITY_DECLARE_SHADOWMAP(_MyShadowMap);
            #endif

            float4 ThicknessClip(in float2 uv)
            {
                float4 volume = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv);

                if (volume.a * 10 < _ThicknessCutoff) // thickness test.
                    discard;

                return volume;
            }
            
		    fout frag (v2f i)
		    {
                
                // Get color/thickness and apply thickness threshold:
                float4 volume = ThicknessClip(i.uv);

                // Initialize fragment color using particle color:
                fout fo;
                fo.color = fixed4(volume.rgb,1);

                half3 absorption = half3(1,1,1);
                
                #if (FLUID_LIGHTING | FLUID_REFLECTION | FLUID_REFRACTION) 

                    float3 eyePos,eyeNormal, worldPos, worldNormal, worldView;

                    // Performs thickness-based cutoff,
                    // returns volume (rgb=color, a=thickness), 
                    // and gets eye space position and normal.
                    SetupEyeSpaceFragment(i.uv,eyePos,eyeNormal); 
                 
                    // Converts position and normal from eye to world space.
                    GetWorldSpaceFragment(eyePos,eyeNormal,worldPos,worldNormal,worldView);


                    // Calculates diffuse and specular lighting:
                    float nv = DotClamped( worldNormal, worldView);
                    float spec = 0;
                    float atten = 1;
                    #if (FLUID_LIGHTING)
                    
                        // directional light shadow (cascades)
                        float4 viewZ = -eyePos.z;
                        float4 zNear = float4( viewZ >= _LightSplitsNear );
                        float4 zFar = float4( viewZ < _LightSplitsFar );
                        float4 weights = zNear * zFar;
                        float4 wPos = float4(worldPos,1);
                        float3 shadowCoord0 = mul( unity_WorldToShadow[0], wPos).xyz;
                        float3 shadowCoord1 = mul( unity_WorldToShadow[1], wPos).xyz;
                        float3 shadowCoord2 = mul( unity_WorldToShadow[2], wPos).xyz;
                        float3 shadowCoord3 = mul( unity_WorldToShadow[3], wPos).xyz;
                        float4 shadowCoord = float4(shadowCoord0 * weights[0] + shadowCoord1 * weights[1] + shadowCoord2 * weights[2] + shadowCoord3 * weights[3],1);
                        atten = UNITY_SAMPLE_SHADOW(_MyShadowMap, shadowCoord);

                        // lighting vectors:
                        float3 lightDirWorld = normalize(_WorldSpaceLightPos0.xyz);
                        float3 h = normalize( lightDirWorld + worldView );
                        float nh = BlinnTerm ( worldNormal, h );
                        float nl = DotClamped( worldNormal, lightDirWorld);	
                                
                        // energy-conserving microfacet specular lightning:
                        half V = SmithBeckmannVisibilityTerm (nl, nv, 1-_Smoothness);
                        half D = NDFBlinnPhongNormalizedTerm(nh,RoughnessToSpecPower(1-_Smoothness));
                        spec = (V * D) * (UNITY_PI/4);
                        if (IsGammaSpace())
                            spec = sqrt(max(1e-4h, spec));

                        // clamp and attenuate specular:
                        spec = max(0, spec * nl) * atten;

                        // ambient:
                        half3 ambient = SampleSphereAmbient(eyeNormal,eyePos) * _AmbientMultiplier;

                        // diffuse lighting:
                        fo.color.rgb *= (ambient + nl * _LightColor0 * atten);

                    #endif

                    // Calculates refraction and absorption (using beer's law).
                    #if (FLUID_REFRACTION)

                        absorption = exp(-_AbsorptionCoeff * (1 - volume.rgb) * volume.a);
                        fixed3 refraction = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, i.uv + eyeNormal.xy * volume.a * _RefractionCoeff) * absorption;
                                           
                        // lerp between diffuse and refraction using transparency:
                        fo.color.rgb = lerp(fo.color.rgb, refraction, _Transparency);

                    #endif

                    // Adds probe reflection:
                    #if (FLUID_REFLECTION)

                        Unity_GlossyEnvironmentData g;
                        g.roughness = 1 - _Smoothness;
                        g.reflUVW = reflect(-worldView,worldNormal);
                        float3 reflection = Unity_GlossyEnvironment (UNITY_PASS_TEXCUBE(unity_SpecCube0), unity_SpecCube0_HDR, g);

                        reflection *= lerp(fixed3(1,1,1),volume.rgb, _Metalness);

                        // lerp between refraction/diffuse and reflection using fresnel:
                        float fresnel = FresnelTerm(_ReflectionCoeff,nv);
                        fo.color.rgb = lerp(fo.color.rgb, reflection, fresnel);

                    #endif

                    // Add specular lighting on top, if any.
                    fo.color.rgb += spec;
                    fo.depth = OutputFragmentDepth(eyePos);

                #endif

                // Adds foam:
                #if (FLUID_FOAM)
                   fixed4 foam = SAMPLE_TEXTURE2D(_Foam,sampler_Foam,i.uv);
                   fo.color.rgb += foam.rgb * absorption;
                #endif
           
                return fo;
            }
	        ENDCG
	    }
    }
}

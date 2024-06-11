
Shader "Obi/URP/Fluid/FluidShading"
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
        Tags{"RenderPipeline" = "UniversalRenderPipeline"}

		Pass
		{

			Name "FluidShader"
		    Blend [_BlendSrc] [_BlendDst]
            ZTest [_ZTest]

			HLSLPROGRAM

            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

			#pragma vertex vert
			#pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #pragma shader_feature _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature _GLOSSYREFLECTIONS_OFF
            #pragma shader_feature _SPECULAR_SETUP
            #pragma shader_feature _RECEIVE_SHADOWS_OFF

            #pragma multi_compile_local _ FLUID_LIGHTING
            #pragma multi_compile_local _ FLUID_REFLECTION
            #pragma multi_compile_local _ FLUID_REFRACTION
            #pragma multi_compile_local _ FLUID_FOAM
			
            #include "ObiLightingURP.cginc"
            #include "ObiFluidsURP.cginc"

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

                VertexPositionInputs vertexInput = GetVertexPositionInputs(v.pos.xyz);

				o.pos = vertexInput.positionCS;
				o.uv = v.uv;

				return o;
			}
            
            TEXTURE2D(_Volume); // rgb = color, a = thickness
            SAMPLER(sampler_Volume);
            float _AmbientMultiplier;
            float _Metalness;
            float _Transparency;
            float _ReflectionCoeff;
            float _RefractionCoeff;
            float _AbsorptionCoeff;
            float _FoamAbsorptionCoeff;   

            float4 ThicknessClip(in float2 uv)
            {
                float4 volume = SAMPLE_TEXTURE2D(_Volume, sampler_Volume, uv);

                if (volume.a * 10 < _ThicknessCutoff) // thickness test.
                    discard;

                return volume;
            }

            inline half3 FresnelTerm (half3 F0, half cosA)
            {
                half t = Pow4 (1 - cosA);   // ala Schlick interpoliation
                return F0 + (1-F0) * t;
            }    
                       
			fout frag (v2f i)
			{
                // Get color/thickness and apply thickness threshold:
                float4 volume = ThicknessClip(i.uv);

				// Initialize fragment color using particle color:
                fout fo;
                fo.color = half4(volume.rgb,1);

                half3 absorption = half3(1,1,1);

                #if (FLUID_LIGHTING | FLUID_REFLECTION | FLUID_REFRACTION) 

                    float3 eyePos,eyeNormal, worldPos, worldNormal, worldView;

                    // Performs thickness-based cutoff,
                    // returns volume (rgb=color, a=thickness), 
                    // and gets eye space position and normal.
                    SetupEyeSpaceFragment(i.uv,eyePos,eyeNormal); 
                 
                    // Converts position and normal from eye to world space.
                    GetWorldSpaceFragment(eyePos,eyeNormal,worldPos,worldNormal,worldView);


                    float nv = saturate(dot(worldNormal, worldView));
                    half3 spec = half3(0,0,0);
                    #if (FLUID_LIGHTING)

                        SurfaceData surfaceData;
                        InitializeStandardLitSurfaceData(i.uv, surfaceData);

                        BRDFData brdfData;
                        InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);
                    
                        // get main light:
                        Light mainLight = GetMainLight(TransformWorldToShadowCoord(worldPos));

                        // direct specular term:
                        spec = DirectBDRF(brdfData, worldNormal, mainLight.direction, worldView);

                        // direct diffuse term:
                        half NdotL = saturate(dot(worldNormal, mainLight.direction));
                        half3 diffuse = mainLight.color * (mainLight.distanceAttenuation * mainLight.shadowAttenuation * NdotL);

                        // SH-based ambient term
                        half3 ambient = SampleSH(worldNormal) * _AmbientMultiplier;
                        
                        fo.color.rgb *= ambient + diffuse;

                    #endif

                    // Calculates refraction and absorption (using beer's law).
                    #if (FLUID_REFRACTION)

                        absorption = exp(-_AbsorptionCoeff * (1 - volume.rgb) * volume.a);
                        half3 refraction = SAMPLE_TEXTURE2D(_CameraOpaqueTexture,sampler_CameraOpaqueTexture,i.uv + eyeNormal.xy * volume.a * _RefractionCoeff) * absorption;
                                           
                        // lerp between diffuse and refraction using transparency:
                        fo.color.rgb = lerp(fo.color.rgb, refraction, _Transparency);

                    #endif

                    // Adds probe reflection:
                    #if (FLUID_REFLECTION)
                    
                        float roughness = 1 - _Smoothness;
                        half3 reflectVector = reflect(-worldView,worldNormal);
                        float3 reflection = GlossyEnvironmentReflection(reflectVector, roughness, 1);

                        reflection *= lerp(half3(1,1,1),volume.rgb, _Metalness);

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
                   half4 foam = SAMPLE_TEXTURE2D(_Foam,sampler_Foam,i.uv);
                   fo.color.rgb += foam.rgb * absorption;
                #endif
                
			    return fo;
			}
			ENDHLSL
		}
	}
}

Shader "Obi/Fluid/Colors/FluidColorsBlend"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        _BlendSrc ("BlendSrc", Float) = 0
        _BlendDst ("BlendDst", Float) = 0
        _ZWrite ("ZWrite", Float) = 0
    }

    SubShader { 

        Pass { 
            Name "FluidColors"
            Tags {"Queue"="Geometry" "IgnoreProjector"="True"}
            
            //Blend DstColor Zero
            //Blend SrcAlpha OneMinusSrcAlpha
            //ZWrite Off
            //ZWrite On

            Blend [_BlendSrc] [_BlendDst]
            ZWrite [_ZWrite]
            ColorMask RGB

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma fragmentoption ARB_precision_hint_fastest

            #include "ObiEllipsoids.cginc"
            #include "ObiFluids.cginc"

            fixed4 _ParticleColor;  

            struct vin{
                float4 vertex   : POSITION;
                float3 corner   : NORMAL;
                fixed4 color    : COLOR;
                
                float4 t0 : TEXCOORD0; // ellipsoid t1 vector
                float4 t1 : TEXCOORD1; // ellipsoid t2 vector
                float4 t2 : TEXCOORD2; // ellipsoid t3 vector
            };

            struct v2f
            {
                float4 pos   : POSITION;
                fixed4 color    : COLOR;

                float4 mapping  : TEXCOORD0;
                float4 viewRay : TEXCOORD1;
                float4 projPos : TEXCOORD2;
            };

            struct fout 
            {
                half4 color : SV_Target;
                float depth : SV_Depth;
            };

            v2f vert(vin v)
            { 
                float3x3 P, IP;
                BuildParameterSpaceMatrices(v.t0,v.t1,v.t2,P,IP);
            
                float3 worldPos;
                float3 view;
                float3 eye;
                float radius = BuildEllipsoidBillboard(v.vertex,v.corner,P,IP,worldPos,view,eye);
            
                v2f o;
                o.pos = mul(UNITY_MATRIX_VP, float4(worldPos,v.vertex.w));
                o.projPos = ComputeScreenPos(o.pos);
                o.mapping = float4(v.corner.xy,1/length(eye),radius); // A[1]
                o.viewRay.xyz = mul((float3x3)UNITY_MATRIX_V,view);                   // A[0]
                o.color = v.color * _ParticleColor;
                COMPUTE_EYEDEPTH(o.viewRay.w);
                return o;
            } 

            fout frag(v2f i)
            {
                fout fo;

                // discard fragment if occluded by the scene:
                float4 proj = UNITY_PROJ_COORD(i.projPos);
                float sceneDepth = Z2EyeDepth (SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, proj.xy/proj.w).r);

                if (sceneDepth < i.viewRay.w)
                    discard;

                float3 p,n;
                float thickness = IntersectEllipsoid(i.viewRay,i.mapping, float3(0,0,0),float3(0,0,0),p, n);
                
                fo.color = i.color;//lerp(half4(1,1,1,1),i.color,thickness); 
                fo.depth = OutputFragmentDepth(p);

                return fo;
            }
             
            ENDCG

        }
    } 
FallBack "Diffuse"
}

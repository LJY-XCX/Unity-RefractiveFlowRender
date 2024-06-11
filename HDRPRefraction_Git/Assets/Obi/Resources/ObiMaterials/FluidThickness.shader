Shader "Hidden/FluidThickness"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader { 

        Pass { 
            Name "FluidThickness"
            Tags {"Queue"="Geometry" "IgnoreProjector"="True"}
            
            Blend One One  
            ZWrite Off
            ColorMask A

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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
                float2 radius : TEXCOORD3;
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
                o.radius = float2(v.t0.w * _RadiusScale,0);
                o.color = v.color * _ParticleColor;

                COMPUTE_EYEDEPTH(o.viewRay.w);
                return o;
            } 

            float4 frag(v2f i) : SV_Target
            {
                float4 proj = UNITY_PROJ_COORD(i.projPos);
                float sceneDepth = Z2EyeDepth (SAMPLE_TEXTURE2D(_CameraDepthTexture,sampler_CameraDepthTexture,proj.xy/proj.w).r);

                // compare scene depth with particle depth:
                if (sceneDepth < i.viewRay.w)
                    discard;

                float3 p,n;
                float thickness = IntersectEllipsoid(i.viewRay.xyz,i.mapping, float3(0,0,0),float3(0,0,0),p, n);

                return thickness * i.radius.x * 2;
            }
             
            ENDCG

        }

        Pass { 
            Name "ThicknessHorizontalBlur"

            Cull Off ZWrite Off ZTest Always

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "ObiUtils.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            TEXTURE2D_HALF(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            half4 frag(v2f i) : SV_Target
            {
                float2 offset = float2(_MainTex_TexelSize.x,0);

                half4 sample1 = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv+offset*3) * .006;
                half4 sample2 = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv+offset*2) * .061;
                half4 sample3 = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv+offset) * .242;
                half4 sample4 = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv) * .383;
                half4 sample5 = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv-offset) * .242;
                half4 sample6 = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv-offset*2) * .061;
                half4 sample7 = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv-offset*3) * .006;

                return sample1 + sample2 + sample3 + sample4 + sample5 + sample6 + sample7;
            }
             
            ENDCG

        }

        Pass { 

            Name "ThicknessVerticalBlur"

            Cull Off ZWrite Off ZTest Always

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "ObiUtils.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            TEXTURE2D_HALF(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;  

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float2 offset = float2(0,_MainTex_TexelSize.y);

                half4 sample1 = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv+offset*3) * .006;
                half4 sample2 = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv+offset*2) * .061;
                half4 sample3 = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv+offset) * .242;
                half4 sample4 = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv) * .383;
                half4 sample5 = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv-offset) * .242;
                half4 sample6 = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv-offset*2) * .061;
                half4 sample7 = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv-offset*3) * .006;

                return sample1 + sample2 + sample3 + sample4 + sample5 + sample6 + sample7;
            }
             
            ENDCG

        }

    } 
FallBack "Diffuse"
}

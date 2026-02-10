Shader "Custom/GPUFrameAnimation"
{
Properties
    {
        [NoScaleOffset] _MainTex ("Main Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1,1,1,1) // 添加叠色属性
        _Columns ("Columns", Float) = 8
        _Rows ("Rows", Float) = 8
        _TotalFrames ("Total Frames", Float) = 64
        _FPS ("FPS", Float) = 30
        [Toggle] _Loop ("Is Loop", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color) // 添加到 Buffer
                UNITY_DEFINE_INSTANCED_PROP(float, _Columns)
                UNITY_DEFINE_INSTANCED_PROP(float, _Rows)
                UNITY_DEFINE_INSTANCED_PROP(float, _TotalFrames)
                UNITY_DEFINE_INSTANCED_PROP(float, _FPS)
                UNITY_DEFINE_INSTANCED_PROP(float, _Loop)
                UNITY_DEFINE_INSTANCED_PROP(float, _StartTime)
                UNITY_DEFINE_INSTANCED_PROP(float4, _PivotOffset)
            UNITY_INSTANCING_BUFFER_END(Props)

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.pos = UnityObjectToClipPos(v.vertex);
                
                float fps = UNITY_ACCESS_INSTANCED_PROP(Props, _FPS);
                float totalFrames = UNITY_ACCESS_INSTANCED_PROP(Props, _TotalFrames);
                float loop = UNITY_ACCESS_INSTANCED_PROP(Props, _Loop);
                float startTime = UNITY_ACCESS_INSTANCED_PROP(Props, _StartTime);
                float cols = UNITY_ACCESS_INSTANCED_PROP(Props, _Columns);
                float rows = UNITY_ACCESS_INSTANCED_PROP(Props, _Rows);
                
                
                float4 pivotOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _PivotOffset);
                // 在转剪裁空间之前，修改模型空间下的顶点位置
                float4 vPos = v.vertex;
                vPos.xy -= pivotOffset.xy;
                o.pos = UnityObjectToClipPos(vPos);

                // 计算相对时间：当前全局时间 - 记录的开始时间
                float relativeTime = max(0, _Time.y - startTime);
                float frameIndex = floor(relativeTime * fps);

                if (loop > 0.5) {
                    frameIndex = fmod(frameIndex, totalFrames);
                } else {
                    frameIndex = min(frameIndex, totalFrames - 1);
                }

                float2 size = float2(1.0 / cols, 1.0 / rows);
                float row = floor(frameIndex / cols);
                float col = fmod(frameIndex, cols);
                // UV 偏移 (标准 Top-Down 布局)
                float2 offset = float2(col * size.x, 1.0 - (row + 1) * size.y);
                
                o.uv = v.uv * size + offset;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                fixed4 tint = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                return tex2D(_MainTex, i.uv) * tint;
            }
            ENDCG
        }
    }
}
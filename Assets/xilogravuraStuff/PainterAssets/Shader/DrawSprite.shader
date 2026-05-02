Shader "Hidden/DrawSprite"
{
    Properties
    {
        _MainTex("Base", 2D) = "white" {}
        _Pos("Pos", Vector) = (0.5, 0.5, 0, 0)
        _Rotation("Rotation", Float) = 0
        _Scale("Scale", Vector) = (0.2, 0.2, 0, 0)
    }

        SubShader
        {
            Tags { "RenderType" = "Opaque" }
            Pass
            {
                ZTest Always Cull Off ZWrite Off
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                sampler2D _MainTex;
                sampler2D _SpriteTex;
                float2 _Pos;
                float _Rotation;
                float2 _Scale;

                struct v2f
                {
                    float2 uv : TEXCOORD0;
                    float4 vertex : SV_POSITION;
                };

                v2f vert(appdata_base v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = v.texcoord;
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    float2 delta = i.uv - _Pos.xy;

                    float cosR = cos(_Rotation);
                    float sinR = sin(_Rotation);
                    float2 rotatedDelta = float2(
                        delta.x * cosR + delta.y * sinR,
                       -delta.x * sinR + delta.y * cosR
                    );

                    float2 uv = rotatedDelta / _Scale.xy + 0.5;

                    if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1)
                        return tex2D(_MainTex, i.uv);

                    fixed4 spriteCol = tex2D(_SpriteTex, uv);
                    fixed4 baseCol = tex2D(_MainTex, i.uv);

                    spriteCol.rgb = fixed3(1, 1, 1);
                    return lerp(baseCol, spriteCol, spriteCol.a);
                }


                ENDCG
            }
        }
}

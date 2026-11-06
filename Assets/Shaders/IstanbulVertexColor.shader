Shader "Istanbul/VertexColorTex"
{
    Properties { _MainTex ("Texture", 2D) = "white" {} }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Lambert addshadow
        sampler2D _MainTex;
        struct Input { float2 uv_MainTex; float4 color : COLOR; };
        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 t = tex2D(_MainTex, IN.uv_MainTex);
            o.Albedo = t.rgb * IN.color.rgb;
        }
        ENDCG
    }
    FallBack "Diffuse"
}

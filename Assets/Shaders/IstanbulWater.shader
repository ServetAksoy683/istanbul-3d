Shader "Istanbul/Water"
{
    Properties
    {
        _Color ("Color", Color) = (0.07,0.30,0.45,0.85)
        _Spec ("Specular", Range(0.01,1)) = 0.8
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        CGPROGRAM
        #pragma surface surf BlinnPhong alpha:fade vertex:vert
        fixed4 _Color;
        half _Spec;
        struct Input { float3 worldPos; float3 viewDir; };
        void vert (inout appdata_full v)
        {
            float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
            v.vertex.y += sin(wp.x * 0.11 + _Time.y * 1.1) * 0.10 + cos(wp.z * 0.09 + _Time.y * 0.8) * 0.09;
        }
        void surf (Input IN, inout SurfaceOutput o)
        {
            float f = pow(1.0 - saturate(normalize(IN.viewDir).y), 3.0);
            o.Albedo = lerp(_Color.rgb, float3(0.55,0.76,0.90), f * 0.55);
            o.Alpha = lerp(_Color.a, 1.0, f * 0.35);
            o.Specular = _Spec;
            o.Gloss = 1.0;
            o.Normal = normalize(float3(sin(IN.worldPos.x * 0.35 + _Time.y * 1.7) * 0.14, cos(IN.worldPos.z * 0.30 + _Time.y * 1.3) * 0.14, 1.0));
        }
        ENDCG
    }
    FallBack "Transparent/Diffuse"
}

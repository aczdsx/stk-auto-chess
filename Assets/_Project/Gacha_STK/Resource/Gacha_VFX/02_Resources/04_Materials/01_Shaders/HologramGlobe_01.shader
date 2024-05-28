// Made with Amplify Shader Editor v1.9.1.5
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Gacha/HologramGlobe_01"
{
	Properties
	{
		[HDR]_Color_Main("Color_Main", Color) = (1,1,1,1)
		_Texture_Main("Texture_Main", 2D) = "white" {}
		_Texture_Main_Alpha("Texture_Main_Alpha", 2D) = "white" {}
		_Fresnel_Bias("Fresnel_Bias", Float) = 0
		_Fresnel_Scale("Fresnel_Scale", Float) = 1
		_Fresnel_Power("Fresnel_Power", Float) = 5
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Custom"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Back
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
		
		CGPROGRAM
		#pragma target 3.0
		#pragma surface surf Unlit keepalpha noshadow 
		struct Input
		{
			float3 worldPos;
			float3 worldNormal;
			float2 uv_texcoord;
		};

		uniform float _Fresnel_Bias;
		uniform float _Fresnel_Scale;
		uniform float _Fresnel_Power;
		uniform float4 _Color_Main;
		uniform sampler2D _Texture_Main;
		uniform float4 _Texture_Main_ST;
		uniform sampler2D _Texture_Main_Alpha;
		uniform float4 _Texture_Main_Alpha_ST;

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float3 ase_worldPos = i.worldPos;
			float3 ase_worldViewDir = normalize( UnityWorldSpaceViewDir( ase_worldPos ) );
			float3 ase_worldNormal = i.worldNormal;
			float fresnelNdotV5 = dot( ase_worldNormal, ase_worldViewDir );
			float fresnelNode5 = ( _Fresnel_Bias + _Fresnel_Scale * pow( 1.0 - fresnelNdotV5, _Fresnel_Power ) );
			float2 uv_Texture_Main = i.uv_texcoord * _Texture_Main_ST.xy + _Texture_Main_ST.zw;
			o.Emission = ( saturate( fresnelNode5 ) + ( _Color_Main * tex2D( _Texture_Main, uv_Texture_Main ) ) ).rgb;
			float2 uv_Texture_Main_Alpha = i.uv_texcoord * _Texture_Main_Alpha_ST.xy + _Texture_Main_Alpha_ST.zw;
			o.Alpha = tex2D( _Texture_Main_Alpha, uv_Texture_Main_Alpha ).r;
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19105
Node;AmplifyShaderEditor.SamplerNode;2;-884.3304,433.064;Inherit;True;Property;_Texture_Main_Alpha;Texture_Main_Alpha;2;0;Create;True;0;0;0;False;0;False;-1;None;0637e8551008bd84db93af010c0dd735;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;6;-586.5254,-219.9807;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;1;-1355.978,51.41609;Inherit;True;Property;_Texture_Main;Texture_Main;1;0;Create;True;0;0;0;False;0;False;-1;None;1c1efc5351864014a86cc2481abd737b;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;9;-1412.767,-432.7763;Inherit;False;Property;_Fresnel_Power;Fresnel_Power;6;0;Create;True;0;0;0;False;0;False;5;5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;8;-1410.768,-515.0929;Inherit;False;Property;_Fresnel_Scale;Fresnel_Scale;5;0;Create;True;0;0;0;False;0;False;1;5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;7;-1409.16,-596.4854;Inherit;False;Property;_Fresnel_Bias;Fresnel_Bias;4;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;4;-875.431,-2.850345;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;11;-768.8508,-425.595;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FresnelNode;5;-1111.392,-550.697;Inherit;True;Standard;WorldNormal;ViewDir;False;False;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;-1.562979,0;Float;False;True;-1;2;ASEMaterialInspector;0;0;Unlit;Gacha/HologramGlobe_01;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;2;False;;0;False;;False;0;False;;0;False;;False;0;Custom;0.5;True;False;0;True;Custom;;Transparent;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;False;2;5;False;;10;False;;0;5;False;;10;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;3;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.ColorNode;3;-1265.979,-131.5839;Inherit;False;Property;_Color_Main;Color_Main;0;1;[HDR];Create;True;0;0;0;False;0;False;1,1,1,1;0,2.041886,6,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
WireConnection;6;0;11;0
WireConnection;6;1;4;0
WireConnection;4;0;3;0
WireConnection;4;1;1;0
WireConnection;11;0;5;0
WireConnection;5;1;7;0
WireConnection;5;2;8;0
WireConnection;5;3;9;0
WireConnection;0;2;6;0
WireConnection;0;9;2;0
ASEEND*/
//CHKSM=E0454A1703B0725D1B3FB81E4DACFDAE9BD36045
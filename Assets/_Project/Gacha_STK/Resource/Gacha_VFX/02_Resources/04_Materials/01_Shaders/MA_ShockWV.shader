// Made with Amplify Shader Editor v1.9.1.5
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Gacha/MA_ShockWV"
{
	Properties
	{
		_Base_Tiling("Base_Tiling", Vector) = (1,1,0,0)
		_Noise_Intensity("Noise_Intensity", Float) = 0
		_Noise_Tiling("Noise_Tiling", Vector) = (1,1,0,0)
		_Noise_Speed("Noise_Speed", Vector) = (1,1,0,0)
		_Base("Base", 2D) = "white" {}
		_Noise("Noise", 2D) = "white" {}
		_Hardness("Hardness", Float) = 1
		_T_Mask_01("T_Mask_01", 2D) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Custom"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Off
		ZWrite Off
		Blend One One
		
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma surface surf Unlit keepalpha noshadow 
		#undef TRANSFORM_TEX
		#define TRANSFORM_TEX(tex,name) float4(tex.xy * name##_ST.xy + name##_ST.zw, tex.z, tex.w)
		struct Input
		{
			float4 vertexColor : COLOR;
			float4 uv_texcoord;
		};

		uniform sampler2D _Base;
		uniform float2 _Base_Tiling;
		uniform sampler2D _Noise;
		uniform float2 _Noise_Speed;
		uniform float2 _Noise_Tiling;
		uniform float _Noise_Intensity;
		uniform float _Hardness;
		uniform sampler2D _T_Mask_01;
		uniform float4 _T_Mask_01_ST;

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float4 appendResult17 = (float4(0.0 , i.uv_texcoord.z , 0.0 , 0.0));
			float2 uvs_TexCoord14 = i.uv_texcoord;
			uvs_TexCoord14.xy = i.uv_texcoord.xy * _Base_Tiling + appendResult17.xy;
			float2 uvs_TexCoord23 = i.uv_texcoord;
			uvs_TexCoord23.xy = i.uv_texcoord.xy * _Noise_Tiling;
			float2 panner20 = ( 1.0 * _Time.y * _Noise_Speed + uvs_TexCoord23.xy);
			float temp_output_25_0 = ( tex2D( _Noise, panner20 ).r * ( _Noise_Intensity * i.uv_texcoord.w ) );
			float2 uv_T_Mask_01 = i.uv_texcoord * _T_Mask_01_ST.xy + _T_Mask_01_ST.zw;
			float temp_output_31_0 = ( pow( tex2D( _Base, ( uvs_TexCoord14.xy + temp_output_25_0 ) ).r , _Hardness ) * tex2D( _T_Mask_01, uv_T_Mask_01 ).r );
			o.Emission = ( i.vertexColor * temp_output_31_0 ).rgb;
			o.Alpha = ( i.vertexColor * temp_output_31_0 ).r;
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19105
Node;AmplifyShaderEditor.DynamicAppendNode;17;-1051.601,716.9517;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;14;-829.037,524.9049;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;23;-1264.519,1036.052;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PannerNode;20;-1009.349,1091.446;Inherit;False;3;0;FLOAT2;1,1;False;2;FLOAT2;0,-1;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;24;-1241.519,1212.052;Inherit;False;Property;_Noise_Speed;Noise_Speed;3;0;Create;True;0;0;0;False;0;False;1,1;0,-0.5;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector2Node;22;-1434.646,1044.419;Inherit;False;Property;_Noise_Tiling;Noise_Tiling;2;0;Create;True;0;0;0;False;0;False;1,1;1,0.6;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;15;-1217.972,709.4125;Inherit;False;Constant;_Float0;Float 0;2;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;4;340.3323,-59.90082;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.VertexColorNode;6;-110.4932,-29.63126;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;13;-1240.34,487.3326;Inherit;False;Property;_Base_Tiling;Base_Tiling;0;0;Create;True;0;0;0;False;0;False;1,1;2,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SamplerNode;18;-814.4063,937.6676;Inherit;True;Property;_Noise;Noise;6;0;Create;True;0;0;0;False;0;False;-1;3fa82b83e62af2a48a904a4c96ae2f57;32c8311af713d3f4f87aac4bbdd73d06;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;10;-130.2056,516.1497;Inherit;True;Property;_Base;Base;5;0;Create;True;0;0;0;False;0;False;18;0429a070baa125b42a4651598f00fdd5;c7fae80a31549d641825e5bf4e212a2b;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;847.9434,-66.08461;Float;False;True;-1;2;ASEMaterialInspector;0;0;Unlit;Gacha/MA_ShockWV;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Off;2;False;;0;False;;False;0;False;;0;False;;False;0;Custom;0.5;True;False;0;True;Custom;;Transparent;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;False;4;1;False;;1;False;;0;5;False;;10;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;4;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.PowerNode;27;303.7429,473.3459;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;28;196.3907,641.5118;Inherit;False;Property;_Hardness;Hardness;7;0;Create;True;0;0;0;False;0;False;1;3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;19;-340.2989,489.5677;Inherit;True;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;29;-606.0708,703.5052;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SamplerNode;30;-47.11572,854.4847;Inherit;True;Property;_T_Mask_01;T_Mask_01;8;0;Create;True;0;0;0;False;0;False;-1;f31658cf307cf4b4aa4928fcf0a9c92c;f31658cf307cf4b4aa4928fcf0a9c92c;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;31;439.8843,692.4847;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;11;722.9213,130.1746;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;25;-374.5193,965.0519;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;32;-482.2021,1272.753;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;26;-663.3086,1171.058;Inherit;False;Property;_Noise_Intensity;Noise_Intensity;1;0;Create;True;0;0;0;False;0;False;0;0.25;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;16;-1304.616,793.3926;Inherit;False;0;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
WireConnection;17;0;15;0
WireConnection;17;1;16;3
WireConnection;14;0;13;0
WireConnection;14;1;17;0
WireConnection;23;0;22;0
WireConnection;20;0;23;0
WireConnection;20;2;24;0
WireConnection;4;0;6;0
WireConnection;4;1;31;0
WireConnection;18;1;20;0
WireConnection;10;1;19;0
WireConnection;0;2;4;0
WireConnection;0;9;11;0
WireConnection;27;0;10;1
WireConnection;27;1;28;0
WireConnection;19;0;14;0
WireConnection;19;1;25;0
WireConnection;29;1;25;0
WireConnection;31;0;27;0
WireConnection;31;1;30;1
WireConnection;11;0;6;0
WireConnection;11;1;31;0
WireConnection;25;0;18;1
WireConnection;25;1;32;0
WireConnection;32;0;26;0
WireConnection;32;1;16;4
ASEEND*/
//CHKSM=F83B57575BF20978B68862DC9360F958F66F228A
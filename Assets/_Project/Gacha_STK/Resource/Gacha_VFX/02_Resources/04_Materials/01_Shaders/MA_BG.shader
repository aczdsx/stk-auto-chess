// Made with Amplify Shader Editor v1.9.1.5
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Gacha/MA_BG"
{
	Properties
	{
		_MainTEX("MainTEX", 2D) = "white" {}
		_AddTEX("AddTEX", 2D) = "white" {}
		_NoiseTEX("NoiseTEX", 2D) = "white" {}
		[HDR]_Color_Main("Color_Main", Color) = (1,1,1,1)
		[HDR]_Color_Sub("Color_Sub", Color) = (1,1,1,1)
		_MainTEX_Tiling("MainTEX_Tiling", Vector) = (1,1,0,0)
		_MainTEX_Speed("MainTEX_Speed", Vector) = (0,1,0,0)
		_AddTEX_Intensity("AddTEX_Intensity", Float) = 0.1
		_AddTEX_Tiling("AddTEX_Tiling", Vector) = (1,1,0,0)
		_AddTEX_Speed("AddTEX_Speed", Vector) = (0,1.35,0,0)
		_NoiseTEX_Intensity("NoiseTEX_Intensity", Float) = 0.1
		_NoiseTEX_Tiling("NoiseTEX_Tiling", Vector) = (1,1,0,0)
		_NoiseTEX_Speed("NoiseTEX_Speed", Vector) = (0,1,0,0)
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] _texcoord2( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Custom"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Back
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
		BlendOp Add
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma surface surf Unlit keepalpha noshadow 
		#undef TRANSFORM_TEX
		#define TRANSFORM_TEX(tex,name) float4(tex.xy * name##_ST.xy + name##_ST.zw, tex.z, tex.w)
		struct Input
		{
			float4 vertexColor : COLOR;
			float2 uv_texcoord;
			float4 uv2_texcoord2;
		};

		uniform float4 _Color_Sub;
		uniform float4 _Color_Main;
		uniform sampler2D _AddTEX;
		uniform float2 _AddTEX_Speed;
		uniform float2 _AddTEX_Tiling;
		uniform float _AddTEX_Intensity;
		uniform sampler2D _MainTEX;
		uniform float2 _MainTEX_Speed;
		uniform float2 _MainTEX_Tiling;
		uniform sampler2D _NoiseTEX;
		uniform float2 _NoiseTEX_Speed;
		uniform float2 _NoiseTEX_Tiling;
		uniform float _NoiseTEX_Intensity;

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float2 uv_TexCoord59 = i.uv_texcoord * _AddTEX_Tiling;
			float2 panner57 = ( 1.0 * _Time.y * _AddTEX_Speed + uv_TexCoord59);
			float4 tex2DNode55 = tex2D( _AddTEX, panner57 );
			float2 uv_TexCoord13 = i.uv_texcoord * _MainTEX_Tiling;
			float2 panner25 = ( 1.0 * _Time.y * _MainTEX_Speed + uv_TexCoord13);
			float2 uv_TexCoord69 = i.uv_texcoord * _NoiseTEX_Tiling;
			float2 panner67 = ( 1.0 * _Time.y * _NoiseTEX_Speed + uv_TexCoord69);
			float temp_output_71_0 = ( ( tex2DNode55.r * _AddTEX_Intensity ) + ( tex2DNode55.r * ( tex2D( _MainTEX, ( panner25 + ( tex2D( _NoiseTEX, panner67 ).r * _NoiseTEX_Intensity ) ) ).r * 1.5 ) ) );
			float4 lerpResult77 = lerp( _Color_Sub , _Color_Main , temp_output_71_0);
			o.Emission = ( i.vertexColor * lerpResult77 ).rgb;
			float4 appendResult90 = (float4(0.0 , i.uv2_texcoord2.x , 0.0 , 0.0));
			float2 uv_TexCoord41 = i.uv_texcoord + appendResult90.xy;
			float temp_output_89_0 = saturate( uv_TexCoord41.y );
			float clampResult88 = clamp( saturate( ( temp_output_89_0 * temp_output_89_0 ) ) , 0.0 , 1.0 );
			o.Alpha = ( i.vertexColor.a * ( ( temp_output_71_0 * saturate( pow( ( 1.0 - pow( saturate( ( ( uv_TexCoord41.x * ( 1.0 - uv_TexCoord41.x ) ) * 3.6 ) ) , 3.0 ) ) , 5.0 ) ) ) * ( 1.0 - clampResult88 ) ) );
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19105
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;56;605.5459,-217.5859;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;57;-73.32135,-447.7503;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;59;-347.9846,-501.7878;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;65;-115.9724,244.4055;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;25;-563.0818,-63.12634;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;13;-837.745,-117.1638;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PannerNode;67;-659.8443,312.3364;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;69;-934.5075,258.299;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;26;-1043.082,-136.1263;Inherit;False;Property;_MainTEX_Tiling;MainTEX_Tiling;6;0;Create;True;0;0;0;False;0;False;1,1;2,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SamplerNode;55;119.5492,-445.2253;Inherit;True;Property;_AddTEX;AddTEX;2;0;Create;True;0;0;0;False;0;False;-1;4b39e0be71e3da540bedd37b3a0424d6;4b39e0be71e3da540bedd37b3a0424d6;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;73;899.7499,-460.566;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;60;-553.3216,-520.7502;Inherit;False;Property;_AddTEX_Tiling;AddTEX_Tiling;9;0;Create;True;0;0;0;False;0;False;1,1;2,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector2Node;58;-322.9846,-336.7878;Inherit;False;Property;_AddTEX_Speed;AddTEX_Speed;10;0;Create;True;0;0;0;False;0;False;0,1.35;0,2;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector2Node;16;-812.745,47.8362;Inherit;False;Property;_MainTEX_Speed;MainTEX_Speed;7;0;Create;True;0;0;0;False;0;False;0,1;0,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleAddOpNode;64;-62.5746,-60.85086;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;54;148.212,-86.7565;Inherit;True;Property;_MainTEX;MainTEX;1;0;Create;True;0;0;0;False;0;False;-1;f42508fd752e7ad4ca6674daf5a12add;f42508fd752e7ad4ca6674daf5a12add;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;72;478.1129,-654.8715;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;75;662.9552,-332.9574;Inherit;False;Property;_AddTEX_Intensity;AddTEX_Intensity;8;0;Create;True;0;0;0;False;0;False;0.1;0.15;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;70;-1139.844,239.3365;Inherit;False;Property;_NoiseTEX_Tiling;NoiseTEX_Tiling;12;0;Create;True;0;0;0;False;0;False;1,1;5,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector2Node;68;-909.5075,423.299;Inherit;False;Property;_NoiseTEX_Speed;NoiseTEX_Speed;13;0;Create;True;0;0;0;False;0;False;0,1;0.1,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;66;-313.3263,381.5497;Inherit;False;Property;_NoiseTEX_Intensity;NoiseTEX_Intensity;11;0;Create;True;0;0;0;False;0;False;0.1;0.015;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;76;498.6455,15.82642;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;1.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;71;1149.75,-286.5502;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;77;1742.904,-400.7878;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;63;-485.5927,149.0736;Inherit;True;Property;_NoiseTEX;NoiseTEX;3;0;Create;True;0;0;0;False;0;False;-1;1be8ef47e281ced469bc3e1e46c55d68;1be8ef47e281ced469bc3e1e46c55d68;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;44;354.7639,647.5848;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;45;549.4755,462.7285;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;47;821.3211,459.412;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;3.6;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;51;1856.267,453.4301;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;48;1189.443,450.2499;Inherit;True;False;2;0;FLOAT;0;False;1;FLOAT;3;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;49;1421.994,452.7394;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;50;1613.267,444.4301;Inherit;True;False;2;0;FLOAT;0;False;1;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;79;1043.51,492.4298;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;62;2020.856,-2.939199;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;38;1439.297,-519.0534;Inherit;False;Property;_Color_Main;Color_Main;4;1;[HDR];Create;True;0;0;0;False;0;False;1,1,1,1;0,18.88913,23.96862,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;78;1440.88,-706.9908;Inherit;False;Property;_Color_Sub;Color_Sub;5;1;[HDR];Create;True;0;0;0;False;0;False;1,1,1,1;0,0.2515087,4,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;2514.843,-233.7367;Float;False;True;-1;2;ASEMaterialInspector;0;0;Unlit;Gacha/MA_BG;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;2;False;;0;False;;False;0;False;;0;False;;False;0;Custom;0.5;True;False;0;True;Custom;;Transparent;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;False;1;5;False;;10;False;;0;5;False;;10;False;;1;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;0;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;83;2300.434,-167.7864;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;81;2020.434,-189.7864;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ComponentMaskNode;42;-22.49536,848.0813;Inherit;True;True;False;True;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;86;1353.578,871.6381;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;88;1544.578,999.6381;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;89;756.5198,985.0913;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;84;1101.511,811.3181;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;87;1772.66,794.3935;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;90;-327.4061,708.4214;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;41;-183.0851,576.0822;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;93;-524.9805,684.1749;Inherit;False;Constant;_Float0;Float 0;14;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;85;2267.979,283.9581;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;92;-629.587,778.2148;Inherit;False;1;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;94;2144.943,-431.2609;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
WireConnection;56;0;55;1
WireConnection;56;1;76;0
WireConnection;57;0;59;0
WireConnection;57;2;58;0
WireConnection;59;0;60;0
WireConnection;65;0;63;1
WireConnection;65;1;66;0
WireConnection;25;0;13;0
WireConnection;25;2;16;0
WireConnection;13;0;26;0
WireConnection;67;0;69;0
WireConnection;67;2;68;0
WireConnection;69;0;70;0
WireConnection;55;1;57;0
WireConnection;73;0;55;1
WireConnection;73;1;75;0
WireConnection;64;0;25;0
WireConnection;64;1;65;0
WireConnection;54;1;64;0
WireConnection;72;0;55;1
WireConnection;72;1;55;1
WireConnection;76;0;54;1
WireConnection;71;0;73;0
WireConnection;71;1;56;0
WireConnection;77;0;78;0
WireConnection;77;1;38;0
WireConnection;77;2;71;0
WireConnection;63;1;67;0
WireConnection;44;0;41;1
WireConnection;45;0;41;1
WireConnection;45;1;44;0
WireConnection;47;0;45;0
WireConnection;51;0;50;0
WireConnection;48;0;79;0
WireConnection;49;0;48;0
WireConnection;50;0;49;0
WireConnection;79;0;47;0
WireConnection;62;0;71;0
WireConnection;62;1;51;0
WireConnection;0;2;94;0
WireConnection;0;9;83;0
WireConnection;83;0;81;4
WireConnection;83;1;85;0
WireConnection;42;0;41;2
WireConnection;86;0;84;0
WireConnection;88;0;86;0
WireConnection;89;0;42;0
WireConnection;84;0;89;0
WireConnection;84;1;89;0
WireConnection;87;0;88;0
WireConnection;90;1;92;1
WireConnection;41;1;90;0
WireConnection;85;0;62;0
WireConnection;85;1;87;0
WireConnection;94;0;81;0
WireConnection;94;1;77;0
ASEEND*/
//CHKSM=C12341EF2368249BB182800F7CECB3D7B67AF2DF
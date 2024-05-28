// Made with Amplify Shader Editor v1.9.1.5
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Gacha/MA_Glitch"
{
	Properties
	{
		TEX_01("TEX_01", 2D) = "white" {}
		[HDR]_Color_Main("Color_Main", Color) = (1,1,1,1)
		_Glitch_Noise_TEX("Glitch_Noise_TEX", 2D) = "white" {}
		_Noise_TEX("Noise_TEX", 2D) = "white" {}
		_NoiseTEX_Intensity("NoiseTEX_Intensity", Float) = 0.1
		_GlitchNoise_Intensity("GlitchNoise_Intensity", Float) = 1
		_GlitchNoise_Tempo("GlitchNoise_Tempo", Float) = 1
		_GlitchTempo("GlitchTempo", Float) = 5
		_Alpha("Alpha", Float) = 0
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
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma surface surf Unlit keepalpha noshadow 
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform float4 _Color_Main;
		uniform sampler2D TEX_01;
		uniform sampler2D _Glitch_Noise_TEX;
		uniform float _GlitchNoise_Tempo;
		uniform float _GlitchNoise_Intensity;
		uniform float _GlitchTempo;
		uniform sampler2D _Noise_TEX;
		uniform float4 _Noise_TEX_ST;
		uniform float _NoiseTEX_Intensity;
		uniform float _Alpha;

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float2 temp_output_28_0_g1 = i.uv_texcoord;
			float mulTime59_g1 = _Time.y * 5.0;
			float4 _GlitchSpeed = float4(20,2,0,1);
			float4 appendResult2_g1 = (float4(0.0 , _GlitchSpeed.x , 0.0 , 0.0));
			float2 panner47_g1 = ( cos( mulTime59_g1 ) * appendResult2_g1.xy + float2( 0,0 ));
			float2 uv_TexCoord53_g1 = i.uv_texcoord * panner47_g1;
			float4 appendResult3_g1 = (float4(0.0 , _GlitchSpeed.y , 0.0 , 0.0));
			float2 panner5_g1 = ( sin( _Time.y ) * appendResult3_g1.xy + float2( 0,0 ));
			float2 uv_TexCoord54_g1 = i.uv_texcoord * panner5_g1 + panner5_g1;
			float2 uv_TexCoord8 = i.uv_texcoord * float2( 1,0.2 );
			float2 panner7 = ( 1.0 * _Time.y * float2( 1,1 ) + uv_TexCoord8);
			float mulTime11 = _Time.y * _GlitchNoise_Tempo;
			float clampResult15 = clamp( ( 1.0 - tex2D( _Glitch_Noise_TEX, panner7 ).r ) , sin( ( mulTime11 * _GlitchNoise_Intensity ) ) , 1.0 );
			float mulTime131 = _Time.y * _GlitchTempo;
			float temp_output_136_0 = saturate( ceil( ( sin( mulTime131 ) - 0.8 ) ) );
			float temp_output_23_0_g1 = ( ( ceil( sin( sin( uv_TexCoord53_g1.y ) ) ) + ceil( sin( sin( uv_TexCoord54_g1.y ) ) ) ) * ( clampResult15 * temp_output_136_0 ) );
			float4 appendResult31_g1 = (float4(( (temp_output_28_0_g1).x + temp_output_23_0_g1 ) , (temp_output_28_0_g1).y , 0.0 , 0.0));
			float2 uv_Noise_TEX = i.uv_texcoord * _Noise_TEX_ST.xy + _Noise_TEX_ST.zw;
			float4 lerpResult32_g1 = lerp( float4( temp_output_28_0_g1, 0.0 , 0.0 ) , appendResult31_g1 , ( tex2D( _Noise_TEX, uv_Noise_TEX ).r * _NoiseTEX_Intensity ));
			float4 tex2DNode6 = tex2D( TEX_01, lerpResult32_g1.xy );
			o.Emission = ( _Color_Main * tex2DNode6 ).rgb;
			o.Alpha = ( tex2DNode6.a * _Alpha );
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19105
Node;AmplifyShaderEditor.OneMinusNode;9;96.87598,933.1286;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;5;-200.124,891.1286;Inherit;True;Property;_Glitch_Noise_TEX;Glitch_Noise_TEX;2;0;Create;True;0;0;0;False;0;False;-1;1be8ef47e281ced469bc3e1e46c55d68;1be8ef47e281ced469bc3e1e46c55d68;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PannerNode;7;-416.124,900.1286;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;1,1;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;8;-638.6233,873.7295;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,0.2;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;106;533.343,590.955;Inherit;True;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;154;867.4208,333.2679;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;153;512.2311,256.3851;Inherit;True;Property;_Noise_TEX;Noise_TEX;3;0;Create;True;0;0;0;False;0;False;-1;3fa82b83e62af2a48a904a4c96ae2f57;3fa82b83e62af2a48a904a4c96ae2f57;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;152;644.2322,464.748;Inherit;False;Property;_NoiseTEX_Intensity;NoiseTEX_Intensity;4;0;Create;True;0;0;0;False;0;False;0.1;0.2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;15;439.876,980.1287;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;12;49.29116,1145.941;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;11;-158.124,1143.129;Inherit;False;1;0;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;157;1041.447,671.8873;Inherit;True;MF_Glitch;-1;;1;f644ba94f4871764db5965a265da29c2;0;3;33;FLOAT;0;False;28;FLOAT2;0,0;False;24;FLOAT;1;False;2;FLOAT4;35;FLOAT;25
Node;AmplifyShaderEditor.RangedFloatNode;156;-362.1989,1137.65;Inherit;False;Property;_GlitchNoise_Tempo;GlitchNoise_Tempo;6;0;Create;True;0;0;0;False;0;False;1;0.2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SinOpNode;132;368.3011,1513.73;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;133;482.9966,1503.756;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0.8;False;1;FLOAT;0
Node;AmplifyShaderEditor.CeilOpNode;134;641.3262,1514.977;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;131;191.2712,1508.744;Inherit;False;1;0;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SinOpNode;14;249.876,1154.129;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;136;837.1971,1514.726;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;135;1012.978,1390.383;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;158;808.3934,985.6415;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;155;-209.901,1242.414;Inherit;False;Property;_GlitchNoise_Intensity;GlitchNoise_Intensity;5;0;Create;True;0;0;0;False;0;False;1;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;159;-27.39935,1522.357;Inherit;True;Property;_GlitchTempo;GlitchTempo;7;0;Create;True;0;0;0;False;0;False;5;5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;161;1792.487,352.8259;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;160;1529.487,283.8259;Inherit;False;Property;_Color_Main;Color_Main;1;1;[HDR];Create;True;0;0;0;False;0;False;1,1,1,1;1,1,1,0.5607843;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;2092.574,368.1835;Float;False;True;-1;2;ASEMaterialInspector;0;0;Unlit;Gacha/MA_Glitch;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;2;False;;0;False;;False;0;False;;0;False;;False;0;Custom;0.5;True;False;0;True;Custom;;Transparent;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;False;2;5;False;;10;False;;0;5;False;;10;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;8;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.SamplerNode;6;1433.992,557.2558;Inherit;True;Property;TEX_01;TEX_01;0;0;Create;True;0;0;0;False;0;False;-1;fee6c60fbb37a374e80086220f6e83c4;fee6c60fbb37a374e80086220f6e83c4;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;162;1828.294,726.2437;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;164;1590.686,871.0902;Inherit;False;Property;_Alpha;Alpha;9;0;Create;True;0;0;0;False;0;False;0;1;0;0;0;1;FLOAT;0
WireConnection;9;0;5;1
WireConnection;5;1;7;0
WireConnection;7;0;8;0
WireConnection;154;0;153;1
WireConnection;154;1;152;0
WireConnection;15;0;9;0
WireConnection;15;1;14;0
WireConnection;12;0;11;0
WireConnection;12;1;155;0
WireConnection;11;0;156;0
WireConnection;157;33;154;0
WireConnection;157;28;106;0
WireConnection;157;24;158;0
WireConnection;132;0;131;0
WireConnection;133;0;132;0
WireConnection;134;0;133;0
WireConnection;131;0;159;0
WireConnection;14;0;12;0
WireConnection;136;0;134;0
WireConnection;135;1;136;0
WireConnection;158;0;15;0
WireConnection;158;1;136;0
WireConnection;161;0;160;0
WireConnection;161;1;6;0
WireConnection;0;2;161;0
WireConnection;0;9;162;0
WireConnection;6;1;157;35
WireConnection;162;0;6;4
WireConnection;162;1;164;0
ASEEND*/
//CHKSM=BFAE926C5BEBC27EFED18829D676BB90D2C2AD31
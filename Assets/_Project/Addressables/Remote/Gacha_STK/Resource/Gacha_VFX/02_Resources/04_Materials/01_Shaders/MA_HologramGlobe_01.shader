// Made with Amplify Shader Editor v1.9.1.5
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Gacha/MA_HologramGlobe_01"
{
	Properties
	{
		[HDR]_Color_Main("Color_Main", Color) = (1,1,1,1)
		_Color_Fresnel("Color_Fresnel", Color) = (1,1,1,1)
		[HDR]_Color_Main2("Color_Main2", Color) = (1,1,1,1)
		_Texture_Main("Texture_Main", 2D) = "white" {}
		_Fresnel_Bias("Fresnel_Bias", Float) = 0
		_Fresnel_Scale("Fresnel_Scale", Float) = 1
		_Fresnel_Power("Fresnel_Power", Float) = 5
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
		#pragma target 3.0
		#pragma surface surf Unlit keepalpha noshadow 
		struct Input
		{
			float4 vertexColor : COLOR;
			float3 worldPos;
			float3 worldNormal;
			float2 uv_texcoord;
			half ASEIsFrontFacing : VFACE;
		};

		uniform float4 _Color_Fresnel;
		uniform float _Fresnel_Bias;
		uniform float _Fresnel_Scale;
		uniform float _Fresnel_Power;
		uniform float4 _Color_Main;
		uniform sampler2D _Texture_Main;
		uniform float4 _Texture_Main_ST;
		uniform float4 _Color_Main2;

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
			float temp_output_17_0 = ( saturate( fresnelNode5 ) * 0.5 );
			float2 uv_Texture_Main = i.uv_texcoord * _Texture_Main_ST.xy + _Texture_Main_ST.zw;
			float4 tex2DNode1 = tex2D( _Texture_Main, uv_Texture_Main );
			float temp_output_23_0 = saturate( (i.ASEIsFrontFacing > 0 ? +1 : -1 ) );
			float4 lerpResult13 = lerp( ( _Color_Main * tex2DNode1 ) , ( _Color_Main2 * tex2DNode1 ) , temp_output_23_0);
			o.Emission = ( i.vertexColor * ( ( _Color_Fresnel * temp_output_17_0 ) + lerpResult13 ) ).rgb;
			o.Alpha = ( i.vertexColor.a * ( ( temp_output_17_0 + tex2DNode1.a ) * (i.ASEIsFrontFacing > 0 ? +1 : -1 ) ) );
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19105
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;16;-530.7291,-180.1372;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;13;-315.2393,-150.2125;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;20;-207.3939,152.9699;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;17;-501.7291,-466.1372;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;9;-1529.767,-573.7763;Inherit;False;Property;_Fresnel_Power;Fresnel_Power;8;0;Create;True;0;0;0;False;0;False;5;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;8;-1527.768,-656.0929;Inherit;False;Property;_Fresnel_Scale;Fresnel_Scale;7;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;7;-1526.16,-737.4854;Inherit;False;Property;_Fresnel_Bias;Fresnel_Bias;5;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FresnelNode;5;-1228.392,-691.697;Inherit;True;Standard;WorldNormal;ViewDir;False;False;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;11;-863.8508,-682.595;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;18;-899.7291,-537.1372;Inherit;False;Constant;_Fresnel_Intensity;Fresnel_Intensity;8;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;3;-1126.938,-277.5788;Inherit;False;Property;_Color_Main;Color_Main;0;1;[HDR];Create;True;0;0;0;False;0;False;1,1,1,1;1,1,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;22;-167.0935,-575.9561;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;21;-514.0935,-788.9561;Inherit;False;Property;_Color_Fresnel;Color_Fresnel;1;0;Create;True;0;0;0;False;0;False;1,1,1,1;0.07075471,0.3920828,1,0.1686275;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;6;-99.52539,-272.9807;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;23;-544.0935,260.0439;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;2;-884.3304,433.064;Inherit;True;Property;_Texture_Main_Alpha;Texture_Main_Alpha;4;0;Create;True;0;0;0;False;0;False;-1;None;b98230e600bc57d4f83e1a4f1771dd1b;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;15;-831.9437,-276.9495;Inherit;False;Property;_Color_Main2;Color_Main2;2;1;[HDR];Create;True;0;0;0;False;0;False;1,1,1,1;0,0.3664559,0.7490197,0.2392157;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;1;-1383.078,88.07954;Inherit;True;Property;_Texture_Main;Texture_Main;3;0;Create;True;0;0;0;False;0;False;-1;None;dabcbd0605f04c64583ffc35c6d7daaf;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;4;-652.7273,31.62266;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;24;-103.9775,444.3158;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;25;-250.604,314.992;Inherit;False;Constant;_Float0;Float 0;9;0;Create;True;0;0;0;False;0;False;-1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;26;164.9059,191.9279;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;27;69.90588,328.9279;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TwoSidedSign;12;-746.6397,278.2348;Inherit;False;0;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;554.4388,-61.96085;Float;False;True;-1;2;ASEMaterialInspector;0;0;Unlit;Gacha/MA_HologramGlobe_01;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Off;2;False;;0;False;;False;0;False;;0;False;;False;0;Custom;0.5;True;False;0;True;Custom;;Transparent;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;False;4;1;False;;1;False;;0;5;False;;10;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;6;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;29;446.3773,-271.5829;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.VertexColorNode;28;209.2424,-453.2481;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;30;410.3222,32.11623;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
WireConnection;16;0;15;0
WireConnection;16;1;1;0
WireConnection;13;0;4;0
WireConnection;13;1;16;0
WireConnection;13;2;23;0
WireConnection;20;0;17;0
WireConnection;20;1;1;4
WireConnection;17;0;11;0
WireConnection;17;1;18;0
WireConnection;5;1;7;0
WireConnection;5;2;8;0
WireConnection;5;3;9;0
WireConnection;11;0;5;0
WireConnection;22;0;21;0
WireConnection;22;1;17;0
WireConnection;6;0;22;0
WireConnection;6;1;13;0
WireConnection;23;0;12;0
WireConnection;4;0;3;0
WireConnection;4;1;1;0
WireConnection;24;1;25;0
WireConnection;24;2;23;0
WireConnection;26;0;20;0
WireConnection;26;1;12;0
WireConnection;27;0;24;0
WireConnection;0;2;29;0
WireConnection;0;9;30;0
WireConnection;29;0;28;0
WireConnection;29;1;6;0
WireConnection;30;0;28;4
WireConnection;30;1;26;0
ASEEND*/
//CHKSM=963FA246D29935004AF57BE0E90643FB5165217D
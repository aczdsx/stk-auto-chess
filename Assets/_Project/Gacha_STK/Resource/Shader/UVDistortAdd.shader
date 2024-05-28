// Made with Amplify Shader Editor v1.9.1.5
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "DistortUVAdd"
{
	Properties
	{
		[HDR]_Color("Color", Color) = (1,1,1,1)
		_MainTex("MainTex", 2D) = "white" {}
		_MainUV("MainUV", Vector) = (1,1,0,0)
		_MainSp("MainSp", Vector) = (0,0,0,0)
		_DisTex("DisTex", 2D) = "white" {}
		_DisUV("DisUV", Vector) = (1,1,0,0)
		_DisInt("DisInt", Range( 0 , 1)) = 0
		_AlphaTex("AlphaTex", 2D) = "white" {}
		_MainPow("MainPow", Range( 0 , 4)) = 1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Custom"  "Queue" = "Transparent+0" "IsEmissive" = "true"  }
		Cull Off
		ZWrite Off
		ZTest Always
		Blend One One
		
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma surface surf Unlit keepalpha noshadow noambient novertexlights nolightmap  nodynlightmap nodirlightmap nofog nometa noforwardadd 
		struct Input
		{
			float2 uv_texcoord;
			float4 vertexColor : COLOR;
		};

		uniform float4 _Color;
		uniform sampler2D _MainTex;
		uniform float2 _MainSp;
		uniform float4 _MainUV;
		uniform sampler2D _DisTex;
		uniform float4 _DisUV;
		uniform float _DisInt;
		uniform float _MainPow;
		uniform sampler2D _AlphaTex;
		uniform float4 _AlphaTex_ST;

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float2 appendResult60 = (float2(_MainUV.x , _MainUV.y));
			float2 appendResult61 = (float2(_MainUV.z , _MainUV.w));
			float2 appendResult114 = (float2(_DisUV.z , _DisUV.w));
			float2 appendResult113 = (float2(_DisUV.x , _DisUV.y));
			float2 uv_TexCoord63 = i.uv_texcoord * appendResult113;
			float2 panner95 = ( _Time.y * appendResult114 + uv_TexCoord63);
			float4 tex2DNode64 = tex2D( _DisTex, panner95 );
			float2 uv_TexCoord59 = i.uv_texcoord * appendResult60 + ( appendResult61 + ( ( tex2DNode64.r * tex2DNode64.a ) * _DisInt ) );
			float2 panner108 = ( _Time.y * _MainSp + uv_TexCoord59);
			float4 tex2DNode57 = tex2D( _MainTex, panner108 );
			float4 temp_output_62_0 = ( tex2DNode57 * tex2DNode57.a );
			float4 temp_cast_0 = (_MainPow).xxxx;
			float2 uv_AlphaTex = i.uv_texcoord * _AlphaTex_ST.xy + _AlphaTex_ST.zw;
			float4 tex2DNode104 = tex2D( _AlphaTex, uv_AlphaTex );
			o.Emission = ( pow( ( _Color * ( temp_output_62_0 * i.vertexColor ) ) , temp_cast_0 ) * ( ( temp_output_62_0 * i.vertexColor.a ) * ( tex2DNode104.r * tex2DNode104.a ) ) ).rgb;
			o.Alpha = 1;
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19105
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;48;0,0;Float;False;True;-1;2;ASEMaterialInspector;0;0;Unlit;DistortUVAdd;False;False;False;False;True;True;True;True;True;True;True;True;False;False;False;False;False;False;False;False;False;Off;2;False;;7;False;;False;0;False;;0;False;;False;0;Custom;0;True;False;0;True;Custom;;Transparent;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;False;4;1;False;;1;False;;0;5;False;;10;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;0;-1;-1;-1;0;False;0;0;False;_Float0;-1;0;False;_CutInt;0;0;0;False;0.1;False;;0;False;;False;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;100;-1036.73,-1.470039;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;103;-1036.625,290.8723;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;105;-606.3848,289.8586;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;109;-1039.777,-225.0211;Inherit;False;Property;_Color;Color;1;1;[HDR];Create;True;0;0;0;False;0;False;1,1,1,1;4,4,4,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;65;-2603.474,123.8823;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;60;-2889.702,-8.26132;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;61;-2892.702,119.7386;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector4Node;102;-3149.43,-10.33122;Inherit;False;Property;_MainUV;MainUV;3;0;Create;True;0;0;0;False;0;False;1,1,0,0;1,1,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PannerNode;108;-2183.043,-2.4196;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;107;-2455.128,153.3809;Inherit;False;Property;_MainSp;MainSp;4;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SamplerNode;57;-1971.592,-2.261311;Inherit;True;Property;_MainTex;MainTex;2;0;Create;True;0;0;0;False;0;False;-1;None;926126e2f7546334a8850370fda1503c;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;59;-2452.697,1.73868;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleTimeNode;96;-3767.566,861.6418;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;95;-3559.566,525.6419;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;63;-3799.566,525.6419;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;113;-3975.148,541.9629;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;114;-3977.148,689.9629;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector4Node;112;-4190.148,528.9629;Inherit;False;Property;_DisUV;DisUV;6;0;Create;True;0;0;0;False;0;False;1,1,0,0;1,0.5,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.VertexColorNode;68;-1300.908,286.227;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;104;-1301.574,495.6811;Inherit;True;Property;_AlphaTex;AlphaTex;8;0;Create;True;0;0;0;False;0;False;-1;None;f8f77cecd20b4dd4c87de491e5be52ff;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;117;-931.1182,499.9918;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;101;-3153.331,750.6685;Inherit;False;Property;_DisInt;DisInt;7;0;Create;True;0;0;0;False;0;False;0;0.1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;106;-2455.128,304.3808;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;64;-3335.566,521.6419;Inherit;True;Property;_DisTex;DisTex;5;0;Create;True;0;0;0;False;0;False;-1;None;59dd91873a88c1d40a07953f6f065490;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;111;-2963.148,526.9629;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;66;-2784.375,521.8821;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;62;-1585.192,-3.30138;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;121;-251.3381,124.6617;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;110;-750.7763,-2.021139;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.PowerNode;115;-511.6384,-0.6299133;Inherit;False;False;2;0;COLOR;0,0,0,0;False;1;FLOAT;1;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;116;-809.4243,130.0205;Inherit;False;Property;_MainPow;MainPow;9;0;Create;True;0;0;0;False;0;False;1;1;0;4;0;1;FLOAT;0
WireConnection;48;2;121;0
WireConnection;100;0;62;0
WireConnection;100;1;68;0
WireConnection;103;0;62;0
WireConnection;103;1;68;4
WireConnection;105;0;103;0
WireConnection;105;1;117;0
WireConnection;65;0;61;0
WireConnection;65;1;66;0
WireConnection;60;0;102;1
WireConnection;60;1;102;2
WireConnection;61;0;102;3
WireConnection;61;1;102;4
WireConnection;108;0;59;0
WireConnection;108;2;107;0
WireConnection;108;1;106;0
WireConnection;57;1;108;0
WireConnection;59;0;60;0
WireConnection;59;1;65;0
WireConnection;95;0;63;0
WireConnection;95;2;114;0
WireConnection;95;1;96;0
WireConnection;63;0;113;0
WireConnection;113;0;112;1
WireConnection;113;1;112;2
WireConnection;114;0;112;3
WireConnection;114;1;112;4
WireConnection;117;0;104;1
WireConnection;117;1;104;4
WireConnection;64;1;95;0
WireConnection;111;0;64;1
WireConnection;111;1;64;4
WireConnection;66;0;111;0
WireConnection;66;1;101;0
WireConnection;62;0;57;0
WireConnection;62;1;57;4
WireConnection;121;0;115;0
WireConnection;121;1;105;0
WireConnection;110;0;109;0
WireConnection;110;1;100;0
WireConnection;115;0;110;0
WireConnection;115;1;116;0
ASEEND*/
//CHKSM=A1E870EF78B13D5B13BBEF1248A50C96869A40D2
// Made with Amplify Shader Editor v1.9.1.5
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Gacha/MA_Trail"
{
	Properties
	{
		[HDR]_Color("Color", Color) = (1,1,1,1)
		_TEX("TEX", 2D) = "white" {}
		_T_Trail_01Copy("T_Trail_01 - Copy", 2D) = "white" {}
		_Panner("Panner", Vector) = (0,0,0,0)
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
		struct Input
		{
			float4 vertexColor : COLOR;
			float2 uv_texcoord;
		};

		uniform float4 _Color;
		uniform sampler2D _TEX;
		uniform float2 _Panner;
		uniform float4 _TEX_ST;
		uniform sampler2D _T_Trail_01Copy;
		uniform float4 _T_Trail_01Copy_ST;

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float2 uv_TEX = i.uv_texcoord * _TEX_ST.xy + _TEX_ST.zw;
			float2 panner8 = ( 1.0 * _Time.y * _Panner + uv_TEX);
			float2 uv_T_Trail_01Copy = i.uv_texcoord * _T_Trail_01Copy_ST.xy + _T_Trail_01Copy_ST.zw;
			float temp_output_9_0 = ( tex2D( _TEX, panner8 ).r * tex2D( _T_Trail_01Copy, uv_T_Trail_01Copy ).r );
			o.Emission = ( ( _Color * i.vertexColor ) * temp_output_9_0 ).rgb;
			o.Alpha = ( i.vertexColor.a * temp_output_9_0 );
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19105
Node;AmplifyShaderEditor.VertexColorNode;6;-645.9821,-309.6254;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;11;129.0725,338.9792;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;9;-186.2488,328.1096;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;1;-798.6855,131.8971;Inherit;True;Property;_TEX;TEX;1;0;Create;True;0;0;0;False;0;False;1;c7fae80a31549d641825e5bf4e212a2b;c7fae80a31549d641825e5bf4e212a2b;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;4;72.5881,-157.8988;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;583.7704,-64.60341;Float;False;True;-1;2;ASEMaterialInspector;0;0;Unlit;Gacha/MA_Trail;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Off;2;False;;0;False;;False;0;False;;0;False;;False;0;Custom;0.5;True;False;0;True;Custom;;Transparent;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;False;4;1;False;;1;False;;0;5;False;;10;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;2;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.SamplerNode;10;-546.2056,415.1497;Inherit;True;Property;_T_Trail_01Copy;T_Trail_01 - Copy;3;0;Create;True;0;0;0;False;0;False;10;0429a070baa125b42a4651598f00fdd5;0429a070baa125b42a4651598f00fdd5;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PannerNode;8;-1067.829,162.9441;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;-2,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;15;-1478.802,132.5872;Inherit;False;Property;_TEXTiling;TEXTiling;4;0;Create;True;0;0;0;False;0;False;0,0;1.5,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector2Node;16;-1255.393,355.0561;Inherit;False;Property;_Panner;Panner;5;0;Create;True;0;0;0;False;0;False;0,0;-4,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.ColorNode;3;-665.3434,-508.9152;Inherit;False;Property;_Color;Color;0;1;[HDR];Create;True;0;0;0;False;0;False;1,1,1,1;1.414214,1.414214,1.414214,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;17;-99.15863,-359.5843;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;12;-901.8022,375.5872;Inherit;False;0;10;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;13;-1298.802,130.5872;Inherit;False;0;1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
WireConnection;11;0;6;4
WireConnection;11;1;9;0
WireConnection;9;0;1;1
WireConnection;9;1;10;1
WireConnection;1;1;8;0
WireConnection;4;0;17;0
WireConnection;4;1;9;0
WireConnection;0;2;4;0
WireConnection;0;9;11;0
WireConnection;10;1;12;0
WireConnection;8;0;13;0
WireConnection;8;2;16;0
WireConnection;17;0;3;0
WireConnection;17;1;6;0
WireConnection;13;0;15;0
ASEEND*/
//CHKSM=03A91D8283FD9545F2ECFCD1D7E77EA9A0ABFE55
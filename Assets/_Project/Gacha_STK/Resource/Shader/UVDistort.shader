// Made with Amplify Shader Editor v1.9.1.5
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "DistortUV"
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
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Off
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma surface surf Unlit alpha:fade keepalpha noshadow noambient novertexlights nolightmap  nodynlightmap nodirlightmap nofog nometa noforwardadd 
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
			o.Emission = ( _Color * ( pow( temp_output_62_0 , temp_cast_0 ) * i.vertexColor ) ).rgb;
			float2 uv_AlphaTex = i.uv_texcoord * _AlphaTex_ST.xy + _AlphaTex_ST.zw;
			float4 tex2DNode104 = tex2D( _AlphaTex, uv_AlphaTex );
			o.Alpha = ( ( temp_output_62_0 * i.vertexColor.a ) * ( tex2DNode104.r * tex2DNode104.a ) ).r;
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19105
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;48;0,0;Float;False;True;-1;2;ASEMaterialInspector;0;0;Unlit;DistortUV;False;False;False;False;True;True;True;True;True;True;True;True;False;False;True;False;False;False;False;False;False;Off;0;False;;0;False;;False;0;False;;0;False;;False;0;Transparent;0;True;False;0;False;Transparent;;Transparent;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;False;2;5;False;;10;False;;2;5;False;;10;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;_Float0;-1;0;False;_CutInt;0;0;0;False;0.1;False;;0;False;;False;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;100;-713.5754,-10.41234;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;103;-713.4699,281.93;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;105;-283.2311,280.9163;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;110;-292.6226,-8.96344;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;109;-716.6226,-233.9634;Inherit;False;Property;_Color;Color;0;1;[HDR];Create;True;0;0;0;False;0;False;1,1,1,1;4,4,4,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;62;-1262.039,-12.24368;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;65;-2280.321,114.94;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;101;-2830.178,741.7262;Inherit;False;Property;_DisInt;DisInt;6;0;Create;True;0;0;0;False;0;False;0;0.1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;60;-2566.549,-17.20362;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;61;-2569.549,110.7963;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector4Node;102;-2826.277,-19.27352;Inherit;False;Property;_MainUV;MainUV;2;0;Create;True;0;0;0;False;0;False;1,1,0,0;1,1,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PannerNode;108;-1859.89,-11.3619;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleTimeNode;106;-2131.975,295.4385;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;107;-2131.975,144.4386;Inherit;False;Property;_MainSp;MainSp;3;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SamplerNode;57;-1648.439,-11.20361;Inherit;True;Property;_MainTex;MainTex;1;0;Create;True;0;0;0;False;0;False;-1;None;926126e2f7546334a8850370fda1503c;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;59;-2129.544,-7.203614;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;64;-3012.413,512.6996;Inherit;True;Property;_DisTex;DisTex;4;0;Create;True;0;0;0;False;0;False;-1;None;59dd91873a88c1d40a07953f6f065490;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleTimeNode;96;-3444.413,852.6996;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;95;-3236.413,516.6996;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;63;-3476.413,516.6996;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;111;-2639.995,518.0206;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;66;-2461.223,512.9398;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;113;-3651.995,533.0206;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;114;-3653.995,681.0206;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector4Node;112;-3866.995,520.0206;Inherit;False;Property;_DisUV;DisUV;5;0;Create;True;0;0;0;False;0;False;1,1,0,0;1,0.5,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.VertexColorNode;68;-977.7545,277.2847;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PowerNode;115;-943.9158,-5.738434;Inherit;False;False;2;0;COLOR;0,0,0,0;False;1;FLOAT;1;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;116;-1273.184,-151.7522;Inherit;False;Property;_MainPow;MainPow;8;0;Create;True;0;0;0;False;0;False;1;1;0;4;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;104;-978.4201,486.7388;Inherit;True;Property;_AlphaTex;AlphaTex;7;0;Create;True;0;0;0;False;0;False;-1;None;f8f77cecd20b4dd4c87de491e5be52ff;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;117;-607.9637,491.0495;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
WireConnection;48;2;110;0
WireConnection;48;9;105;0
WireConnection;100;0;115;0
WireConnection;100;1;68;0
WireConnection;103;0;62;0
WireConnection;103;1;68;4
WireConnection;105;0;103;0
WireConnection;105;1;117;0
WireConnection;110;0;109;0
WireConnection;110;1;100;0
WireConnection;62;0;57;0
WireConnection;62;1;57;4
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
WireConnection;64;1;95;0
WireConnection;95;0;63;0
WireConnection;95;2;114;0
WireConnection;95;1;96;0
WireConnection;63;0;113;0
WireConnection;111;0;64;1
WireConnection;111;1;64;4
WireConnection;66;0;111;0
WireConnection;66;1;101;0
WireConnection;113;0;112;1
WireConnection;113;1;112;2
WireConnection;114;0;112;3
WireConnection;114;1;112;4
WireConnection;115;0;62;0
WireConnection;115;1;116;0
WireConnection;117;0;104;1
WireConnection;117;1;104;4
ASEEND*/
//CHKSM=7C5A3A6FCF3F259D1B7AF0D96597D9572BF0D053
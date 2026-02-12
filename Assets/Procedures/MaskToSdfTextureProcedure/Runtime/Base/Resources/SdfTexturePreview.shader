/*
	Copyright � Carl Emil Carlsen 2021-2024
	http://cec.dk
*/

Shader "Unlit/SdfTexturePreview"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#include "UnityCG.cginc"

			struct ToVert
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct ToFrag
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _MainTex_TexelSize;


			ToFrag Vert( ToVert v )
			{
				ToFrag o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}


			half4 Frag( ToFrag i ) : SV_Target
			{
				float dist = tex2D( _MainTex, i.uv ).r;

				// Expect scaling to follow the convension of Unity's SDF Bake Tool.
				// "the underlying surface scales such that the largest side of the Texture is of length 1".
				// https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@15.0/manual/sdf-in-vfx-graph.html
				
				// Apply colors (blue is negative, red is positive).
				half4 col = half4( 0.05.xxx, 1 );
				if( dist > 0 ) col.r = 0.2 + dist;
				else col.b = 0.2 - dist;
				float t = 1 - abs( frac( dist * 10 ) * 2 - 1 );
				float fd = fwidth( t );
				t = 1 - smoothstep( -fd, fd, t );
				col.rgb += t;
				
				return col;
			}
			ENDCG
		}
	}
}
/*
	Copyright © Carl Emil Carlsen 2024
	http://cec.dk
*/

using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Simplex.Procedures
{
	public class Texture3DToTextureSliceProcedure
	{
		ComputeShader _computeShader;
		int _SliceKernel;

		RenderTexture _sliceTexture;

		Vector2Int _groupThreadCount;

		const int threadGroupWidth = 8; // Must match define in compute shader.


		public RenderTexture sliceTexture => _sliceTexture;

		static class ShaderIDs
		{
			public static readonly int _Tex3DRead = Shader.PropertyToID( nameof( _Tex3DRead ) );
			public static readonly int _SliceTex = Shader.PropertyToID( nameof( _SliceTex ) );
			public static readonly int _SliceResolution = Shader.PropertyToID( nameof( _SliceResolution ) );
			public static readonly int _SliceToTex3DMatrix = Shader.PropertyToID( nameof( _SliceToTex3DMatrix ) );
		}


		public Texture3DToTextureSliceProcedure()
		{
			_computeShader = Object.Instantiate( Resources.Load<ComputeShader>( nameof( Texture3DToTextureSliceProcedure ) ) );
			_computeShader.hideFlags = HideFlags.HideAndDontSave;

			_SliceKernel = _computeShader.FindKernel( nameof( _SliceKernel ) );
		}


		public void Update( Texture texture3D, Vector2Int sliceResolution, Matrix4x4 sliceLocalToWorld, Matrix4x4 texture3DWorldToLocal )
		{
			if( !_sliceTexture || _sliceTexture.width != sliceResolution.x || _sliceTexture.height != sliceResolution.y ) {
				_sliceTexture?.Release();
				_sliceTexture = CreateTexture( "SliceTexture", sliceResolution, texture3D.graphicsFormat );
				_computeShader.SetTexture( _SliceKernel, ShaderIDs._SliceTex, _sliceTexture );
				_computeShader.SetInts( ShaderIDs._SliceResolution, new int[] { sliceResolution.x, sliceResolution.y } );
				_groupThreadCount = new Vector2Int(
					Mathf.CeilToInt( sliceResolution.x / (float) threadGroupWidth ),
					Mathf.CeilToInt( sliceResolution.y / (float) threadGroupWidth )
				);
			}
		
			Matrix4x4 sliceToTex3D = texture3DWorldToLocal * sliceLocalToWorld;
			_computeShader.SetMatrix( ShaderIDs._SliceToTex3DMatrix, sliceToTex3D );
			_computeShader.SetTexture( _SliceKernel, ShaderIDs._Tex3DRead, texture3D );

			// Compute.
			_computeShader.Dispatch( _SliceKernel, _groupThreadCount.x, _groupThreadCount.y, 1 );
		}


		public void Release()
		{
			_sliceTexture?.Release();
		}


		static RenderTexture CreateTexture( string name, Vector2Int resolution, GraphicsFormat sourceFormat )
		{
			// Note: GraphicsFormatUtility is shit. For example, GraphicsFormatUtility.IsHalfFormat( GraphicsFormat.R16_UNorm ) returns false.
			
			GraphicsFormat format;
			if( GraphicsFormatUtility.IsCompressedFormat( sourceFormat ) )
			{
				switch( sourceFormat )
				{
					case GraphicsFormat.RGBA_DXT1_UNorm:
						format = GraphicsFormat.R8G8B8A8_UNorm;
						break;
						// TODO: handle other compressed cases ...
					default:
						throw new System.Exception( "Format needs support" );
				}
				//Debug.Log( sourceFormat + " -> " + format );
			} else {
				format = sourceFormat;
			}
			

			RenderTexture rt = new RenderTexture( resolution.x, resolution.y, 0, format, 0 );
			rt.name = name;
			rt.autoGenerateMips = false;
			rt.enableRandomWrite = true;
			rt.Create();
			return rt;
		}
	}
}
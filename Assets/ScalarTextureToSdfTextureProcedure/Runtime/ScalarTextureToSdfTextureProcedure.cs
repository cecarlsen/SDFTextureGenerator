/*
	Copyright © Carl Emil Carlsen 2021-2024
	http://cec.dk
*/

using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Simplex.Procedures
{
	public class ScalarTextureToSdfTextureProcedure
	{
		RenderTexture _sdfTexture;
		RenderTexture _floodTexture;

		ComputeShader _computeShader;
		int _SeedKernel;
		int _FloodKernel;
		int _DistKernel;
		int _ShowSeedsKernel;

		LocalKeyword _ADD_BORDERS;

		Vector2Int _groupThreadCount;

		const int threadGroupWidth = 8; // Must match define in compute shader.

		public RenderTexture sdfTexture => _sdfTexture;

		[System.Serializable] public enum DownSampling { None, Half, Quater }
		[System.Serializable] public enum Precision { _16, _32 }


		static class ShaderIDs
		{
			public static readonly int _SeedTexRead = Shader.PropertyToID( nameof( _SeedTexRead ) );
			public static readonly int _FloodTex = Shader.PropertyToID( nameof( _FloodTex ) );
			public static readonly int _FloodTexRead = Shader.PropertyToID( nameof( _FloodTexRead ) );
			public static readonly int _SdfTex = Shader.PropertyToID( nameof( _SdfTex ) );
			public static readonly int _Resolution = Shader.PropertyToID( nameof( _Resolution ) );
			public static readonly int _StepSize = Shader.PropertyToID( nameof( _StepSize ) );
			public static readonly int _SeedThreshold = Shader.PropertyToID( nameof( _SeedThreshold ) );
			public static readonly int _TexelSize = Shader.PropertyToID( nameof( _TexelSize ) );
		}


		public ScalarTextureToSdfTextureProcedure()
		{
			_computeShader = Object.Instantiate( Resources.Load<ComputeShader>( nameof( ScalarTextureToSdfTextureProcedure ) ) );
			_computeShader.hideFlags = HideFlags.HideAndDontSave;

			_SeedKernel = _computeShader.FindKernel( nameof( _SeedKernel ) );
			_FloodKernel = _computeShader.FindKernel( nameof( _FloodKernel ) );
			_DistKernel = _computeShader.FindKernel( nameof( _DistKernel ) );
			_ShowSeedsKernel = _computeShader.FindKernel( nameof( _ShowSeedsKernel ) );

			_ADD_BORDERS = new LocalKeyword( _computeShader, nameof( _ADD_BORDERS ) );
		}


		public void Update
		(
			Texture sourceTexture, float sourceValueThreshold, 
			DownSampling downSampling = DownSampling.None, Precision precision = Precision._32, bool addBorders = false,
			bool _showSource = false
		){

			if( !sourceTexture ) return;

			// Ensure and adapt resources.
			Vector2Int resolution = new Vector2Int( sourceTexture.width, sourceTexture.height );
			switch( downSampling ) {
				case DownSampling.Half: resolution /= 2; break;
				case DownSampling.Quater: resolution /= 4; break;
			}
			GraphicsFormat sdfFormat;
			switch( precision ) {
				case Precision._16: sdfFormat = GraphicsFormat.R16_SFloat; break;
				default: sdfFormat = GraphicsFormat.R32_SFloat; break;
			}
			if( !_sdfTexture || _sdfTexture.width != resolution.x || _sdfTexture.height != resolution.y || _sdfTexture.graphicsFormat != sdfFormat ) {
				_sdfTexture?.Release();
				_sdfTexture = CreateTexture( "SdfTexture", resolution, sdfFormat );
				_computeShader.SetTexture( _ShowSeedsKernel, ShaderIDs._SdfTex, _sdfTexture );
				_computeShader.SetTexture( _DistKernel, ShaderIDs._SdfTex, _sdfTexture );
			}
			if( !_floodTexture || _floodTexture.width != resolution.x || _floodTexture.height != resolution.y ) {
				_floodTexture?.Release();
				_floodTexture = CreateTexture( "FloodTexture", resolution, GraphicsFormat.R32G32B32A32_UInt );
				_computeShader.SetTexture( _SeedKernel, ShaderIDs._FloodTex, _floodTexture );
				_computeShader.SetTexture( _FloodKernel, ShaderIDs._FloodTex, _floodTexture );
				_computeShader.SetTexture( _DistKernel, ShaderIDs._FloodTexRead, _floodTexture );
				_computeShader.SetTexture( _ShowSeedsKernel, ShaderIDs._FloodTexRead, _floodTexture );
				_computeShader.SetInts( ShaderIDs._Resolution, new int[]{ resolution.x, resolution.y } );
				_computeShader.SetVector( ShaderIDs._TexelSize, _sdfTexture.texelSize );
				_groupThreadCount = new Vector2Int(
					Mathf.CeilToInt( resolution.x / (float) threadGroupWidth ),
					Mathf.CeilToInt( resolution.y / (float) threadGroupWidth )
				);
			}

			// Set keywords.
			if( _computeShader.IsKeywordEnabled( _ADD_BORDERS ) != addBorders ) _computeShader.SetKeyword( _ADD_BORDERS, addBorders );

			// Seed.
			_computeShader.SetTexture( _SeedKernel,  ShaderIDs._SeedTexRead, sourceTexture );
			_computeShader.SetFloat( ShaderIDs._SeedThreshold, sourceValueThreshold );
			_computeShader.Dispatch( _SeedKernel, _groupThreadCount.x, _groupThreadCount.y, 1 );

			// Show seeds.
			if( _showSource ) {
				_computeShader.Dispatch( _ShowSeedsKernel, _groupThreadCount.x, _groupThreadCount.y, 1 );
				return;
			}

			// Flood.
			int sizeMax = Mathf.Max( resolution.x, resolution.y );
			int stepMax = (int) Mathf.Log( Mathf.NextPowerOfTwo( sizeMax ), 2 ); // 2^c_maxSteps is max image size on x and y
			for( int n = stepMax; n >= 0; n-- ) {
				int stepSize = n > 0 ? (int) Mathf.Pow( 2, n ) : 1;
				_computeShader.SetInt( ShaderIDs._StepSize, stepSize );
				_computeShader.Dispatch( _FloodKernel, resolution.x, resolution.y, 1 );
			}

			// Compute SDF.
			_computeShader.Dispatch( _DistKernel, _groupThreadCount.x, _groupThreadCount.y, 1 );
		}


		public void Release()
		{
			_sdfTexture?.Release();
			_floodTexture?.Release();
			_sdfTexture = null;
			_floodTexture = null;
		}


		static RenderTexture CreateTexture( string name, Vector2Int resolution, GraphicsFormat format )
		{
			RenderTexture rt = new RenderTexture( resolution.x, resolution.y, 0, format, 0 );
			rt.name = name;
			rt.autoGenerateMips = false;
			rt.enableRandomWrite = true;
			rt.Create();
			return rt;
		}
	}
}
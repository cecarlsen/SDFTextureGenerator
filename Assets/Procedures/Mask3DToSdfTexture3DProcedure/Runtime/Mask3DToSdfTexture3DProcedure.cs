/*
	Copyright © Carl Emil Carlsen 2024
	http://cec.dk
*/

using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Simplex.Procedures
{
	public class Mask3DToSdfTexture3DProcedure
	{
		RenderTexture _sdfTexture;
		RenderTexture _floodTexture;

		ComputeShader _computeShader;
		int _SeedKernel;
		int _FloodKernel;
		int _DistKernel;
		int _ShowSeedsKernel;

		LocalKeyword _ADD_BORDERS;
		LocalKeyword[] _sourceScalarKeywords;

		Vector3Int _groupThreadCount;

		const int threadGroupLength = 8; // Must match define in compute shader.

		public RenderTexture sdfTexture => _sdfTexture;

		[System.Serializable] public enum DownSampling { None, Half, Quater }
		[System.Serializable] public enum Precision { _16, _32 }
		[System.Serializable] public enum TextureScalar { R, G, B, A, Luminance }

		static class ShaderIDs
		{
			public static readonly int _SeedTexRead = Shader.PropertyToID( nameof( _SeedTexRead ) );
			public static readonly int _FloodTex = Shader.PropertyToID( nameof( _FloodTex ) );
			public static readonly int _FloodTexRead = Shader.PropertyToID( nameof( _FloodTexRead ) );
			public static readonly int _SdfTex = Shader.PropertyToID( nameof( _SdfTex ) );
			public static readonly int _Resolution = Shader.PropertyToID( nameof( _Resolution ) );
			public static readonly int _StepSize = Shader.PropertyToID( nameof( _StepSize ) );
			public static readonly int _SeedThreshold = Shader.PropertyToID( nameof( _SeedThreshold ) );
		}


		public Mask3DToSdfTexture3DProcedure()
		{
			_computeShader = Object.Instantiate( Resources.Load<ComputeShader>( nameof( Mask3DToSdfTexture3DProcedure ) ) );
			_computeShader.hideFlags = HideFlags.HideAndDontSave;

			_SeedKernel = _computeShader.FindKernel( nameof( _SeedKernel ) );
			_FloodKernel = _computeShader.FindKernel( nameof( _FloodKernel ) );
			_DistKernel = _computeShader.FindKernel( nameof( _DistKernel ) );
			_ShowSeedsKernel = _computeShader.FindKernel( nameof( _ShowSeedsKernel ) );

			_ADD_BORDERS = new LocalKeyword( _computeShader, nameof( _ADD_BORDERS ) );
			var sourceScalarKeywordNames = System.Enum.GetNames( typeof( TextureScalar ) );
			_sourceScalarKeywords = new LocalKeyword[ sourceScalarKeywordNames.Length ];
			for( int sc = 0; sc < _sourceScalarKeywords.Length; sc++ ){
				_sourceScalarKeywords[ sc ] = new LocalKeyword( _computeShader, "_" + sourceScalarKeywordNames[ sc ].ToUpper() );
			}
		}


		public void Update
		(
			Texture sourceTexture, float sourceValueThreshold, 
			TextureScalar sourceScalar = TextureScalar.R, DownSampling downSampling = DownSampling.None, Precision precision = Precision._32, bool addBorders = false,
			bool _showSource = false
		){
			if( !sourceTexture ) return;
			if( sourceTexture.dimension != TextureDimension.Tex3D ) throw new System.Exception( "sourceTexture must be a 3D texture." );

			int resolutionZ = sourceTexture is RenderTexture ? ( sourceTexture as RenderTexture ).volumeDepth : ( sourceTexture as Texture3D ).depth;

			// Ensure and adapt resources.
			Vector3Int resolution = new Vector3Int( sourceTexture.width, sourceTexture.height, resolutionZ );
			switch( downSampling ) {
				case DownSampling.Half: resolution /= 2; break;
				case DownSampling.Quater: resolution /= 4; break;
			}
			GraphicsFormat sdfFormat;
			switch( precision ) {
				case Precision._16: sdfFormat = GraphicsFormat.R16_SFloat; break;
				default: sdfFormat = GraphicsFormat.R32_SFloat; break;
			}
			if( !_sdfTexture || _sdfTexture.width != resolution.x || _sdfTexture.height != resolution.y || _sdfTexture.volumeDepth != resolution.z || _sdfTexture.graphicsFormat != sdfFormat ) {
				_sdfTexture?.Release();
				_sdfTexture = CreateTexture3D( "SdfTexture", resolution, sdfFormat );
				_computeShader.SetTexture( _ShowSeedsKernel, ShaderIDs._SdfTex, _sdfTexture );
				_computeShader.SetTexture( _DistKernel, ShaderIDs._SdfTex, _sdfTexture );
			}
			if( !_floodTexture || _floodTexture.width != resolution.x || _floodTexture.height != resolution.y || _floodTexture.volumeDepth != resolution.z ) {
				_floodTexture?.Release();
				_floodTexture = CreateTexture3D( "FloodTexture", resolution, GraphicsFormat.R32G32B32A32_UInt );
				_computeShader.SetTexture( _SeedKernel, ShaderIDs._FloodTex, _floodTexture );
				_computeShader.SetTexture( _FloodKernel, ShaderIDs._FloodTex, _floodTexture );
				_computeShader.SetTexture( _DistKernel, ShaderIDs._FloodTexRead, _floodTexture );
				_computeShader.SetTexture( _ShowSeedsKernel, ShaderIDs._FloodTexRead, _floodTexture );
				_computeShader.SetInts( ShaderIDs._Resolution, new int[]{ resolution.x, resolution.y, resolution.z } );
				_groupThreadCount = new Vector3Int(
					Mathf.CeilToInt( resolution.x / (float) threadGroupLength ),
					Mathf.CeilToInt( resolution.y / (float) threadGroupLength ),
					Mathf.CeilToInt( resolution.z / (float) threadGroupLength )
				);
			}

			// Set keywords.
			if( _computeShader.IsKeywordEnabled( _ADD_BORDERS ) != addBorders ) _computeShader.SetKeyword( _ADD_BORDERS, addBorders );
			int sourceScalarIndex = (int) sourceScalar;
			for( int sc = 0; sc < _sourceScalarKeywords.Length; sc++ ){
				if( _computeShader.IsKeywordEnabled( _sourceScalarKeywords[ sc ] ) != ( sc == sourceScalarIndex ) ){
					_computeShader.SetKeyword( _sourceScalarKeywords[ sc ], sc == sourceScalarIndex );
				}
			}

			// Seed.
			_computeShader.SetTexture( _SeedKernel,  ShaderIDs._SeedTexRead, sourceTexture );
			_computeShader.SetFloat( ShaderIDs._SeedThreshold, sourceValueThreshold );
			_computeShader.Dispatch( _SeedKernel, _groupThreadCount.x, _groupThreadCount.y, _groupThreadCount.z );

			// Show seeds.
			if( _showSource ) {
				_computeShader.Dispatch( _ShowSeedsKernel, _groupThreadCount.x, _groupThreadCount.y, _groupThreadCount.z );
				return;
			}

			// Flood.
			int sizeMax = Mathf.Max( Mathf.Max( resolution.x, resolution.y ), resolution.z );
			int stepMax = (int) Mathf.Log( Mathf.NextPowerOfTwo( sizeMax ), 2 ); // 2^c_maxSteps is max image size on x and y
			for( int n = stepMax; n >= 0; n-- ) {
				int stepSize = n > 0 ? (int) Mathf.Pow( 2, n ) : 1;
				_computeShader.SetInt( ShaderIDs._StepSize, stepSize );
				_computeShader.Dispatch( _FloodKernel, resolution.x, resolution.y, resolution.z );
			}

			// Compute SDF.
			_computeShader.Dispatch( _DistKernel, _groupThreadCount.x, _groupThreadCount.y, _groupThreadCount.z );
		}


		public void Release()
		{
			_sdfTexture?.Release();
			_floodTexture?.Release();
			_sdfTexture = null;
			_floodTexture = null;
		}


		static Vector3 TexelSize3D( Texture t )
		{
			Vector3 texelSize = t.texelSize;
			int resolutionZ = t is RenderTexture ? ( t as RenderTexture ).volumeDepth : ( t as Texture3D ).depth;
			texelSize.z = 1f / resolutionZ;
			return texelSize;
		}

		static RenderTexture CreateTexture3D( string name, Vector3Int resolution, GraphicsFormat format )
		{
			RenderTexture rt = new RenderTexture( resolution.x, resolution.y, 0, format, 0 ){
				name = name,
				dimension = TextureDimension.Tex3D,
				volumeDepth = resolution.z,
				autoGenerateMips = false,
				enableRandomWrite = true,
			};
			
			rt.Create();
			return rt;
		}
	}
}
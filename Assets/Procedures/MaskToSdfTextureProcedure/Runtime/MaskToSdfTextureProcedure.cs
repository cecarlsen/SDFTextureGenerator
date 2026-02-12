/*
	Copyright © Carl Emil Carlsen 2021-2026
	http://cec.dk
*/

using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Simplex.Procedures
{
	public class MaskToSdfTextureProcedure
	{
		RenderTexture _sdfTexture;
		RenderTexture _floodTexture;
		RenderTexture _floodDoubleTexture;

		ComputeShader _computeShader;
		int _SeedKernel;
		int _FloodKernel;
		int _DistKernel;

		CommandBuffer _cmd;

		LocalKeyword _SUB_PIXEL_INTERPOLATION;
		LocalKeyword _DOUBLE_BUFFERING;
		LocalKeyword _ADD_BORDERS;
		LocalKeyword[] _sourceScalarKeywords;

		Vector2Int _groupThreadCount;
		bool _usingDoubleBuffering;

		const int threadGroupWidth = 8; // Must match define in compute shader.


		/// <summary>
		/// Resulting SDF texture. Before calling Update() this texture will be null.
		/// </summary>
		public RenderTexture sdfTexture => _sdfTexture;


		[System.Serializable] public enum DownSampling { None, Half, Quater, Eighth }
		[System.Serializable] public enum Precision { _16, _32 }
		[System.Serializable] public enum TextureScalar { R, G, B, A, Luminance }


		static class ShaderIDs
		{
			public static readonly int _SourceTexRead = Shader.PropertyToID( nameof( _SourceTexRead ) );
			public static readonly int _FloodTex = Shader.PropertyToID( nameof( _FloodTex ) );
			public static readonly int _FloodTexRead = Shader.PropertyToID( nameof( _FloodTexRead ) );
			public static readonly int _SdfTex = Shader.PropertyToID( nameof( _SdfTex ) );
			public static readonly int _Resolution = Shader.PropertyToID( nameof( _Resolution ) );
			public static readonly int _Threshold = Shader.PropertyToID( nameof( _Threshold ) );
			public static readonly int _JumpStep = Shader.PropertyToID( nameof( _JumpStep ) );
			public static readonly int _DownSamplingStep = Shader.PropertyToID( nameof( _DownSamplingStep ) );
		}


		public MaskToSdfTextureProcedure()
		{
			_computeShader = Object.Instantiate( Resources.Load<ComputeShader>( nameof( MaskToSdfTextureProcedure ) ) );
			_computeShader.hideFlags = HideFlags.HideAndDontSave;

			_SeedKernel = _computeShader.FindKernel( nameof( _SeedKernel ) );
			_FloodKernel = _computeShader.FindKernel( nameof( _FloodKernel ) );
			_DistKernel = _computeShader.FindKernel( nameof( _DistKernel ) );

			_SUB_PIXEL_INTERPOLATION = new LocalKeyword( _computeShader, nameof( _SUB_PIXEL_INTERPOLATION ) );
			_DOUBLE_BUFFERING = new LocalKeyword( _computeShader, nameof( _DOUBLE_BUFFERING ) );
			_ADD_BORDERS = new LocalKeyword( _computeShader, nameof( _ADD_BORDERS ) );
			var sourceScalarKeywordNames = System.Enum.GetNames( typeof( TextureScalar ) );
			_sourceScalarKeywords = new LocalKeyword[ sourceScalarKeywordNames.Length ];
			for( int sc = 0; sc < _sourceScalarKeywords.Length; sc++ ){
				_sourceScalarKeywords[ sc ] = new LocalKeyword( _computeShader, "_" + sourceScalarKeywordNames[ sc ].ToUpper() );
			}
		}


		/// <summary>
		/// Generates a Signed Distance Field (SDF) texture from a mask texture using the Jump Flooding algorithm.
		/// The resulting SDF texture stores normalized distances from each pixel to the nearest edge, with positive
		/// values outside the mask and negative values inside.
		/// </summary>
		/// <param name="sourceTexture">The source texture to generate the SDF from.</param>
		/// <param name="sourceValueThreshold">The threshold value that determines the boundary between inside and outside regions. Values above this threshold are considered inside.</param>
		/// <param name="sourceScalar">Which color channel to sample from the source texture (R, G, B, A, or Luminance).</param>
		/// <param name="downSampling">Downsampling factor to reduce the output resolution (None, Half, Quarter, or Eighth of source resolution).</param>
		/// <param name="precision">Bit depth of the output SDF texture (16-bit or 32-bit float).</param>
		/// <param name="useSubPixelInterpolation">When enabled, performs gradient-based subpixel interpolation for more accurate edge positions.</param>
		/// <param name="useDoubleBuffering">When enabled, an extra internal texture will be created to avoid read/write to the same texture. If you see jitter in your sdf between updates, enable this. Default is false</param>
		/// <param name="addBorders">When enabled, adds a border around the texture edges to ensure proper distance calculations at boundaries.</param>
		/// <param name="prioritizeMemoryUsageOverSpeed">When enabled, uses 16-bit float format for internal flood texture (64-bit total) instead of 32-bit (128-bit total), reducing memory usage at the cost of potential R/W performance.</param>
		public void Update
		(
			Texture sourceTexture, float sourceValueThreshold, 
			TextureScalar sourceScalar = TextureScalar.R, DownSampling downSampling = DownSampling.None, Precision precision = Precision._32,
			bool useSubPixelInterpolation = true, bool useDoubleBuffering = false, bool addBorders = false, bool prioritizeMemoryUsageOverSpeed = false
		){
			if( !sourceTexture ) return;

			// Ensure and adapt resources.
			if( _cmd == null ){
				_cmd = new CommandBuffer();
				_cmd.name = nameof( MaskToSdfTextureProcedure );
			}
			var resolution = new Vector2Int( sourceTexture.width, sourceTexture.height );
			int downSamplingStep = (int) Mathf.Pow( 2, (int) downSampling );
			resolution /= downSamplingStep;
			bool resize = !_sdfTexture || _sdfTexture.width != resolution.x || _sdfTexture.height != resolution.y;
			var sdfFormat = GetGraphicsFormat( precision );
			if( resize || _sdfTexture.graphicsFormat != sdfFormat ) {
				_sdfTexture?.Release();
				_sdfTexture = CreateTexture( "SdfTexture", resolution, sdfFormat );
				_computeShader.SetTexture( _DistKernel, ShaderIDs._SdfTex, _sdfTexture );
			}
			bool updateDoubleBuffering = useDoubleBuffering != _usingDoubleBuffering;
			bool rebuildCmd = resize || updateDoubleBuffering;
			var floodFormat = prioritizeMemoryUsageOverSpeed ? GraphicsFormat.R16G16B16A16_SFloat : GraphicsFormat.R32G32B32A32_SFloat; // 128bit -> more memory, potentially faster R/W alignment (Nvidia).
			bool recreateFloodTexture = resize || _floodTexture.graphicsFormat != floodFormat;
			if( recreateFloodTexture ){
				_floodTexture?.Release();
				_floodTexture = CreateTexture( "FloodTexture", resolution, floodFormat );
				_computeShader.SetTexture( _SeedKernel, ShaderIDs._FloodTex, _floodTexture );
			}
			if( updateDoubleBuffering || ( recreateFloodTexture && useDoubleBuffering ) ){
				_floodDoubleTexture?.Release();
				if( useDoubleBuffering ) _floodDoubleTexture = CreateTexture( "FloodDoubleTexture", resolution, floodFormat );
			}
			if( resize ){
				_groupThreadCount = new Vector2Int(
					Mathf.CeilToInt( resolution.x / (float) threadGroupWidth ),
					Mathf.CeilToInt( resolution.y / (float) threadGroupWidth )
				);
			}
			if( recreateFloodTexture || ( !useDoubleBuffering && updateDoubleBuffering ) ){
				_computeShader.SetTexture( _FloodKernel, ShaderIDs._FloodTex, _floodTexture );
				_computeShader.SetTexture( _DistKernel, ShaderIDs._FloodTexRead, _floodTexture );
			}
			_usingDoubleBuffering = useDoubleBuffering;

			// Set keywords.
			if( _computeShader.IsKeywordEnabled( _SUB_PIXEL_INTERPOLATION ) != useSubPixelInterpolation ) _computeShader.SetKeyword( _SUB_PIXEL_INTERPOLATION, useSubPixelInterpolation );
			if( _computeShader.IsKeywordEnabled( _DOUBLE_BUFFERING ) != useDoubleBuffering ) _computeShader.SetKeyword( _DOUBLE_BUFFERING, useDoubleBuffering );
			if( _computeShader.IsKeywordEnabled( _ADD_BORDERS ) != addBorders ) _computeShader.SetKeyword( _ADD_BORDERS, addBorders );
			int sourceScalarIndex = (int) sourceScalar;
			for( int sc = 0; sc < _sourceScalarKeywords.Length; sc++ ){
				if( _computeShader.IsKeywordEnabled( _sourceScalarKeywords[ sc ] ) != ( sc == sourceScalarIndex ) ){
					_computeShader.SetKeyword( _sourceScalarKeywords[ sc ], sc == sourceScalarIndex );
				}
			}

			// Set remaining input resources and constants.
			_computeShader.SetTexture( _SeedKernel,  ShaderIDs._SourceTexRead, sourceTexture );
			if( resize ){
				_computeShader.SetInts( ShaderIDs._Resolution, new int[]{ resolution.x, resolution.y } );
				_computeShader.SetInt( ShaderIDs._DownSamplingStep, downSamplingStep );
			}
			if( useSubPixelInterpolation ) _computeShader.SetTexture( _DistKernel,  ShaderIDs._SourceTexRead, sourceTexture );
			_computeShader.SetFloat( ShaderIDs._Threshold, sourceValueThreshold );

			// Build command buffer.
			if( rebuildCmd )
			{
				bool floodDoubleToggle = false;
				_cmd.Clear();

				// Seed.
				_cmd.DispatchCompute( _computeShader, _SeedKernel, _groupThreadCount.x, _groupThreadCount.y, 1 );

				// Flood.
				int sizeMax = Mathf.Max( resolution.x, resolution.y );
				int stepMax = (int) Mathf.Log( Mathf.NextPowerOfTwo( sizeMax ), 2 ); // 2^c_maxSteps is max image size on x and y.
				for( int n = stepMax; n >= 0; n-- ) {
					int jumpStep = n > 0 ? (int) Mathf.Pow( 2, n ) : 1;
					if( useDoubleBuffering ){
						_cmd.SetComputeTextureParam( _computeShader, _FloodKernel, ShaderIDs._FloodTexRead, floodDoubleToggle ? _floodDoubleTexture : _floodTexture );
						_cmd.SetComputeTextureParam( _computeShader, _FloodKernel, ShaderIDs._FloodTex, floodDoubleToggle ? _floodTexture : _floodDoubleTexture );
						floodDoubleToggle = !floodDoubleToggle;
					}
					_cmd.SetComputeIntParam( _computeShader, ShaderIDs._JumpStep, jumpStep );
					_cmd.DispatchCompute( _computeShader, _FloodKernel, resolution.x, resolution.y, 1 );
				}

				// Compute distances.
				if( useDoubleBuffering ) _cmd.SetComputeTextureParam( _computeShader, _DistKernel, ShaderIDs._FloodTexRead, floodDoubleToggle ? _floodDoubleTexture : _floodTexture );
				_cmd.DispatchCompute( _computeShader, _DistKernel, _groupThreadCount.x, _groupThreadCount.y, 1 );
			}

			// Execute!
			Graphics.ExecuteCommandBuffer( _cmd );
		}


		public void Release()
		{
			DestroyNicely( _computeShader );
			_sdfTexture?.Release();
			_floodTexture?.Release();
			_floodDoubleTexture?.Release();
			_computeShader = null;
			_sdfTexture = null;
			_floodTexture = null;
			_floodDoubleTexture = null;
		}


		static void DestroyNicely( Object o )
		{
			if( Application.isPlaying ) Object.Destroy( o );
			else Object.DestroyImmediate( o );
		}


		static RenderTexture CreateTexture( string name, Vector2Int resolution, GraphicsFormat format )
		{
			var rt = new RenderTexture( resolution.x, resolution.y, 0, format, 0 ){
				name = name,
				autoGenerateMips = false,
				enableRandomWrite = true,
			};
			rt.Create();
			return rt;
		}


		static GraphicsFormat GetGraphicsFormat( Precision precision )
		{
			switch( precision )
			{
				case Precision._16: return GraphicsFormat.R16_SFloat;
				default: return GraphicsFormat.R32_SFloat;
			}
		}
	}
}
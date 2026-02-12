/*
	Copyright © Carl Emil Carlsen 2024-2026
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

		Vector3Int _groupThreadCount;
		bool _usingDoubleBuffering;

		const int threadGroupLength = 8; // Must match define in compute shader.
		const GraphicsFormat floodFormat = GraphicsFormat.R32G32B32A32_UInt;

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
			public static readonly int _CoordPrecision = Shader.PropertyToID( nameof( _CoordPrecision ) );
		}


		public Mask3DToSdfTexture3DProcedure()
		{
			_computeShader = Object.Instantiate( Resources.Load<ComputeShader>( nameof( Mask3DToSdfTexture3DProcedure ) ) );
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
		public void Update
		(
			Texture sourceTexture, float sourceValueThreshold, 
			TextureScalar sourceScalar = TextureScalar.R, DownSampling downSampling = DownSampling.None, Precision precision = Precision._32,
			bool useSubPixelInterpolation = true, bool useDoubleBuffering = false, bool addBorders = false
		){
			if( !sourceTexture ) return;
			if( sourceTexture.dimension != TextureDimension.Tex3D ) throw new System.Exception( "sourceTexture must be a 3D texture." );

			// Ensure and adapt resources.
			if( _cmd == null ){
				_cmd = new CommandBuffer();
				_cmd.name = nameof( Mask3DToSdfTexture3DProcedure );
			}
			int resolutionZ = sourceTexture is RenderTexture ? ( sourceTexture as RenderTexture ).volumeDepth : ( sourceTexture as Texture3D ).depth;
			var resolution = new Vector3Int( sourceTexture.width, sourceTexture.height, resolutionZ );
			int downSamplingStep = (int) Mathf.Pow( 2, (int) downSampling );
			resolution /= downSamplingStep;
			bool resize = !_sdfTexture || _sdfTexture.width != resolution.x || _sdfTexture.height != resolution.y || _sdfTexture.volumeDepth != resolution.z;
			var sdfFormat = GetGraphicsFormat( precision );
			if( resize || _sdfTexture.graphicsFormat != sdfFormat ) {
				_sdfTexture?.Release();
				_sdfTexture = CreateTexture3D( "SdfTexture", resolution, sdfFormat );
				_computeShader.SetTexture( _DistKernel, ShaderIDs._SdfTex, _sdfTexture );
			}
			bool updateDoubleBuffering = useDoubleBuffering != _usingDoubleBuffering;
			bool rebuildCmd = resize || updateDoubleBuffering;
			if( resize ) {
				_floodTexture?.Release();
				_floodTexture = CreateTexture3D( "FloodTexture", resolution, floodFormat );
				_computeShader.SetTexture( _SeedKernel, ShaderIDs._FloodTex, _floodTexture );
			}
			if( updateDoubleBuffering || ( resize && useDoubleBuffering ) ){
				_floodDoubleTexture?.Release();
				if( useDoubleBuffering ) _floodDoubleTexture = CreateTexture3D( "FloodDoubleTexture", resolution, floodFormat );
			}
			if( resize ){
				_groupThreadCount = new Vector3Int(
					Mathf.CeilToInt( resolution.x / (float) threadGroupLength ),
					Mathf.CeilToInt( resolution.y / (float) threadGroupLength ),
					Mathf.CeilToInt( resolution.z / (float) threadGroupLength )
				);
			}
			if( resize || ( !useDoubleBuffering && updateDoubleBuffering ) ){
				_computeShader.SetTexture( _FloodKernel, ShaderIDs._FloodTex, _floodTexture );
				_computeShader.SetTexture( _DistKernel, ShaderIDs._FloodTexRead, _floodTexture );
			}
			_usingDoubleBuffering = useDoubleBuffering;
			int resMax = Mathf.Max( Mathf.Max( resolution.x, resolution.y ), resolution.z );
			int nearPow2 = Mathf.NextPowerOfTwo( resMax );
			const int precision16 = 65536; // We are storing each coord component in 16bits, so decimal precision is quite limited.
			int coordPrecition = precision16 / nearPow2; // Increase coordinate precision as texture resolution lowers. For a 1024 texture we have 64 (65536/1024) unique decimal values.

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
				_computeShader.SetInts( ShaderIDs._Resolution, new int[]{ resolution.x, resolution.y, resolution.z } );
				_computeShader.SetInt( ShaderIDs._DownSamplingStep, downSamplingStep );
			}
			_computeShader.SetFloat( ShaderIDs._Threshold, sourceValueThreshold );
			_computeShader.SetInt( ShaderIDs._CoordPrecision, coordPrecition );

			// Build command buffer.
			if( rebuildCmd )
			{
				bool floodDoubleToggle = false;
				_cmd.Clear();

				// Seed.
				_cmd.DispatchCompute( _computeShader, _SeedKernel, _groupThreadCount.x, _groupThreadCount.y, _groupThreadCount.z );

				// Flood.
				int sizeMax = Mathf.Max( Mathf.Max( resolution.x, resolution.y ), resolution.z );
				int stepMax = (int) Mathf.Log( Mathf.NextPowerOfTwo( sizeMax ), 2 ); // 2^c_maxSteps is max image size on x and y.
				for( int n = stepMax; n >= 0; n-- ) {
					int jumpStep = n > 0 ? (int) Mathf.Pow( 2, n ) : 1;
					if( useDoubleBuffering ){
						_cmd.SetComputeTextureParam( _computeShader, _FloodKernel, ShaderIDs._FloodTexRead, floodDoubleToggle ? _floodDoubleTexture : _floodTexture );
						_cmd.SetComputeTextureParam( _computeShader, _FloodKernel, ShaderIDs._FloodTex, floodDoubleToggle ? _floodTexture : _floodDoubleTexture );
						floodDoubleToggle = !floodDoubleToggle;
					}
					_cmd.SetComputeIntParam( _computeShader, ShaderIDs._JumpStep, jumpStep );
					_cmd.DispatchCompute( _computeShader, _FloodKernel, resolution.x, resolution.y, resolution.z );
				}

				// Compute distances.
				if( useDoubleBuffering ) _cmd.SetComputeTextureParam( _computeShader, _DistKernel, ShaderIDs._FloodTexRead, floodDoubleToggle ? _floodDoubleTexture : _floodTexture );
				_cmd.DispatchCompute( _computeShader, _DistKernel, _groupThreadCount.x, _groupThreadCount.y, _groupThreadCount.z );
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
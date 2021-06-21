/*
	Copyright © Carl Emil Carlsen 2021
	http://cec.dk
*/

using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class SDFTextureGenerator
{
	RenderTexture _sdfTexture;
	RenderTexture _jfaTexture;

	ComputeShader _computeShader;
	int _seedKernel;
	int _floodKernel;
	int _distKernel;
	int _showSeedsKernel;

	Vector2Int _groupThreadCount;

	bool _addBorderKeyWordEnabled;

	const int threadGroupWidth = 8; // Must match define in compute shader.
	const string addBorderKeyword = "ADD_BORDER";

	public RenderTexture sdfTexture => _sdfTexture;

	[SerializeField] public enum DownSampling { None, Half, Quater }
	[SerializeField] public enum Precision { _8, _16, _32 }

	static class ShaderIDs
	{
		public static readonly int seedTexRead = Shader.PropertyToID( "_SeedTexRead" );
		public static readonly int sdfTex = Shader.PropertyToID( "_SDFTex" );
		public static readonly int floodTex = Shader.PropertyToID( "_FloodTex" );
		public static readonly int floodTexRead = Shader.PropertyToID( "_FloodTexRead" );
		public static readonly int resolution = Shader.PropertyToID( "_Resolution" );
		public static readonly int stepSize = Shader.PropertyToID( "_StepSize" );
		public static readonly int seedThreshold = Shader.PropertyToID( "_SeedThreshold" );
		public static readonly int texelSize = Shader.PropertyToID( "_TexelSize" );
	}


	public void Update
	(
		Texture sourceTexture, float sourceValueThreshold, 
		DownSampling downSampling = DownSampling.None, Precision precision = Precision._32, bool addBorder = false,
		bool _showSource = false
	){

		if( !sourceTexture ) return;

		// Ensure resources.
		if( !_computeShader ) {
			_computeShader = Object.Instantiate( Resources.Load<ComputeShader>( nameof( SDFTextureGenerator ) + "Compute" ) );
			_computeShader.hideFlags = HideFlags.HideAndDontSave;
			_seedKernel = _computeShader.FindKernel( "Seed" );
			_floodKernel = _computeShader.FindKernel( "Flood" );
			_distKernel = _computeShader.FindKernel( "Dist" );
			_showSeedsKernel = _computeShader.FindKernel( "ShowSeeds" );
		}
		Vector2Int resolution = new Vector2Int( sourceTexture.width, sourceTexture.height );
		switch( downSampling ) {
			case DownSampling.Half: resolution /= 2; break;
			case DownSampling.Quater: resolution /= 4; break;
		}
		GraphicsFormat sdfFormat;
		switch( precision ) {
			case Precision._8: sdfFormat = GraphicsFormat.R8_UNorm; break;		// R8_SNorm is not supported for async download, so we use R8_UNorm.
			case Precision._16: sdfFormat = GraphicsFormat.R16_UNorm; break;	// ... and let the rest of the precision formats follow the same style.
			default: sdfFormat = GraphicsFormat.R32_SFloat; break;				// ... R32 does not have a UNorm version, so we choose SFloat and use as unsigned.
		}
		if( !_sdfTexture || _sdfTexture.width != resolution.x || _sdfTexture.height != resolution.y || _sdfTexture.graphicsFormat != sdfFormat ) {
			if( _sdfTexture ) _sdfTexture.Release();
			_sdfTexture = CreateTexture( "SDFTexture", resolution, sdfFormat );
			_computeShader.SetTexture( _showSeedsKernel, ShaderIDs.sdfTex, _sdfTexture );
			_computeShader.SetTexture( _distKernel, ShaderIDs.sdfTex, _sdfTexture );
		}
		if( !_jfaTexture || _jfaTexture.width != resolution.x || _jfaTexture.height != resolution.y ) {
			if( _jfaTexture ) _jfaTexture.Release();
			_jfaTexture = CreateTexture( "JFATexture", resolution, GraphicsFormat.R32G32B32A32_SInt );
			_computeShader.SetTexture( _seedKernel, ShaderIDs.floodTex, _jfaTexture );
			_computeShader.SetTexture( _floodKernel, ShaderIDs.floodTex, _jfaTexture );
			_computeShader.SetTexture( _distKernel, ShaderIDs.floodTexRead, _jfaTexture );
			_computeShader.SetTexture( _showSeedsKernel, ShaderIDs.floodTexRead, _jfaTexture );
			_computeShader.SetInts( ShaderIDs.resolution, new int[]{ resolution.x, resolution.y } );
			_computeShader.SetVector( ShaderIDs.texelSize, _sdfTexture.texelSize );
			_groupThreadCount = new Vector2Int(
				Mathf.CeilToInt( resolution.x / (float) threadGroupWidth ),
				Mathf.CeilToInt( resolution.y / (float) threadGroupWidth )
			);
		}

		if( addBorder && !_addBorderKeyWordEnabled ) _computeShader.EnableKeyword( addBorderKeyword );
		else if( !addBorder && _addBorderKeyWordEnabled ) _computeShader.DisableKeyword( addBorderKeyword );
		_addBorderKeyWordEnabled = addBorder;

		// Seed.
		_computeShader.SetTexture( _seedKernel,  ShaderIDs.seedTexRead, sourceTexture );
		_computeShader.SetFloat( ShaderIDs.seedThreshold, sourceValueThreshold );
		_computeShader.Dispatch( _seedKernel, _groupThreadCount.x, _groupThreadCount.y, 1 );

		// Show seeds.
		if( _showSource ) {
			_computeShader.Dispatch( _showSeedsKernel, _groupThreadCount.x, _groupThreadCount.y, 1 );
			return;
		}

		// Flood.
		int sizeMax = Mathf.Max( resolution.x, resolution.y );
		int stepMax = (int) Mathf.Log( Mathf.NextPowerOfTwo( sizeMax ), 2 ); // 2^c_maxSteps is max image size on x and y
		for( int n = stepMax; n >= 0; n-- ) {
			int stepSize = n > 0 ? (int) Mathf.Pow( 2, n ) : 1;
			_computeShader.SetInt( ShaderIDs.stepSize, stepSize );
			_computeShader.Dispatch( _floodKernel, resolution.x, resolution.y, 1 );
		}

		// Compute SDF.
		_computeShader.Dispatch( _distKernel, _groupThreadCount.x, _groupThreadCount.y, 1 );
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


	public void Release()
	{
		if( _sdfTexture ) _sdfTexture.Release();
		_sdfTexture = null;
	}
}
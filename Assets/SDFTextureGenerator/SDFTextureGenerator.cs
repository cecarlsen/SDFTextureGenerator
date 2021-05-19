/*
	Copyright © Carl Emil Carlsen 2021
	http://cec.dk
*/

using System.Collections;
using System.Collections.Generic;
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

	public RenderTexture sdfTexture => _sdfTexture;

	[SerializeField] public enum DownSampling { None, Half, Quater }
	[SerializeField] public enum Precision { _16, _32 }

	static class ShaderIDs
	{
		public static readonly int seedTexRead = Shader.PropertyToID( "_SeedTexRead" );
		public static readonly int sdfTex = Shader.PropertyToID( "_SDFTex" );
		public static readonly int floodTexRead = Shader.PropertyToID( "_FloodTexRead" );
		public static readonly int stepSize = Shader.PropertyToID( "_StepSize" );
		public static readonly int seedThreshold = Shader.PropertyToID( "_SeedThreshold" );
		public static readonly int texelSize = Shader.PropertyToID( "_TexelSize" );
	}


	public void Update
	(
		Texture sourceTexture, float sourceValueThreshold, 
		DownSampling downSampling = DownSampling.None, Precision precision = Precision._32, 
		bool _showSource = false
	){
		if( !sourceTexture ) return;

		if( !_computeShader ) {
			_computeShader = Object.Instantiate<ComputeShader>( Resources.Load<ComputeShader>( nameof( SDFTextureGenerator ) + "Compute" ) );
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
		GraphicsFormat sdfFormat = precision == Precision._32 ? GraphicsFormat.R32_SFloat : GraphicsFormat.R16_SFloat;
		if( !_sdfTexture || _sdfTexture.width != resolution.x || _sdfTexture.height != resolution.y || _sdfTexture.graphicsFormat != sdfFormat ) {
			if( _sdfTexture ) _sdfTexture.Release();
			_sdfTexture = new RenderTexture( resolution.x, resolution.y, 0, sdfFormat, 0 );
			_sdfTexture.name = "SDFTexture";
			_sdfTexture.autoGenerateMips = false;
			_sdfTexture.enableRandomWrite = true;
			_sdfTexture.Create();
		}
		if( !_jfaTexture || _jfaTexture.width != resolution.x || _jfaTexture.height != resolution.y ) {
			if( _jfaTexture ) _jfaTexture.Release();
			//_jfaTexture = new RenderTexture( resolution.x, resolution.y, 0, GraphicsFormat.R16G16B16A16_SInt, 0 );
			_jfaTexture = new RenderTexture( resolution.x, resolution.y, 0, RenderTextureFormat.ARGBInt, RenderTextureReadWrite.Linear );
			_jfaTexture.name = "JFATexture";
			_jfaTexture.autoGenerateMips = false;
			_jfaTexture.enableRandomWrite = true;
			_jfaTexture.Create();

			_computeShader.SetTexture( _seedKernel, "_FloodTex", _jfaTexture );
			_computeShader.SetTexture( _floodKernel, "_FloodTex", _jfaTexture );
			_computeShader.SetTexture( _distKernel, "_FloodTexRead", _jfaTexture );
		}

		_computeShader.SetTexture( _seedKernel, ShaderIDs.seedTexRead, sourceTexture );
		_computeShader.SetTexture( _distKernel, ShaderIDs.sdfTex, _sdfTexture );
		
		// Seed.
		_computeShader.SetTexture( _seedKernel,  ShaderIDs.seedTexRead, sourceTexture );
		_computeShader.SetVector( ShaderIDs.texelSize, _sdfTexture.texelSize );
		_computeShader.SetFloat( ShaderIDs.seedThreshold, sourceValueThreshold );
		_computeShader.Dispatch( _seedKernel, resolution.x, resolution.y, 1 );

		//Debug.Log( "Dispatch: " + resolution.x + "x" + resolution.y );

		// Show seeds.
		if( _showSource ) {
			_computeShader.SetTexture( _showSeedsKernel, ShaderIDs.floodTexRead, _jfaTexture );
			_computeShader.SetTexture( _showSeedsKernel, ShaderIDs.sdfTex, _sdfTexture );
			_computeShader.Dispatch( _showSeedsKernel, resolution.x, resolution.y, 1 );
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
		_computeShader.Dispatch( _distKernel, resolution.x, resolution.y, 1 );
	}


	public void Release()
	{
		if( _sdfTexture ) _sdfTexture.Release();
		_sdfTexture = null;
	}
}
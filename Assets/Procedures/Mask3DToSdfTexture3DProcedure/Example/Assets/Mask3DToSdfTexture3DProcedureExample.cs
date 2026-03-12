/*
	Copyright © Carl Emil Carlsen 2024-2026
	http://cec.dk
*/

using UnityEngine;
using UnityEngine.Events;

namespace Simplex.Procedures.Examples
{
	[ExecuteInEditMode]
	public class Mask3DToSdfTexture3DProcedureExample : MonoBehaviour
	{
		[SerializeField] Texture _sourceTexture3D = null;
		[SerializeField] float _sourceValueThreshold = 0.5f;
		[SerializeField] Mask3DToSdfTexture3DProcedure.TextureScalar _sourceChannel = Mask3DToSdfTexture3DProcedure.TextureScalar.R;
		[SerializeField] Mask3DToSdfTexture3DProcedure.DownSampling _downSampling = Mask3DToSdfTexture3DProcedure.DownSampling.None;
		[SerializeField] Mask3DToSdfTexture3DProcedure.Precision _precision = Mask3DToSdfTexture3DProcedure.Precision._32;
		[SerializeField] bool _useSubPixelInterpolation = true;
		[SerializeField] bool _useDoubleBuffering = false;
		[SerializeField,Tooltip("Note that border will eat up a one pixel border when useSubPixelInterpolation is disabled.")] bool _addBorders = false;

		[Header("Output")]
		[SerializeField] UnityEvent<RenderTexture> _sdfTexture3DEvent = new UnityEvent<RenderTexture>();

		[Header("Assets")]
		[SerializeField] ComputeShader _computeShaderAsset;

		Mask3DToSdfTexture3DProcedure _procedure;


		void OnEnable()
		{
			_procedure?.Release();
			_procedure = null;

			if( !_computeShaderAsset){
				Debug.LogError( "ComputeShader asset missing." );
				return;
			}

			_procedure = new Mask3DToSdfTexture3DProcedure( _computeShaderAsset );
		}


		void OnDisable()
		{
			_procedure?.Release();
		}


		void Reset()
		{
			_procedure?.Release();
		}


		void Update()
		{
			if( _procedure == null ) return;

			_procedure.Update(
				_sourceTexture3D, _sourceValueThreshold, _sourceChannel, _downSampling, _precision, 
				_useSubPixelInterpolation, _useDoubleBuffering, _addBorders
			);
			_sdfTexture3DEvent.Invoke( _procedure.sdfTexture );
		}
	}
}
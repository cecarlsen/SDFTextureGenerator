/*
	Copyright © Carl Emil Carlsen 2024
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
		[SerializeField] Mask3DToSdfTexture3DProcedure.SourceChannel _sourceChannel = Mask3DToSdfTexture3DProcedure.SourceChannel.R;
		[SerializeField] Mask3DToSdfTexture3DProcedure.DownSampling _downSampling = Mask3DToSdfTexture3DProcedure.DownSampling.None;
		[SerializeField] Mask3DToSdfTexture3DProcedure.Precision _precision = Mask3DToSdfTexture3DProcedure.Precision._32;
		[SerializeField] bool _addBorders = false;
		[SerializeField] bool _showSource = false;

		[Header("Output")]
		[SerializeField] UnityEvent<RenderTexture> _sdfTexture3DEvent = new UnityEvent<RenderTexture>();

		Mask3DToSdfTexture3DProcedure _procedure;


		void OnEnable()
		{
			_procedure?.Release();
			_procedure = new Mask3DToSdfTexture3DProcedure();
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
			_procedure.Update( _sourceTexture3D, _sourceValueThreshold, _sourceChannel, _downSampling, _precision, _addBorders, _showSource );
			_sdfTexture3DEvent.Invoke( _procedure.sdfTexture );
		}
	}
}
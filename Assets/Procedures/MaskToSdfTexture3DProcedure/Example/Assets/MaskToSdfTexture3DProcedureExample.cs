/*
	Copyright © Carl Emil Carlsen 2024
	http://cec.dk
*/

using UnityEngine;
using UnityEngine.Events;

namespace Simplex.Procedures.Examples
{
	[ExecuteInEditMode]
	public class MaskToSdfTexture3DProcedureExample : MonoBehaviour
	{
		[SerializeField] Texture _sourceTexture3D = null;
		[SerializeField] float _sourceValueThreshold = 0.5f;
		[SerializeField] MaskToSdfTexture3DProcedure.SourceChannel _sourceChannel = MaskToSdfTexture3DProcedure.SourceChannel.R;
		[SerializeField] MaskToSdfTexture3DProcedure.DownSampling _downSampling = MaskToSdfTexture3DProcedure.DownSampling.None;
		[SerializeField] MaskToSdfTexture3DProcedure.Precision _precision = MaskToSdfTexture3DProcedure.Precision._32;
		[SerializeField] bool _addBorders = false;
		[SerializeField] bool _showSource = false;

		[Header("Output")]
		[SerializeField] UnityEvent<RenderTexture> _sdfTexture3DEvent = new UnityEvent<RenderTexture>();

		MaskToSdfTexture3DProcedure _procedure;


		void OnEnable()
		{
			_procedure?.Release();
			_procedure = new MaskToSdfTexture3DProcedure();
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
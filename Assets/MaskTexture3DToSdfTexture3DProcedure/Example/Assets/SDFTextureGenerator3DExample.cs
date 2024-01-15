/*
	Copyright © Carl Emil Carlsen 2024
	http://cec.dk
*/

using UnityEngine;
using UnityEngine.Events;

namespace Simplex.Procedures.Examples
{
	[ExecuteInEditMode]
	public class MaskTexture3DToSdfTexture3DProcedureExample : MonoBehaviour
	{
		[SerializeField] Texture _sourceTexture3D = null;
		[SerializeField] float _sourceValueThreshold = 0.5f;
		[SerializeField] MaskTexture3DToSdfTexture3DProcedure.DownSampling _downSampling = MaskTexture3DToSdfTexture3DProcedure.DownSampling.None;
		[SerializeField] MaskTexture3DToSdfTexture3DProcedure.Precision _precision = MaskTexture3DToSdfTexture3DProcedure.Precision._32;
		[SerializeField] bool _addBorders = false;
		[SerializeField] bool _showSource = false;

		[Header("Output")]
		[SerializeField] UnityEvent<RenderTexture> _sdfTexture3DEvent = new UnityEvent<RenderTexture>();

		MaskTexture3DToSdfTexture3DProcedure _generator;


		void OnEnable()
		{
			_generator?.Release();
			_generator = new MaskTexture3DToSdfTexture3DProcedure();
		}


		void OnDisable()
		{
			_generator?.Release();
		}


		void Reset()
		{
			_generator?.Release();
		}


		void Update()
		{
			_generator.Update( _sourceTexture3D, _sourceValueThreshold, _downSampling, _precision, _addBorders, _showSource );
			_sdfTexture3DEvent.Invoke( _generator.sdfTexture );
		}
	}
}
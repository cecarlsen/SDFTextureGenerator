/*
	Copyright © Carl Emil Carlsen 2024
	http://cec.dk
*/

using UnityEngine;
using UnityEngine.Events;

namespace Simplex.Procedures.Examples
{
	[ExecuteInEditMode]
	public class ScalarTexture3DToSdfTexture3DProcedureExample : MonoBehaviour
	{
		[SerializeField] Texture _sourceTexture3D = null;
		[SerializeField] float _sourceValueThreshold = 0.5f;
		[SerializeField] ScalarTexture3DToSdfTexture3DProcedure.DownSampling _downSampling = ScalarTexture3DToSdfTexture3DProcedure.DownSampling.None;
		[SerializeField] ScalarTexture3DToSdfTexture3DProcedure.Precision _precision = ScalarTexture3DToSdfTexture3DProcedure.Precision._32;
		[SerializeField] bool _addBorders = false;
		[SerializeField] bool _showSource = false;

		[Header("Output")]
		[SerializeField] UnityEvent<RenderTexture> _sdfTexture3DEvent = new UnityEvent<RenderTexture>();

		ScalarTexture3DToSdfTexture3DProcedure _generator;


		void OnEnable()
		{
			_generator?.Release();
			_generator = new ScalarTexture3DToSdfTexture3DProcedure();
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
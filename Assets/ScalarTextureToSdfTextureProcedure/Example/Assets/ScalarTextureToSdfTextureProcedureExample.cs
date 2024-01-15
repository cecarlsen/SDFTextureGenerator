/*
	Copyright © Carl Emil Carlsen 2021-2024
	http://cec.dk
*/

using UnityEngine;
using UnityEngine.Events;

namespace Simplex.Procedures.Examples
{
	[ExecuteInEditMode]
	public class ScalarTextureToSdfTextureProcedureExample : MonoBehaviour
	{
		[SerializeField] Texture _sourceTexture = null;
		[SerializeField] float _sourceValueThreshold = 0.5f;
		[SerializeField] ScalarTextureToSdfTextureProcedure.DownSampling _downSampling = ScalarTextureToSdfTextureProcedure.DownSampling.None;
		[SerializeField] ScalarTextureToSdfTextureProcedure.Precision _precision = ScalarTextureToSdfTextureProcedure.Precision._32;
		[SerializeField] bool _addBorder = false;
		[SerializeField] bool _showSource = false;

		[Header("Output")]
		[SerializeField] UnityEvent<RenderTexture> _sdfTextureEvent = new UnityEvent<RenderTexture>();

		ScalarTextureToSdfTextureProcedure _generator;


		void OnEnable()
		{
			_generator?.Release();
			_generator = new ScalarTextureToSdfTextureProcedure();
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
			_generator.Update( _sourceTexture, _sourceValueThreshold, _downSampling, _precision, _addBorder, _showSource );
			_sdfTextureEvent.Invoke( _generator.sdfTexture );
		}
	}
}

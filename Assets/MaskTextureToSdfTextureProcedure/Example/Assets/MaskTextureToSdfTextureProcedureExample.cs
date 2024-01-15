/*
	Copyright © Carl Emil Carlsen 2021-2024
	http://cec.dk
*/

using UnityEngine;
using UnityEngine.Events;

namespace Simplex.Procedures.Examples
{
	[ExecuteInEditMode]
	public class MaskTextureToSdfTextureProcedureExample : MonoBehaviour
	{
		[SerializeField] Texture _sourceTexture = null;
		[SerializeField] float _sourceValueThreshold = 0.5f;
		[SerializeField] MaskTextureToSdfTextureProcedure.DownSampling _downSampling = MaskTextureToSdfTextureProcedure.DownSampling.None;
		[SerializeField] MaskTextureToSdfTextureProcedure.Precision _precision = MaskTextureToSdfTextureProcedure.Precision._32;
		[SerializeField] bool _addBorder = false;
		[SerializeField] bool _showSource = false;

		[Header("Output")]
		[SerializeField] UnityEvent<RenderTexture> _sdfTextureEvent = new UnityEvent<RenderTexture>();

		MaskTextureToSdfTextureProcedure _generator;


		void OnEnable()
		{
			_generator?.Release();
			_generator = new MaskTextureToSdfTextureProcedure();
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

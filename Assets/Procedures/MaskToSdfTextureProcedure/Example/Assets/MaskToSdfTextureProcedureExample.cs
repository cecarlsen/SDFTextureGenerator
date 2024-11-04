/*
	Copyright © Carl Emil Carlsen 2021-2024
	http://cec.dk
*/

using UnityEngine;
using UnityEngine.Events;

namespace Simplex.Procedures.Examples
{
	[ExecuteInEditMode]
	public class MaskToSdfTextureProcedureExample : MonoBehaviour
	{
		[SerializeField] Texture _sourceTexture = null;
		[SerializeField,Range(0f,1f)] float _sourceValueThreshold = 0.5f;
		[SerializeField] MaskToSdfTextureProcedure.TextureScalar _sourceChannel = MaskToSdfTextureProcedure.TextureScalar.R;
		[SerializeField] MaskToSdfTextureProcedure.DownSampling _downSampling = MaskToSdfTextureProcedure.DownSampling.None;
		[SerializeField] MaskToSdfTextureProcedure.Precision _precision = MaskToSdfTextureProcedure.Precision._32;
		[SerializeField] bool _addBorders = false;
		[SerializeField] bool _showSource = false;

		[Header("Output")]
		[SerializeField] UnityEvent<RenderTexture> _sdfTextureEvent = new UnityEvent<RenderTexture>();

		MaskToSdfTextureProcedure _procedure;


		void OnEnable()
		{
			_procedure?.Release();
			_procedure = new MaskToSdfTextureProcedure();
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
			_procedure.Update( _sourceTexture, _sourceValueThreshold, _sourceChannel, _downSampling, _precision, _addBorders, _showSource );
			_sdfTextureEvent.Invoke( _procedure.sdfTexture );
		}
	}
}

/*
	Copyright © Carl Emil Carlsen 2021
	http://cec.dk
*/

using UnityEngine;
using UnityEngine.Events;

[ExecuteInEditMode]
public class SDFTextureGeneratorExample : MonoBehaviour
{
	[SerializeField] Texture _sourceTexture = null;
	[SerializeField] float _sourceValueThreshold = 0.5f;
	[SerializeField] SDFTextureGenerator.DownSampling _downSampling = SDFTextureGenerator.DownSampling.None;
	[SerializeField] SDFTextureGenerator.Precision _precision = SDFTextureGenerator.Precision._32;
	[SerializeField] bool _addBorder = false;
	[SerializeField] bool _showSource = false;

	[Header("Output")]
	[SerializeField] UnityEvent<RenderTexture> _sdfTextureEvent = null;

	SDFTextureGenerator _generator;


	void OnEnable()
	{
		_generator = new SDFTextureGenerator();
	}


	void OnDisable()
	{
		_generator.Release();
	}


	void Update()
	{
		_generator.Update( _sourceTexture, _sourceValueThreshold, _downSampling, _precision, _addBorder, _showSource );
		_sdfTextureEvent.Invoke( _generator.sdfTexture );
	}
}
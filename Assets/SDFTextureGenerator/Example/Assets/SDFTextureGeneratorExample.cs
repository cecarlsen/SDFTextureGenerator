/*
	Copyright © Carl Emil Carlsen 2021
	http://cec.dk
*/

using UnityEngine;
using UnityEngine.Events;

public class SDFTextureGeneratorExample : MonoBehaviour
{
	[SerializeField] Texture _sourceTexture = null;
	[SerializeField] float _sourceValueThreshold = 0.5f;
	[SerializeField] SDFTextureGenerator.DownSampling _downSampling = SDFTextureGenerator.DownSampling.None;
	[SerializeField] SDFTextureGenerator.Precision _precision = SDFTextureGenerator.Precision._32;

	[Header("Output")]
	[SerializeField] UnityEvent<RenderTexture> _sdfTextureEvent = null;

	SDFTextureGenerator _generator;


	void Awake()
	{
		_generator = new SDFTextureGenerator();
	}


	void OnDestroy()
	{
		_generator.Release();
	}


	void Update()
	{
		_generator.Update( _sourceTexture, _sourceValueThreshold, _downSampling, _precision );
		_sdfTextureEvent.Invoke( _generator.sdfTexture );
	}
}
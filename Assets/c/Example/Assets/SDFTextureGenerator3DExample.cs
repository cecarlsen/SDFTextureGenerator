/*
	Copyright © Carl Emil Carlsen 2024
	http://cec.dk
*/

using UnityEngine;
using UnityEngine.Events;

[ExecuteInEditMode]
public class SDFTexture3DGeneratorExample : MonoBehaviour
{
	[SerializeField] Texture _sourceTexture3D = null;
	[SerializeField] float _sourceValueThreshold = 0.5f;
	[SerializeField] SDFTexture3DGenerator.DownSampling _downSampling = SDFTexture3DGenerator.DownSampling.None;
	[SerializeField] SDFTexture3DGenerator.Precision _precision = SDFTexture3DGenerator.Precision._32;
	[SerializeField] bool _addBorders = false;
	[SerializeField] bool _showSource = false;

	[Header("Output")]
	[SerializeField] UnityEvent<RenderTexture> _sdfTexture3DEvent = new UnityEvent<RenderTexture>();

	SDFTexture3DGenerator _generator;


	void OnEnable()
	{
		_generator?.Release();
		_generator = new SDFTexture3DGenerator();
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
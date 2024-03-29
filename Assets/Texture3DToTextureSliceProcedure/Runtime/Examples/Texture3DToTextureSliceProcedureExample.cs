/*
	Copyright © Carl Emil Carlsen 2024
	http://cec.dk
*/

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

namespace Simplex.Procedures.Examples
{
	[ExecuteInEditMode]
	public class Texture3DToTextureSliceProcedureExample : MonoBehaviour
	{
		[SerializeField] Texture _sourceTexture3D = null;
		[SerializeField] Transform _sourceTexture3DLocator = null;
		[SerializeField] Vector2Int _sliceResolution = new Vector2Int( 512, 512 );
		[SerializeField] UnityEvent<Texture> _sliceTextureEvent = new UnityEvent<Texture>();
		[SerializeField] bool _drawGizmosAlways = true;
		[SerializeField,Range(0,1)] float _volumeGizmoOpacity = 0.2f;

		Texture3DToTextureSliceProcedure _slicer;

		public Texture sourceTexture
		{
			get => _sourceTexture3D;
			set => _sourceTexture3D = value;
		}


		void OnEnable()
		{
			_slicer?.Release();
			_slicer = new Texture3DToTextureSliceProcedure();
		}


		void OnDisable()
		{
			_slicer?.Release();
		}


		void Update()
		{
			if( !_sourceTexture3D || _sourceTexture3D.dimension != TextureDimension.Tex3D ) return;

			Matrix4x4 texture3DWorldToLocal = _sourceTexture3DLocator ? _sourceTexture3DLocator.worldToLocalMatrix : Matrix4x4.identity;
			_slicer.Update( _sourceTexture3D, _sliceResolution, sliceLocalToWorld: transform.localToWorldMatrix, texture3DWorldToLocal );

			_sliceTextureEvent.Invoke( _slicer.sliceTexture );
		}


		public void SetAsMainTextureOnMeshRendererMaterial( Texture texture )
		{
			var meshRenderer = GetComponent<MeshRenderer>();
			if( !meshRenderer || !meshRenderer.sharedMaterial ) return;
			meshRenderer.sharedMaterial.mainTexture = texture;
		}


		void OnDrawGizmos()
		{
			if( _drawGizmosAlways ) DrawGizmos();
		}


		void OnDrawGizmosSelected()
		{
			if( !_drawGizmosAlways ) DrawGizmos();
		}


		void DrawGizmos()
		{
			if( _sourceTexture3D.dimension != TextureDimension.Tex3D ) return;

			Matrix4x4 texture3DLocalToWorld = _sourceTexture3DLocator ? _sourceTexture3DLocator.localToWorldMatrix : Matrix4x4.identity;

			Gizmos.matrix = texture3DLocalToWorld;
			Gizmos.DrawWireCube( Vector3.zero, Vector3.one );

			Gizmos.matrix = transform.localToWorldMatrix;
			Gizmos.DrawWireCube( Vector3.zero, new Vector3( 1, 1, 0 ) );

			Gizmos.matrix = Matrix4x4.identity;

#if UNITY_EDITOR
			if( _volumeGizmoOpacity > 0f )
			{
				int depth = _sourceTexture3D is RenderTexture ? (_sourceTexture3D as RenderTexture ).volumeDepth : ( _sourceTexture3D as Texture3D ).depth;
				float maxRes = Mathf.Max( Mathf.Max( _sourceTexture3D.width, _sourceTexture3D.height ), depth );
				UnityEditor.Handles.matrix = texture3DLocalToWorld * Matrix4x4.Scale( new Vector3( maxRes / (float) _sourceTexture3D.width, maxRes / _sourceTexture3D.height, maxRes / depth ) );
				UnityEditor.Handles.DrawTexture3DVolume( _sourceTexture3D, _volumeGizmoOpacity, qualityModifier: 0.5f, FilterMode.Point );
				UnityEditor.Handles.matrix = Matrix4x4.identity;
			}
#endif
		}
	}
}
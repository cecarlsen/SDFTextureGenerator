/*
	Copyright © Carl Emil Carlsen 2026
	http://cec.dk
*/

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Experimental.Rendering;

namespace Simplex.Procedures.Examples
{
	[ExecuteInEditMode]
	public class MaskToSdfTextureValueDisplay : MonoBehaviour
	{
		[SerializeField] Texture _sourceTexture = null;
		[SerializeField,Range(0f,1f)] float _sourceValueThreshold = 0.5f;
		[SerializeField] MaskToSdfTextureProcedure.TextureScalar _sourceChannel = MaskToSdfTextureProcedure.TextureScalar.R;
		[SerializeField] MaskToSdfTextureProcedure.DownSampling _downSampling = MaskToSdfTextureProcedure.DownSampling.None;
		[SerializeField] MaskToSdfTextureProcedure.Precision _precision = MaskToSdfTextureProcedure.Precision._32;
		[SerializeField] bool _useSubPixelInterpolation = true;
		[SerializeField] bool _useDoubleBuffering = true;
		[SerializeField] bool _addBorders = false;
		//[SerializeField] bool _showSource = false;
		[SerializeField] bool _animateThreshold = false;

		[Header("Gizmos")]
		[SerializeField] bool _showDistanceLabelGizmos = false;
		//[SerializeField] bool _showFloodLabelGizmos = false;

		[Header("Output")]
		[SerializeField] UnityEvent<RenderTexture> _sdfTextureEvent = new UnityEvent<RenderTexture>();

		MaskToSdfTextureProcedure _procedure;

		Texture2D _sdfReadbackTexture;
		Color[] _sdfColors;
		Texture2D _floodReadbackTexture;
		Color[] _floodColors;

		static readonly Color insideColor = new Color( 0.4f, 0.4f, 1f );
		static readonly Color outsideColor = new Color( 1f, 0.4f, 0.2f );


		void OnEnable()
		{
			_procedure?.Release();
			_procedure = new MaskToSdfTextureProcedure();
		}


		void OnDisable()
		{
			_procedure?.Release();
			if( _sdfReadbackTexture ) DestroyNicely( _sdfReadbackTexture );
		}


		void Reset()
		{
			_procedure?.Release();
		}


		void Update()
		{
			if( Application.isPlaying && _animateThreshold ) _sourceValueThreshold = Mathf.Lerp( 0.2f, 0.6f, Mathf.Sin( Time.time ) * 0.5f + 0.5f );

			_procedure.Update( _sourceTexture, _sourceValueThreshold, _sourceChannel, _downSampling, _precision, _useSubPixelInterpolation, _useDoubleBuffering, _addBorders );
			_sdfTextureEvent.Invoke( _procedure.sdfTexture );

			if( _showDistanceLabelGizmos ){
				Copy( _procedure.sdfTexture, ref _sdfReadbackTexture );
				_sdfColors = _sdfReadbackTexture.GetPixels();
			}

			//if( _showFloodLabelGizmos ){
			//	Copy( _procedure.debugFloodTexture, ref _floodReadbackTexture );
			//	_floodColors = _floodReadbackTexture.GetPixels();
			//}
		}


		void OnDrawGizmos()
		{
			if( !_procedure?.sdfTexture ) return;

			int w = _procedure.sdfTexture.width;
			int h = _procedure.sdfTexture.height;

			if( _showDistanceLabelGizmos && _sdfReadbackTexture ){
				int dimMax = Mathf.Max( w, h );
				#if UNITY_EDITOR
				for( int py = 0; py < h; py++ ){
					for( int px = 0; px < w; px++ ){
						int p = py * w + px;
						float sd = _sdfColors[ p ].r * dimMax;
						var center = new Vector2( px + 0.5f, py + 0.5f );
						UnityEditor.Handles.Label( center, new GUIContent( sd.ToString("F2") ) );
					}
				}
				#endif
				for( int py = 1; py < h-1; py++ ){
					for( int px = 1; px < w-1; px++ ){
						int p = py * w + px;
						int pE = p + 1;
						int pW = p - 1;
						int pN = p + w;
						int pS = p - w;
						float sd = _sdfColors[ p ].r;
						float sdE = _sdfColors[ pE ].r;
						float sdW = _sdfColors[ pW ].r;
						float sdN = _sdfColors[ pN ].r;
						float sdS = _sdfColors[ pS ].r;
						var normal = new Vector2( sdW - sdE, sdS - sdN );
						if( normal.sqrMagnitude > 0f ) normal.Normalize();
						var center = new Vector2( px + 0.5f, py + 0.5f );
						Gizmos.color = sd < 0f ? insideColor : outsideColor;
						Gizmos.DrawLine( center, center + normal * sd * dimMax );
						Gizmos.DrawSphere( center, 0.05f );
					}
				}
			}

			/*
			if( _showFloodLabelGizmos && _floodReadbackTexture )
			{
				#if UNITY_EDITOR
				for( int py = 0; py < h; py++ ){
					for( int px = 0; px < w; px++ ){
						int p = py * w + px;
						var c = _floodColors[ p ];
						var coordInside = new Vector2( c.r, c.g ) - Vector2.one;
						var coordOutside = new Vector2( c.b, c.a ) - Vector2.one;
						var center = new Vector2( px + 0.5f, py + 0.5f );
						bool validInside = coordInside.sqrMagnitude > 0f;
						bool validOutside = coordOutside.sqrMagnitude > 0f;
						if( validInside ) UnityEditor.Handles.Label( center, new GUIContent( $"{coordInside.x.ToString("F1")},{coordInside.y.ToString("F1")}"  ) );
						if( validOutside ) UnityEditor.Handles.Label( center, new GUIContent( $"{coordOutside.x.ToString("F1")},{coordOutside.y.ToString("F1")}"  ) );
						if( _showSource ){
							if( validInside ){
								Gizmos.color = new Color( insideColor.r, insideColor.g, insideColor.b, 0.5f );
								Gizmos.DrawLine( center, coordInside );
								Gizmos.DrawSphere( center, 0.05f );
							}
							if( validOutside ){
								Gizmos.color = new Color( outsideColor.r, outsideColor.g, outsideColor.b, 0.5f );
								Gizmos.DrawLine( center, coordOutside );
								Gizmos.DrawSphere( center, 0.05f );
							}
						}
					}
				}
				#endif
			}
			*/

			if( _showDistanceLabelGizmos )
			{
				Gizmos.color = new Color( 1, 1, 1, 0.3f );
				for( int py = 0; py <= h; py++ ) Gizmos.DrawLine( new Vector2( 0, py ), new Vector2( w, py  ) );
				for( int px = 0; px <= w; px++ ) Gizmos.DrawLine( new Vector2( px, 0 ), new Vector2( px, h  ) );
			}
		}


		static void DestroyNicely( Object o )
		{
			if( Application.isPlaying ) Destroy( o );
			else DestroyImmediate( o );
		}


		static void Copy( RenderTexture src, ref Texture2D dst )
		{
			int w = src.width;
			int h = src.height;
			var format = GraphicsFormatUtility.GetTextureFormat( src.graphicsFormat );
			if( !dst || dst.width !=  w ||dst.height !=  h || dst.format != format ){
				if( dst ) DestroyNicely( dst );
				dst = new Texture2D( w, h, format, mipChain: false );
			}
			
			Graphics.SetRenderTarget( src );
			dst.ReadPixels( new Rect( 0, 0, w, h ), 0, 0 );
			Graphics.SetRenderTarget( null );	
		}
	}
}
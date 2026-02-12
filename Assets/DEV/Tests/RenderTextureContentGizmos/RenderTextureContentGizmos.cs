/*
	Copyright © Carl Emil Carlsen 2026
	http://cec.dk
*/

using System.Text;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[ExecuteInEditMode]
public class RenderTextureContentGizmos : MonoBehaviour
{
	[SerializeField] RenderTexture _renderTexture = null;
	[SerializeField] bool _isSdfTexture = false;
	[SerializeField] bool _showGrid = true;
	[SerializeField] bool showRLabel = false;
	[SerializeField] bool showGLabel = false;
	[SerializeField] bool showBLabel = false;
	[SerializeField] bool showALabel = false;

	Texture2D _readbackTexture;
	Color[] _colors;

	static readonly Color insideColor = new Color( 0.4f, 0.4f, 1f );
	static readonly Color outsideColor = new Color( 1f, 0.4f, 0.2f );


	public RenderTexture renderTexture
	{
		get { return _renderTexture; }
		set { _renderTexture = value; }
	}


	void Update()
	{
		Copy( _renderTexture, ref _readbackTexture );
		_colors = _readbackTexture.GetPixels();
	}


	void OnDrawGizmos()
	{
		if( !_renderTexture ) return;

		int w = _renderTexture.width;
		int h = _renderTexture.height;
		int dimMax = Mathf.Max( w, h );

		Gizmos.matrix = transform.localToWorldMatrix;

		#if UNITY_EDITOR
		if( _readbackTexture && showRLabel || showRLabel || showBLabel || showALabel )
		{
			UnityEditor.Handles.matrix = transform.localToWorldMatrix;
			var sb = new StringBuilder();
			for( int py = 0; py < h; py++ ){
				for( int px = 0; px < w; px++ ){
					int p = py * w + px;
					var c = _colors[ p ];
					if( _isSdfTexture ) c *= dimMax;
					sb.Clear();
					if( showRLabel ) sb.Append( c.r.ToString("F2") );
					if( showGLabel ) sb.Append( sb.Length > 0 ? ", " : "" + c.g.ToString("F2") );
					if( showBLabel ) sb.Append( sb.Length > 0 ? ", " : "" + c.b.ToString("F2") );
					if( showALabel ) sb.Append( sb.Length > 0 ? ", " : "" + c.a.ToString("F2") );
					var center = new Vector2( px + 0.5f, py + 0.5f );
					UnityEditor.Handles.Label( center, new GUIContent( sb.ToString() ) );
				}
			}
		}
		#endif

		if( _isSdfTexture ){
			for( int py = 1; py < h-1; py++ ){
				for( int px = 1; px < w-1; px++ ){
					int p = py * w + px;
					int pE = p + 1;
					int pW = p - 1;
					int pN = p + w;
					int pS = p - w;
					float sd = _colors[ p ].r;
					float sdE = _colors[ pE ].r;
					float sdW = _colors[ pW ].r;
					float sdN = _colors[ pN ].r;
					float sdS = _colors[ pS ].r;
					var normal = new Vector2( sdW - sdE, sdS - sdN );
					if( normal.sqrMagnitude > 0f ) normal.Normalize();
					var center = new Vector2( px + 0.5f, py + 0.5f );
					Gizmos.color = sd < 0f ? insideColor : outsideColor;
					Gizmos.DrawLine( center, center + normal * sd * dimMax );
					Gizmos.DrawSphere( center, 0.05f );
				}
			}
		}

		if( _showGrid )
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
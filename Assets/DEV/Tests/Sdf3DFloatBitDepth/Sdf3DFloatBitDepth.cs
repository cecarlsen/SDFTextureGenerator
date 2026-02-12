/*
	Copyright © Carl Emil Carlsen 2026
	http://cec.dk
*/
using UnityEngine;
using static UnityEngine.Mathf;

[ExecuteInEditMode]
public class Sdf3DFloatBitDepth : MonoBehaviour
{
	public Vector3Int resolution = new Vector3Int( 512, 512, 512 );


	void Update()
	{
		int resMax = Max( Max( resolution.x, resolution.y ), resolution.z );
		int nearPow2 = NextPowerOfTwo( resMax );
		const int precision16 = 65536; // We are storing each coord component in 16bits.
		int coordPrecition = precision16 / nearPow2;
		Debug.Log( $"nearPow2: {nearPow2}, coordPrecition: {coordPrecition}\n" );

		// We use a R32G32B32A32_Uint texture (128 bits per pixel).
		long memoryUsageBits = 128L * ( resolution.x * resolution.y * resolution.z );
		double memoryUsageGb = memoryUsageBits / 8.0 / 1000000000.0;
		Debug.Log( $"memoryUsageGb: {memoryUsageGb.ToString("F1")}\n" );
	}
}
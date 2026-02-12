# Changelog


## [1.1.0] - 2026-02-12

	- Added useSubPixelInterpolation option. When enabled, performs gradient-based subpixel interpolation for more accurate edge positions.
	- Added optional double buffering to remove jitter (GPU race conditions).
	- Added DownSampling.Eighth option.
	- Fixed minor inside/outside distance conflict. Outside distance values represented distances to nearest inside pixels, and vise versa – not distances to the border between inside and outside, which lie between pixels. To compensate, distances has been reduced by 0.5 pixel.
	- Utilized CommandBuffer for a minor performance gain.
	- Fixed sampling reading bias towards 0,0.
	- Improved comments.
	- Removed show seeds option. This was mostly for development debugging purposes.


## [1.0.2] - 2024-11-04

	- Renamed ScalarTexture3DToSdfTexture3DProcedure to Mask3DToSdfTexture3DProcedure.
	- Added TextureScalar parameter with options R, G, B, A, and Luminance.


## [1.0.1] - 2024-01-15

- Changed distance scaling to follow the [convention](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@15.0/manual/sdf-in-vfx-graph.html) of Unity's SDF Bake Tool: "the underlying surface scales such that the largest side of the Texture is of length 1".
- Changed name to Scalarexture3DToSdfTexture3DProcedure and put inside namespace Simplex.Procedures.


## [1.0.0] - 2024-01-14

- Initial public version.

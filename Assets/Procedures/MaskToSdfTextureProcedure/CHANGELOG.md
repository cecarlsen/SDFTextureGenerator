# Changelog


## [1.0.6] - 2024-10-29

	- Renamed ScalarTextureToSdfTextureProcedure to MaskToSdfTextureProcedure.
	- Added SourceChannel parameter with options R, G, B, A, and Luminance.


## [1.0.5] - 2024-01-15

	- Changed distance scaling to follow the [convention](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@15.0/manual/sdf-in-vfx-graph.html) of Unity's SDF Bake Tool: "the underlying surface scales such that the largest side of the Texture is of length 1".
	- Changed name to ScalarTextureToSdfTextureProcedure and put inside namespace Simplex.Procedures.


## [1.0.4] - 2024-01-14

	- Refactoring.


## [1.0.3] - 2021-06-21

	- Added support for 8-bit texture.
	- Changed distance values to unsigned. To read: sd = ( sample * 2 - 1 ) * max(width,height).


## [1.0.2] - 2021-05-26

	- Distance values are now normalized by the max dimension of the texture.
	- Added option for adding border.


## [1.0.1] - 2021-05-23

	- Refactoring and minor compute shader optimisation (group thread size).


## [1.0.0] - 2021-05-19

	- Initial public version.

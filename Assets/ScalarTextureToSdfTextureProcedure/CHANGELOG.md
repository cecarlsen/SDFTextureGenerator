# Changelog


## [1.0.5] - 2024-01-15

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

# SDFTextureGenerator

Signed Distance Field (SDF) texture generator for Unity using the Jump Flooding Algorithm (JFA) implemented in a ComputeShader.

Updated with Unity 6000.3.0f1.

There are two main procedures in this repo:  

 - MaskToSdfTextureProcedure (pixels).  
 - Mask3DToSdfTexture3DProcedure (voxels).  

![Splash](https://raw.githubusercontent.com/cecarlsen/SDFTextureGenerator/master/ReadmeImages/Splash.jpg)


### Impleementation notes

- Using a single texture to store both inside and outside seeds, reducing the number of dispatch calls and texture R/W.  
- Using GPU group shared memory to reduce reads per thread.  
- Optional sub pixel interpolation for smoother edges, especially at low resolutions.
- Optional downsampling.
- Optional double buffering to remove jitter (GPU race conditions).
- Optional SDF and internal flood texture precision settings.

![Splash](https://raw.githubusercontent.com/cecarlsen/SDFTextureGenerator/master/ReadmeImages/SubPixelInterpolation.gif)

### References
- [Shadertoy implementation](https://www.shadertoy.com/view/Mdy3DK) and [Explanation of JFA](https://blog.demofox.org/2016/02/29/fast-voronoi-diagrams-and-distance-dield-textures-on-the-gpu-with-the-jump-flooding-algorithm/) by Demofox

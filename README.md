# SDFTextureGenerator

Signed Distance Field (SDF) texture generator for Unity using the Jump Flooding Algorithm (JFA) implemented in a ComputeShader.

Updated with Unity 6000.0.21f1.

There are three procedures in this repo:  
 - MaskToSdfTextureProcedure (pixels).  
 - Mask3DToSdfTexture3DProcedure (voxels).  
 - Texture3DToTextureSliceProcedure (voxels to pixels). 

![Splash](https://raw.githubusercontent.com/cecarlsen/SDFTextureGenerator/master/ReadmeImages/Splash.jpg)

### References
- [Shadertoy implementation](https://www.shadertoy.com/view/Mdy3DK) and [Explanation of JFA](https://blog.demofox.org/2016/02/29/fast-voronoi-diagrams-and-distance-dield-textures-on-the-gpu-with-the-jump-flooding-algorithm/) by Demofox

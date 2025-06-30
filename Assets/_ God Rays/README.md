# God Rays Feature Explanation

## Get started

- Add `God Rays Feature` to list of Scriptalbe Render Features in `URP Renderer data`
- Create Global Volume with `God Rays Volume Component` on it

## Restrictions

- Names/paths of shaders and svc(shader variant collection) not recommended to change: I use it for initialization
  Shader Feature after first creating.
- `GodRaysSVC.shadervariants` should contains only two shaders: `Ledsna/GodRays` with selected count of iterations (
  shader feature `ITERATIONS_64` for example) and `Ledsna/BilaterialBlur` with empty keyword set
- To change parameters of effect create Global Volume and add `God Rays Volume Component`. Don't change default values
  in Render Feature
- Count iterations of loop in fragment shader(`SampleCount`) can't be changed in build version. SVC set up witch of
  shader variants with `ITERATIONS_X` will be used in build. It's automatically updates after changing in inspector
- If you want to disable God Rays Effect entirely from build you need:
    - Remove God Rays Feature
    - Clear `GodRaysSVC.shadervariants` all shaders if you also want remove shader to add to build

## Features

- Effect fully compatable with unity volume system
- On Vulkan and Meta you get more performance as render feature use Framebuffer optimization that enabled on this APIs
- For friendly user experience I use package `Naughty Attributes`
- If you want see effect in scene enable bool `renderInScene` in Render Feature settings

## Open problems

- Unity add to build shader `Ledsna/GodRays` with empty set of keywords. It's hard to understand why unity shader
  preprocessor thinks that that shader used(I tried remove serailization from Shader field `godRaysShader` but this didn't
  help). Probably I should use `IPreprocessShaders` but I think this overhead.

# Full Story

I decided to use the Shader Varian Collection to save the shader versions needed for the build. For this I need write scripts that update svc(Shaver Variant Collection) when developer change properties in editor

```cs
// What is second argument with type PassType??? 
var oldVariant =
    new ShaderVariantCollection.ShaderVariant(godRaysShader, (?)PassType(?), oldKeyword);
if (svc.Contains(oldVariant))
    svc.Remove(oldVariant);

var newVariant =
    new ShaderVariantCollection.ShaderVariant(godRaysShader, (?)PassType(?), newKeyword);
if (!svc.Contains(oldVariant))
    svc.Add(newVariant);
```

I'm going into the [docs](https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Rendering.PassType.html) to find out what the PassType is

> This corresponds to "LightMode" tag in the shader pass, see [Pass tags](https://docs.unity3d.com/6000.1/Documentation/Manual/SL-PassTags.html).

Yeah! I've already heard about this tag. Without thinking, I add `"LightMode" = "ScriptableRenderPipeline"` **to the list of tags in SubShader Tags block**

I go back to the code, add `PassType.ScriptableRenderPipeline` and see that everything works: no errors like belove

![example of error](image.png)

and svc looks like it should

![Shader Varian Collection](image-1.png)

In build also all works fine and only the necessary shader option in it.

But time passes and I notice that I added a non-existent `LightMode` tag to the `SubShader` `Tags` block. In [docs](https://docs.unity3d.com/6000.1/Documentation/Manual/SL-SubShaderTags.html) there is only this SubShader tags:

- `RenderPipeline`
- `Queue`
- `RenderType`
- `DisableBatching`
- `ForceNoShadowCasting`
- `CanUseSpriteAtlas`
- `PreviewType`

And page [Add a shader tag to a SubShader or Pass](https://docs.unity3d.com/6000.1/Documentation/Manual/add-shader-tag.html) tells this:

> Note that both SubShaders and Passes use the Tags block, but they work differently. Assigning SubShader tags to a Pass has no effect, and vice versa.

wtf, how all this work? In theory, the shader should have a `Normal` `PassType` as I added non exisitng tag to SubShader Tags block, which means my script should not work. Even in the picture above from the `svc`, you can see that the shader has a `PassType` `ScriptableRenderPipeline`.

I'm starting to figure it out, I understand that the `ScriptableRenderPipeline` tag is not in the list that is on the [page](https://docs.unity3d.com/6000.1/Documentation/Manual/SL-PassTags.html) where it translates from PassType [doc](https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Rendering.PassType.html):

> This corresponds to "LightMode" tag in the shader pass, see [Pass tags](https://docs.unity3d.com/6000.1/Documentation/Manual/SL-PassTags.html).

and only later I noticed that this list is for BRP.

I spend some more time to realize that the list of allowed tags for URP is not in section 
```
Materials and shaders / Custom shaders / Shader languages reference / ShaderLab language reference / Pass in ShaderLab reference / Pass tags in ShaderLab reference
```
but in section
```
Materials and shaders / Shaders / Shaders in URP / Writing custom shaders in URP / Shader methods in URP / ShaderLab Pass tags in URP reference
```

on this [page](https://docs.unity3d.com/6000.1/Documentation/Manual/urp/urp-shaders/urp-shaderlab-pass-tags.html)

## So ...

Why can't you just add information about URP Tags to [Pass tags in ShaderLab reference](https://docs.unity3d.com/6000.1/Documentation/Manual/SL-PassTags.html), because for the SubShader, all tags for URP are listed in the same section [SubShader tags in ShaderLab reference](https://docs.unity3d.com/6000.2/Documentation/Manual/SL-SubShaderTags.html)?

# But what about the bug with the `SubShader` `Tags` block?

1. Creating an empty 3d URP project
2. Add unlit shader example from [docs](https://docs.unity3d.com/6000.1/Documentation/Manual/urp/writing-shaders-urp-basic-unlit-structure.html):
    ```shader
    ```


From [docs](https://docs.unity3d.com/6000.2/Documentation/Manual/add-shader-tag.html)

> Note that both SubShaders and Passes use the `Tags` block, but they work differently. Assigning SubShader tags to a Pass has no effect, and vice versa.

But in "Universal Render Pipeline/Lit" shader

```cs
Shader "Universal Render Pipeline/Lit"
{
    Properties
    {
        ...
    }

    SubShader
    {
        // Universal Pipeline tag is required. If Universal render pipeline is not set in the graphics settings
        // this Subshader will fail. One can add a subshader below or fallback to Standard built-in to make this
        // material work with both Universal Render Pipeline and Builtin Unity Pipeline
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }
        LOD 300

      Pass {...}
      
      ...
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.LitShader"
}
```

`"UniversalMaterialType" = "Lit"` set in `SubShader` `Tags` but it's belong to [Pass Tags](https://docs.unity3d.com/6000.1/Documentation/Manual/urp/urp-shaders/urp-shaderlab-pass-tags.html) and not to [SubShader Tags](https://docs.unity3d.com/6000.1/Documentation/Manual/SL-SubShaderTags.html)


I also noticed that I made a mistake in some of my custom shaders and added the Light Mode tag to the list of SubShader tags and everything worked correctly: I mean, the shader did not ignore this line, but processed it correctly, which could be understood from my Shader Variant Collection and the cs code working correctly:

```cs
var oldVariant =
    new ShaderVariantCollection.ShaderVariant(godRaysShader, (?)PassType(?), oldKeyword);
if (svc.Contains(oldVariant))
    svc.Remove(oldVariant);

var newVariant =
    new ShaderVariantCollection.ShaderVariant(godRaysShader, (?)PassType(?), newKeyword);
if (!svc.Contains(oldVariant))
    svc.Add(newVariant);
```

There were no mistakes like these

![example of error](image.png)

Despite the fact that it was written in the shader
```cs
SubShader
{
    Tags
    {
        "RenderType"="Opaque"
        "DisableBatching"="True"
        "RenderPipeline" = "UniversalPipeline"
        "LightMode" = "ScriptableRenderPipeline"
    }
    ...
}
```

After several tests, I have an assumption that adding tags from a Pass Tags block to a SubShader causes Unity to add this tag to all other Pass Tags inside the current SubShader.

I can provide more information about my case if needed.

P.s. I know that `LightMode` with `"ScriptableRenderPipeline"` does not exist.


# Preparing character artwork
The 2D Animation package is used together with the [PSD Importer](https://docs.unity3d.com/Packages/com.unity.2d.psdimporter@latest) package to import of your artwork for animation. The PSD Importer imports the graphic data from each [Photoshop Layer](https://helpx.adobe.com/photoshop/using/layer-basics.html) as Sprites, and provides various [importer options](#psd-importer-features) that prepare your artwork for animation. The PSD Importer currently only supports the [Adobe Photoshop .psb](https://helpx.adobe.com/photoshop/using/file-formats.html#large_document_format_psb) file format, thus it is recommended to create your artwork in [Adobe Photoshop](https://www.adobe.com/products/photoshop.html) or any other graphic software which supports the Adobe .psb file format.

When preparing your character or prop artwork, it is recommended to prepare them in a neutral or idle position. Depending on the complexity and use of your animation, it is also recommended to separate the individual parts of the artwork onto different Photoshop [Layers](https://helpx.adobe.com/photoshop/using/layer-basics.html) (see Example 1 below). The artwork file must be saved in the [Adobe Photoshop .psb](https://helpx.adobe.com/photoshop/using/file-formats.html#large_document_format_psb) file format, which is functionally identical to the more common Adobe [.psd format](https://helpx.adobe.com/photoshop/using/file-formats.html#photoshop_format_psd), but supports much larger images than the .psd format (up to 300,000 pixels in any dimension). You can convert existing artwork in the .psd format to the .psb format by opening and then saving them as .psb files in Adobe Photoshop.

![](images/2DAnimationV2_PSDLayers.png)<br/>Example 1: Layered character artwork in Adobe Photoshop.

Once the artwork is imported, a Prefab is generated from the graphic data of each layer as individual Sprites, which may be arranged in their original position or as a Sprite sheet depending on the selected [importer options](#psd-importer-features). The generated Prefab is referred to as an 'actor' when used with the 2D Animation package.

## PSD Importer features
The PSD Importer has many features and options that prepare the actor for animation. For example, enable the [Mosaic](https://docs.unity3d.com/Packages/com.unity.2d.psdimporter@5.0/manual/PSD-importer-properties.html#Mosiac) option to have Unity automatically generate a Sprite sheet from the imported layers; or enable [Character Rig](https://docs.unity3d.com/Packages/com.unity.2d.psdimporter@5.0/manual/PSD-importer-properties.html#character-rig) to have Unity generate a Prefab with Sprites generated from the imported source file, with the Sprites arranged into their original positions based on the source file. Refer the [PSD Importer's documentation](https://docs.unity3d.com/Packages/com.unity.2d.psdimporter@latest) for more information about the different options and their functions.

![](images/2DAnimationV2_Mosaic_Prefab.png)<br/>Example 2: The imported actor's layers arranged into a Sprite sheet, and reassembled into their original positions in the generated Prefab.

## General workflow
There are several ways to animate with the 2D Animation package, depending on how your artwork is prepared and how your animation will be used in your Unity Project. Sample projects with examples of the different ways to use the 2D Animation package are distributed with the package, and can be imported via the [Package Manager](https://docs.unity3d.com/Manual/Packages.html). Refer to the [Samples documentation](Examples.md) for more information.

The following is a general workflow for importing a multilayered and multipart character into Unity for 2D animation with the PSD Importer, with a more detailed specific example available in the [Character imported with the PSD Importer](ex-psd-importer.md) sample documentation:

1. Save your artwork as a .psb file in Adobe Photoshop by selecting the __Large Document Format__ under the __Save As__ menu, or convert an existing .psd file into the .psb format which is supported by the [PSD Importer](https://docs.unity3d.com/Packages/com.unity.2d.psdimporter@latest).
   <br/>
2. Import the .psb file into Unity with the PSD Importer, which generates a Prefab containing Sprites based on the layers of the source file. This Prefab is referred to as an 'actor' when used with the 2D Animation package.
   <br/>
3. Select the actor and go to its Inspector window to select its Importer settings. Refer to the [PSD Importer](https://docs.unity3d.com/Packages/com.unity.2d.psdimporter@latest) documentation and the [imported Samples and respective documentation](Examples.md) to determine which settings are a best fit for your Project. <br/>
   For example, the following are the recommended import settings for a [character with multiple limbs and layers in its source file](ex-psd-importer.md):
   <br/>
    * Set **Texture Type** to __Sprite(2D and UI)__.
    * Set **Sprite Mode** to __Multiple__.
    * Select the __Mosaic__ check box.
    * Select the __Character Rig__ check box.
    * Select the __Use Layer Grouping__ check box to preserve any Layer Groups in the original .psb source file.
    <br/>
4. Select __Apply__ to apply the above settings. The generated actor is now ready for [rigging](CharacterRig.md).
   <br/>
5. Drag the actor Prefab into the Scene view to begin [animating](Animating-actor.md). Unity automatically adds the [Sprite Skin component](SpriteSkin.md) to the actor which deforms the Sprite using GameObject Transforms to represent the bones that are [rigged](CharacterRig.md) and weighted to the Sprite  in the [Skinning Editor](SkinningEditor.md).

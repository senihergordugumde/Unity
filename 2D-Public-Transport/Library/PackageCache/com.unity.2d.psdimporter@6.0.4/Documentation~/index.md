# Overview
The **PSD Importer** is an [Asset importer](https://docs.unity3d.com/ScriptReference/AssetImporter.html) that imports [Adobe Photoshop .psb](https://helpx.adobe.com/photoshop/using/file-formats.html#large_document_format_psb) files into Unity, and generates a [Prefab](https://docs.unity3d.com/Manual/Prefabs.html) of Sprites based on the imported source file. The .psb file format is functionally identical to the more common Adobe [.psd format](https://helpx.adobe.com/photoshop/using/file-formats.html#photoshop_format_psd), but supports much larger images than the .psd format (up to 300,000 pixels in any dimension). To convert existing artwork from .psd to .psb format, you can open them in Adobe Photoshop and then save them as .psb files.

Importing .psb files with the PSD Importer allows you to use features such as [Mosaic](PSD-importer-properties.md#Mosaic) (to automatically generate a Sprite sheet from the imported layers) and [Character Rig](PSD-importer-properties.md#Rig) (to reassemble the Sprites of a character as they are arranged in their source files).

The PSD Importer currently only supports two [Texture Modes](https://docs.unity3d.com/Manual/TextureTypes.html):[ Default](https://docs.unity3d.com/Manual/TextureTypes.html#Default) and[ Sprite](https://docs.unity3d.com/Manual/TextureTypes.html#Sprite).

**Note:** The **Sprite Library Asset** is no longer editable from the Skinning Editor of the [2D Animation](https://docs.unity3d.com/Packages/com.unity.2d.animation@latest) from version 6.0 onwards as the Category and Label options have been removed from the Sprite Visibility panel. However, the PSD Importer will continue to automatically generate Sprite Library Assets if relevant data from a previous version is present.

## Supported and unsupported Photoshop effects

When importing a .psb file into Unity with the PSD Importer, the importer generates a prefab of Sprites based on the image and layer data of the imported .psb file. However, the PSD Importer does not support all of Photoshop’s layer and visual effects or features. The PSD Importer ignores the following Photoshop layer properties and visual effects when it generates the Sprites and prefab:

- Channels
- Blend Modes
- Layer Opacity
- Effects

If you want to add visual effects to the generated Sprites, you can add additional Textures to the Sprites with the [Sprite Editor's Secondary Textures](https://docs.unity3d.com/Manual/SpriteEditor-SecondaryTextures.html) module. Shaders can sample these Secondary Textures to apply additional effects to the Sprite, such as normal mapping. Refer to the [Sprite Editor: Secondary Textures](https://docs.unity3d.com/Manual/SpriteEditor-SecondaryTextures.html) documentation for more information.

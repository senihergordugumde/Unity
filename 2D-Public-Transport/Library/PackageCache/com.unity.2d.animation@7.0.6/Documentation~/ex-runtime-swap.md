# Runtime Swap
This sample demonstrates how you can use the Sprite Library API to override a specific Entry. Note that the sample requires the [PSD Importer](https://docs.unity3d.com/Packages/com.unity.2d.psdimporter@latest) installed. Open the `6 Runtime Swap` Scene to see the sample in action.

![](images/2D-animation-samples-runtimeswap.png)
The graphic Assets are located in `Assets/Samples/2D Animation/[X.Y.Z]/Samples/5 SpriteSwap/Sprites`:

- `Primary.psb`
- `Skeleton.psb`

The `Skeleton.psb` uses the [.skeleton Asset](https://docs.unity3d.com/Packages/com.unity.2d.psdimporter@5.0/manual/PSD-importer-properties.html#main-skeleton) from the `Primary.psb` for its rigging. It also references the `Primary.spriteLib` Sprite Library Asset located in `Assets/Samples/2D Animation/[X.Y.Z]/Samples/5 SpriteSwap/Sprites`.

## Runtime Swap script
A custom MonoBehaviour script called the `RuntimeSwap` is attached to the `KnigtboyRig` GameObject. The script is located in `Assets/Samples/2D Animation/[X.Y.Z]/Samples/5 SpriteSwap/Scripts/Runtime/RuntimeSwap.cs`

Pressing a button (in the sample Scene) with a Sprite from the `Skeleton.psb` causes the script to use the override API from the Sprite Library to override that Sprite Entry.

```c++
m_SpriteLibraryTarget.AddOverride(entry.sprite, entry.category, entry.entry);
```

Pressing a button with a Sprite from the `Primary.psb` causes the script to use the override rest API from the Sprite Library to remove the Sprite Entry override.

```c++
m_SpriteLibraryTarget.RemoveOverride(entry.category, entry.entry);
```

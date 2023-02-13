# Sprite Swapping
__Sprite Swap__ is a feature that enables you to change a GameObject’s rendered Sprite at runtime. This has a number of uses, such as easily creating multiple characters which [share a skeleton](ex-skeleton-sharing.md) (requires the [PSD Importer package](https://docs.unity3d.com/Packages/com.unity.2d.psdimporter@latest)) or [reuse existing bone and Mesh data](SkinEdToolsShortcuts.md#copy-and-paste-behavior) while looking visually different.

Using Sprite Swap to change the rendered Sprites on each frame at runtime, you can even simulate a [frame-by-frame animation](FFanimation.md) style. The 2D Animation package comes with several Sample projects of the other different ways you can use Sprite Swap to achieve different effects and features, refer to the [Sample documentation here](ex-sprite-swap.md) for more information about these examples.

## Sprite Swap Assets and components
Sprite Swap requires the following Assets and components, which are all included with the 2D Animation package:

1. The [Sprite Library Asset](SLAsset.md) that contains a set of selected Sprites which are assigned to different [Categories](SLAsset.md#category) and [Labels](SLAsset.md#entry).
   <br/>
2. Attach the [Sprite Library component](SLAsset.html#sprite-library-component) to a GameObject to assign or change which __Sprite Library Asset__ the GameObject refers to.
   <br/>
3. The [Sprite Resolver component](SLAsset.html#sprite-resolver-component) is used to request a Sprite registered to the Sprite Library Asset by referring to the __Category__ and __Label__ value of the desired Sprite.

## Skeletal animation limitations
To ensure Sprite Swap works correctly with skeletal animation, the skeleton must be identical between the Sprites being swapped. Use the [Copy and Paste tools](SkinEdToolsShortcuts.md#copy-and-paste-behavior) to duplicate the bone and skeleton data from one Sprite to another to ensure they can be swapped correctly.

## Animator limitations
In a single [Animator Controller](https://docs.unity3d.com/Manual/AnimatorControllers.html), you cannot have one [Animation Clip](https://docs.unity3d.com/Manual/AnimationClips.html) animating the [Sprite Renderer’s](https://docs.unity3d.com/Manual/class-SpriteRenderer.html) assigned Sprite while another [Animation Clip](https://docs.unity3d.com/Manual/AnimationClips.html) animates the [Sprite Resolver’s](SLAsset.html#sprite-resolver-component) Sprite Key. If these two clips are in the same [Animator Controller](https://docs.unity3d.com/Manual/AnimatorControllers.html), they will conflict with each other causing unwanted playback results.

To resolve this issue, we advise the following solutions. The first solution is to separate the [Animation Clips](https://docs.unity3d.com/Manual/AnimationClips.html) into separate [Animator Controllers](https://docs.unity3d.com/Manual/AnimatorControllers.html) that contain only clips that animate either a [Sprite Renderer’s](https://docs.unity3d.com/Manual/class-SpriteRenderer.html) Sprite or the [Sprite Resolver’s](SLAsset.html#sprite-resolver-component) Sprite Key; but not both types in the same [Animator Controller](https://docs.unity3d.com/Manual/AnimatorControllers.html).

The second solution is to update all [Animation Clips](https://docs.unity3d.com/Manual/AnimationClips.html) to the same type so that they can all remain in a single [Animator Controller](https://docs.unity3d.com/Manual/AnimatorControllers.html), by converting all clips animating a [Sprite Renderer’s](https://docs.unity3d.com/Manual/class-SpriteRenderer.html) Sprite to animating a [Sprite Resolver’s](SLAsset.html#sprite-resolver-component) Sprite Key, or vice versa.

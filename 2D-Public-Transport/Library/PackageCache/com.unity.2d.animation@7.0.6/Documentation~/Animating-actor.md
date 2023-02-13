#Animating an actor
After [importing](PreparingArtwork.md) and [rigging](CharacterRig.md) an actor, you can begin animating by simply dragging the rigged actor into the Scene view. By repositioning the different bones of the actor on the Animation timeline with [Unity's animation workflow and tools](https://docs.unity3d.com/Manual/AnimationSection.html). The mesh of the actor [deforms](SpriteSkin.md) with the positioning of the rigged bones, creating the animation.

Aside from this method, there are other ways that you can animate with the 2D Animation packge. The following are a few examples based on the Sample projects available for you to import to use with the package.

## Sprite Swap
The 2D Animation package allows you to use the [Sprite Swap](SpriteSwapIntro.md) feature to swap to different Sprites at runtime, from [swapping only a single part](CharacterParts.md) of an actor to [swapping the entire Sprite Library Asset](SLASwap.md) it refers to.

### Frame-by-frame animation
By using [Sprite Swap](SpriteSwapIntro.md), you can create frame-by-frame style animations by swapping to different Sprites on each frame at runtime. Refer to the [Frame-by-frame Animation](FFanimation.md) documentation for more detailed information on how you can achieve this animation style.

### Other Sample projects
[Sample projects ](Examples.md) are distributed with the 2D Animation package and available for import. These projects include examples of the different ways you can animate with the package features, such as the [Flipbook Animation Swap](ex-sprite-swap.md#flipbook-animation-swap) and [Animated Swap](ex-sprite-swap.md#animated-swap) and so on. Refer to the respective Sample documentation pages for more information.

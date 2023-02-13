using System.Runtime.CompilerServices;

// Some code in Unity.Entities.TypeManager
// (https://github.cds.internal.unity3d.com/unity/dots/blob/d82f136abd45af8760235b885b63ecb50dcaf5f8/Packages/com.unity.entities/Unity.Entities/Types/TypeManager.cs#L426)
// uses reflection to call a static Unity.Burst.Editor.BurstLoader.IsDebugging property,
// to ensure that BurstLoader has been initialized.
// It specifically looks in the Unity.Burst.Editor.dll assembly.
// So we use type-forwarding to let it find the "real" BurstLoader.
[assembly: TypeForwardedToAttribute(typeof(Unity.Burst.Editor.BurstLoader))]
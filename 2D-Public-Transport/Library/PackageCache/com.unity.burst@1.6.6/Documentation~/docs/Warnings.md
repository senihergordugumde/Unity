# Warnings 

# IgnoreWarning attribute

The `Unity.Burst.CompilerServices.IgnoreWarningAttribute` attribute lets you suppress warnings for a specific function that is being compiled from Burst. It is highly recommended that developers do not use this because the warnings that the Burst compiler generates are very important to adhere to.

One of the few places where this attribute can serve to be useful is if you want a true 'crash-the-game' abort. _A reminder that exceptions in Burst _only throws properly in the editor_. This means that any exception thrown from Burst that persists into the built game will become an abort, which will crash the game. Burst will warn you about these exceptions, and advise you to place them in functions guarded with `[Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]`. But let's say a developer really wants to throw an exception that will crash the game; with the `IgnoreWarningAttribute` you can suppress the warnings that Burst would provide on the throw like:

```c#
[IgnoreWarning(1370)]
int DoSomethingMaybe(int x)
{
    if (x < 0) throw new Exception("Dang - sorry I crashed your game!");

    return x * x;
}
```

# BC1370

## An exception was thrown from a function without the correct [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")] guard....

This warning will be produced if a throw is encountered in code not guarded by `[Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]`. This is because a throw in a Standalone Player translates directly to an abort which will terminate the application with a crash (as described above) [IgnoreWarning Attribute](#ignorewarning-attribute)

> Note that this warning is **only** produced for exceptions that persist into player builds, meaning that editor-only or debug-only exception throws that are removed from player builds will be ignored.

# BC1371

## A call to the method 'xxx' has been discarded, due to its use as an argument to a discarded method...

To understand this warning let us look at a simple example :

```c#
[BurstDiscard]
static void DoSomeManagedStuff(int x)
{
    ///.. only run when Burst compilation is disabled
}

// A function that computes some result which we need to pass to managed code
int BurstCompiledCode(int x,int y)
{
    return y+2*x;
}

[BurstCompile]
void BurstMethod()
{
    var myValue = BurstCompiledCode(1,3);
    DoSomeManagedStuff(myValue);
}
```

Now if your c# code is compiled in release mode (the default in modern unity versions), the local variable `myValue` will be optimised away, and Burst will see something like the below :

```c#
[BurstCompile]
void BurstedMethod()
{
    DoSomeManagedStuff(BurstCompiledCode(1,3));
}
```

This will cause Burst to generate the warning, since `DoSomeManagedStuff` is marked `[BurstDiscard]`, so Burst will discard it along with any arguments, in this case `BurstCompiledCode` is an argument directly. This of course means that the `BurstCompiledCode` function is no longer executed, which may be unexpected, and hence why the warning is generated.

If this is not what you intended then you can workaround it by ensuring the variable has multiple uses, e.g. 

```c#

void IsUsed(ref int x)
{
    // Dummy function to prevent removal
}

[BurstCompile]
void BurstedMethod()
{
    var myValue = BurstCompiledCode(1,3);
    DoSomeManagedStuff(myValue);
    IsUsed(ref myValue);
}
```

Alternatively if you happy that the code is being discarded correctly, you can ignore the warning on the `BurstedMethod` e.g. 

```c#
[IgnoreWarning(1371)]
[BurstCompile]
void BurstMethod()
{
    var myValue = BurstCompiledCode(1,3);
    DoSomeManagedStuff(myValue);
}
```

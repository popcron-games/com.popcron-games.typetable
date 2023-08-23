# TypeCache
Utility for retrieving types through a cached source at runtime, not just in editor. A scriptable object will be used, it will have a list of assembly names which will be populated automatically as its used in the editor (in build it's fixed).

### Fetch all static methods with an attribute:
```cs
foreach ((MethodInfo method, Debug attribute) in TypeCache.GetMethodsWithAttribute<Debug>())
{
    Debug.Log($"Method {method} has the debug attribute!");
}
```

### Retrieve all types that implement an interface:
```cs
foreach (Type type in TypeCache.GetTypesThatImplement<IExample>())
{
    Debug.Log($"Type {type} implements the IExample interface!");
}
```

### Retrieve all types that inherit from a class:
```cs
foreach (Type type in TypeCache.GetSubtypesOf<MonoBehaviour>())
{
    Debug.Log($"Type {type} is a component!");
}
```
### Create objects of type X:
```cs
foreach (Type type in TypeCache.GetSubtypesOf<Mod>())
{
    if (type.IsAbstract) continue;
    Mod mod = (Mod)Activator.CreateInstance(type);
    Debug.Log($"Created mod {mod}!");
}
```
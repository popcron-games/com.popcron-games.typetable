# TypeTable
![Alt text](image.png)

### Features
* Assigning types an ID (`ushort`) using the `[TypeID]` attribute
  * This attribute will preserve the type being decorated with the attribute, so its not missing because of IL2CPP stripping
* Fetching type IDs and `Type`s from IDs using `TypeTable.GetID<T>()` and `TypeTable.GetType(typeId)`
* Fetching types that implement the desired type, or inherit from it using `TypeTable.GetTypesAssignableFrom<T>()`
* Types in build are loaded using a TypeTableLoader script, this script is automatically generated when available types change while using the editor
  * Use the `Window/Popcron/TypeTable/Generate Loader Script` menu item to do this manually
 
### Installation
URL for adding as package:
```json
https://github.com/popcron-games/com.popcron-games.typetable.git
```

### Example
Where `IManager` instances are automatically created in build or in editor
```cs
#if UNITY_EDITOR
[UnityEditor.InitializeOnLoad]
#endif
public static class ManagerManager
{
    private static bool initialized;
    private static readonly Dictionary<Type, IManager> managers = new();

    static ManagerManager()
    {
        Initialize();
    }

    private static void Dispose()
    {
        foreach (IManager manager in managers.Values)
        {
            manager.Dispose();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (initialized) return;
        initialized = true;

        foreach (Type type in TypeTable.GetTypesAssignableFrom<IManager>())
        {
            if (type.IsInterface) continue;
            managers.Add(type, (IManager)Activator.CreateInstance(type));
        }

#if UNITY_EDITOR
        UnityEditor.Compilation.CompilationPipeline.compilationStarted += (v) =>
        {
            Dispose();
        };
#else
        Application.quitting += Dispose;
#endif
    }
}

[TypeID(100)]
public interface IManager : IDisposable
{

}

[TypeID(101)]
public class GameManager : IManager
{
    public GameManager()
    {
        //will get called in editor (in edit mode) and in build (like its play in editor)
        Debug.Log("created game manager");
    }

    void IDisposable.Dispose()
    {
        Debug.Log("destroyed game manager");
    }
}
```
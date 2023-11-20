# TypeTable
![Alt text](image.png)

### Features
* Assigning types an ID using the `[TypeID]` attribute
    * This attribute will preserve the type in builds so its not missing due to IL2CPP stripping
* Fetching type IDs using `TypeTable.GetID<T>()`
 
### Installation
URL for adding as package:
```json
https://github.com/popcron-games/com.popcron-games.typecache.git
```

### Example
```cs
[TypeID(1)]
public class Position
{
    public Vector3 value;
}

[TypeID(2)]
public class Color
{
    public Vector3 hsv;
}

[TypeID(3)]
public interface ICommand
{
    void Run();
}

[TypeID(4)]
public class PlayGame : ICommand
{
    void ICommand.Run()
    {
        Debug.Log("play game!");
    }
}

foreach (Type type in TypeTable.Types)
{
    Debug.Log(type);
}

foreach (Type commandType in TypeTable.GetTypesAssignableFrom<ICommand>())
{
    if (commandType.IsInterface) continue;
    ICommand command = (ICommand)Activator.CreateInstance(commandType);
    command.Run();
}
```
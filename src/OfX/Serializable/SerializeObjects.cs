using System.Text.Json;

namespace OfX.Serializable;

/// <summary>
/// We use the native .NET Serialization System.Text.Json for serialization and deserialization!
/// </summary>
public static class SerializeObjects
{
    public static string SerializeObject(object obj) => JsonSerializer.Serialize(obj);

    public static object DeserializeObject(string objSerialized, Type objectType) =>
        JsonSerializer.Deserialize(objSerialized, objectType);
}
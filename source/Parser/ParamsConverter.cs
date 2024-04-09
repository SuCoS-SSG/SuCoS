using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace SuCoS.Parser;

/// <summary>
/// A custom YAML type converter for dictionaries with string keys and object values.
/// </summary>
public class ParamsConverter : IYamlTypeConverter
{
    /// <summary>
    /// Checks if the converter can handle the specified type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is a dictionary with string keys and object values, false otherwise.</returns>
    public bool Accepts(Type type)
    {
        return type == typeof(Dictionary<string, object>);
    }

    /// <summary>
    /// Reads a YAML stream and deserializes it into a dictionary.
    /// </summary>
    /// <param name="parser">The YAML parser.</param>
    /// <param name="type">The type of the object to deserialize.</param>
    /// <returns>A dictionary deserialized from the YAML stream.</returns>
    public object ReadYaml(IParser parser, Type type)
    {
        var dictionary = new Dictionary<string, object>();

        if (!parser.TryConsume<MappingStart>(out _))
        {
            // throw new YamlException("Expected a mapping start.");
        }

        while (!parser.TryConsume<MappingEnd>(out _))
        {
            if (!parser.TryConsume<Scalar>(out var key))
            {
                throw new YamlException("Expected a key.");
            }

            if (parser.TryConsume<SequenceStart>(out _))
            {
                var list = new List<object>();
                while (!parser.TryConsume<SequenceEnd>(out _))
                {
                    if (parser?.Current is MappingStart)
                    {
                        list.Add(ReadYaml(parser, type));
                    }
                    else if (parser?.Current is Scalar)
                    {
                        if (parser.TryConsume<Scalar>(out var scalar))
                        {
                            list.Add(scalar.Value);
                        }
                    }
                    else
                    {
                        throw new YamlException(
                            "Expected a value, a nested mapping, or a sequence end."
                        );
                    }
                }

                dictionary[key.Value] = list;
            }
            else if (parser.TryConsume<MappingStart>(out _))
            {
                var nestedDictionary =
                    (Dictionary<string, object>)ReadYaml(parser, type);
                dictionary[key.Value] = nestedDictionary;
            }
            else
            {
                if (parser.TryConsume<Scalar>(out var value))
                {
                    dictionary[key.Value] = value.Value;
                }
            }
        }

        return dictionary;
    }

    /// <summary>
    /// Writes an object to a YAML stream.
    /// </summary>
    /// <param name="emitter">The YAML emitter.</param>
    /// <param name="value">The object to serialize.</param>
    /// <param name="type">The type of the object to serialize.</param>
    public void WriteYaml(IEmitter emitter, object? value, Type type)
    {
        throw new NotImplementedException();
    }
}

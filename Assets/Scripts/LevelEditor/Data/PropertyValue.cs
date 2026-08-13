using System;
using UnityEngine;

/// <summary>
/// A tagged-union value used by property bags (<see cref="ComponentSpec"/>,
/// <see cref="EntityInstance"/>). Only one of the fields below is meaningful,
/// selected by <see cref="Type"/>.
/// </summary>
[Serializable]
public struct PropertyValue
{
    public PropertyType Type;

    public float FloatValue;
    public int IntValue;
    public bool BoolValue;
    public string StringValue;
    public Vector2 Vector2Value;
    public Color ColorValue;

    public static PropertyValue FromFloat(float value) => new() { Type = PropertyType.Float, FloatValue = value };
    public static PropertyValue FromInt(int value) => new() { Type = PropertyType.Int, IntValue = value };
    public static PropertyValue FromBool(bool value) => new() { Type = PropertyType.Bool, BoolValue = value };
    public static PropertyValue FromString(string value) => new() { Type = PropertyType.String, StringValue = value };
    public static PropertyValue FromVector2(Vector2 value) => new() { Type = PropertyType.Vector2, Vector2Value = value };
    public static PropertyValue FromColor(Color value) => new() { Type = PropertyType.Color, ColorValue = value };

    public override string ToString()
    {
        return Type switch
        {
            PropertyType.Float => FloatValue.ToString("0.###"),
            PropertyType.Int => IntValue.ToString(),
            PropertyType.Bool => BoolValue.ToString(),
            PropertyType.String => StringValue,
            PropertyType.Vector2 => Vector2Value.ToString(),
            PropertyType.Color => ColorValue.ToString(),
            _ => string.Empty,
        };
    }
}

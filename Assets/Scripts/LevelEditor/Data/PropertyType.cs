/// <summary>
/// The value kinds a per-instance/per-component property can hold.
/// Deliberately small and primitive-only so it maps cleanly onto both
/// Unity component fields and MiniScript's dynamically-typed values.
/// </summary>
public enum PropertyType
{
    Float,
    Int,
    Bool,
    String,
    Vector2,
    Color,
}

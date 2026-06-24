namespace WTorricos.Either;

/// <summary>
/// Represents a unit value (void/nothing). Used when an operation has no meaningful return value
/// but you still want to track success/failure.
/// </summary>
public sealed record Unit
{
    public static readonly Unit Instance = new();

    private Unit() { }

    public override string ToString() => "()";
}

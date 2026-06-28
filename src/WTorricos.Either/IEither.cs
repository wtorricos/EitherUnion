namespace WTorricos.Either;

/// <summary>
/// IEither represents a discriminated union of two types: Failure (error) or Ok (success value).
/// This is v1's core type using C# union types for expressive, type-safe error handling.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public readonly union IEither<T>(Failure, Ok<T>);

using System;

namespace Nova.Exceptions;

public class DuplicatedDefinitionException(string message) : ArgumentException(message) { }

/// <summary>
/// Used when something is not found and it is not an argument.
/// </summary>
public class InvalidAccessException(string message) : Exception(message) { }

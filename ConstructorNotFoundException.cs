namespace Rampastring.XNAUI;

using System;

/// <summary>
/// The exception that is thrown when the <see cref="GUICreator"/> fails to find a matching
/// constructor for a GUI control type.
/// </summary>
public class ConstructorNotFoundException : Exception
{
    public ConstructorNotFoundException(string message)
        : base(message)
    {
    }
}
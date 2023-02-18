namespace Rampastring.XNAUI.XNAControls;

using Rampastring.Tools;

/// <summary>
/// Provides an interface for an object that can parse
/// and apply custom INI attributes for controls.
/// </summary>
public interface IControlINIAttributeParser
{
    /// <summary>
    /// Attempts to parse given key's value and sets the parameter value for the given control.
    /// </summary>
    /// <param name="control">The control that the parsing happens for currently.</param>
    /// <param name="iniFile">The INI file that is being read from.</param>
    /// <param name="key">The key that is being read.</param>
    /// <param name="value">The key's value.</param>
    /// <returns>Whether the parsing was succesful.</returns>
    bool ParseINIAttribute(XNAControl control, IniFile iniFile, string key, string value);
}
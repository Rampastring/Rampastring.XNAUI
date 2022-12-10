using Rampastring.Tools;

namespace Rampastring.XNAUI.XNAControls
{
    /// <summary>
    /// Provides an interface for an object that can parse
    /// and apply custom INI attributes for controls.
    /// </summary>
    public interface IControlINIAttributeParser
    {
        bool ParseAttributeFromINI(XNAControl control, IniFile iniFile, string key, string value);
    }
}
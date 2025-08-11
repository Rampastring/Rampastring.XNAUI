using Microsoft.Xna.Framework.Content;
#if XNA
using System.Reflection;
#endif

namespace Rampastring.XNAUI
{
    public static class ContentManagerExtensions
    {
#if XNA
        private static readonly FieldInfo rootDirectoryField = typeof(ContentManager)
            .GetField("rootDirectory", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.SetField | BindingFlags.GetField);
        private static readonly FieldInfo fullRootDirectoryField = typeof(ContentManager)
            .GetField("fullRootDirectory", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.SetField | BindingFlags.GetField);
#endif

        /// <summary>
        /// Sets the root directory for the ContentManager with XNA vs MonoGame differences.
        /// </summary>
        /// <param name="contentManager">The ContentManager instance.</param>
        /// <param name="rootDirectory">The new root directory path.</param>
        public static void SetRootDirectory(this ContentManager contentManager, string rootDirectory)
        {
#if !XNA
            contentManager.RootDirectory = rootDirectory;
#else
            rootDirectoryField.SetValue(contentManager, rootDirectory);
            fullRootDirectoryField.SetValue(contentManager, rootDirectory);
#endif
        }
    }
}
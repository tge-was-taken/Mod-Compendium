using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace ModCompendiumLibrary.ModSystem.Builders.Utilities
{
    // https://stackoverflow.com/questions/19523419/unable-to-launch-shortcut-lnk-files-from-32-bit-c-sharp-application-when-the-f
    public static class ShortcutResolver
    {
        public static string ResolveShortcut( string filename )
        {
            // this gets the full path from a shortcut (.lnk file).
            ShellLink link = new ShellLink();
            ( ( IPersistFile )link ).Load( filename, STGM_READ );
            StringBuilder sb = new StringBuilder( MAX_PATH );
            WIN32_FIND_DATAW data = new WIN32_FIND_DATAW();
            ( ( IShellLinkW )link ).GetPath( sb, sb.Capacity, out data, 0 );
            string finalString = sb.ToString();
            if ( finalString.Length == 0 )
                finalString = filename;
            // If the the shortcut's target resolves to the Program Files or System32 directory, and the user is on a
            // 64-bit machine, the final string may actually point to C:\Program Files (x86) or C:\Windows\SYSWOW64.
            // This is due to File System Redirection in Windows -- http://msdn.microsoft.com/en-us/library/aa365743%28VS.85%29.aspx.
            // Unfortunately the solution there doesn't appear to work for 32-bit apps on 64-bit machines.
            // We will provide a workaround here:
            string newPath = ValidateShortcutPath( finalString, "SysWOW64", "System32" );
            if ( File.Exists( newPath ) && !File.Exists( finalString ) )
            {
                // the file is actually stored in System32 instead of SysWOW64. Let's update it.
                finalString = newPath;
            }
            newPath = ValidateShortcutPath( finalString, "Program Files (x86)", "Program Files" );
            if ( File.Exists( newPath ) && !File.Exists( finalString ) )
            {
                // the file is actually stored in Program Files instead of Program Files (x86). Let's update it.
                finalString = newPath;
            }
            // the lnk may incorrectly resolve to the C:\Windows\Installer directory. Check for this.
            if ( finalString.ToLower().IndexOf( "windows\\installer" ) > -1 )
                finalString = filename;
            if ( File.Exists( finalString ) )
                return finalString;
            else
                return filename;
        }

        public static string ValidateShortcutPath( string finalString, string findWhat, string replaceWith )
        {
            string finalStringLower = finalString.ToLower();
            string findWhatLower = findWhat.ToLower();
            int findValue = finalStringLower.IndexOf( findWhatLower );
            if ( findValue > -1 )
            {
                // the shortcut resolved to the findWhat directory, which can be SysWOW64 or Program Files (x86), 
                // but this may not be correct. Let's check by replacing it with another value.
                string newString = finalString.Substring( 0, findValue ) + replaceWith + finalString.Substring( findValue + findWhat.Length );
                if ( File.Exists( newString ) && !File.Exists( finalString ) )
                {
                    // the file is actually stored at a different location. Let's update it.
                    finalString = newString;
                }
            }
            return finalString;
        }

        [StructLayout( LayoutKind.Sequential, CharSet = CharSet.Auto )]
        private struct WIN32_FIND_DATAW
        {
            public uint dwFileAttributes;
            public long ftCreationTime;
            public long ftLastAccessTime;
            public long ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs( UnmanagedType.ByValTStr, SizeConst = 260 )]
            public string cFileName;
            [MarshalAs( UnmanagedType.ByValTStr, SizeConst = 14 )]
            public string cAlternateFileName;
        }

        private const int STGM_READ = 0;
        private const int MAX_PATH = 260;

        [Flags()]
        enum SLGP_FLAGS
        {
            /// <summary>Retrieves the standard short (8.3 format) file name</summary>
            SLGP_SHORTPATH = 0x1,
            /// <summary>Retrieves the Universal Naming Convention (UNC) path name of the file</summary>
            SLGP_UNCPRIORITY = 0x2,
            /// <summary>Retrieves the raw path name. A raw path is something that might not exist and may include environment variables that need to be expanded</summary>
            SLGP_RAWPATH = 0x4
        }

        [Flags()]
        enum SLR_FLAGS
        {
            /// <summary>
            /// Do not display a dialog box if the link cannot be resolved. When SLR_NO_UI is set,
            /// the high-order word of fFlags can be set to a time-out value that specifies the
            /// maximum amount of time to be spent resolving the link. The function returns if the
            /// link cannot be resolved within the time-out duration. If the high-order word is set
            /// to zero, the time-out duration will be set to the default value of 3,000 milliseconds
            /// (3 seconds). To specify a value, set the high word of fFlags to the desired time-out
            /// duration, in milliseconds.
            /// </summary>
            SLR_NO_UI = 0x1,
            /// <summary>Obsolete and no longer used</summary>
            SLR_ANY_MATCH = 0x2,
            /// <summary>If the link object has changed, update its path and list of identifiers.
            /// If SLR_UPDATE is set, you do not need to call IPersistFile::IsDirty to determine
            /// whether or not the link object has changed.</summary>
            SLR_UPDATE = 0x4,
            /// <summary>Do not update the link information</summary>
            SLR_NOUPDATE = 0x8,
            /// <summary>Do not execute the search heuristics</summary>
            SLR_NOSEARCH = 0x10,
            /// <summary>Do not use distributed link tracking</summary>
            SLR_NOTRACK = 0x20,
            /// <summary>Disable distributed link tracking. By default, distributed link tracking tracks
            /// removable media across multiple devices based on the volume name. It also uses the
            /// Universal Naming Convention (UNC) path to track remote file systems whose drive letter
            /// has changed. Setting SLR_NOLINKINFO disables both types of tracking.</summary>
            SLR_NOLINKINFO = 0x40,
            /// <summary>Call the Microsoft Windows Installer</summary>
            SLR_INVOKE_MSI = 0x80
        }

        /// <summary>The IShellLink interface allows Shell links to be created, modified, and resolved</summary>
        [ComImport(), InterfaceType( ComInterfaceType.InterfaceIsIUnknown ), Guid( "000214F9-0000-0000-C000-000000000046" )]
        private interface IShellLinkW
        {
            /// <summary>Retrieves the path and file name of a Shell link object</summary>
            void GetPath( [Out(), MarshalAs( UnmanagedType.LPWStr )] StringBuilder pszFile, int cchMaxPath, out WIN32_FIND_DATAW pfd, SLGP_FLAGS fFlags );
            /// <summary>Retrieves the list of item identifiers for a Shell link object</summary>
            void GetIDList( out IntPtr ppidl );
            /// <summary>Sets the pointer to an item identifier list (PIDL) for a Shell link object.</summary>
            void SetIDList( IntPtr pidl );
            /// <summary>Retrieves the description string for a Shell link object</summary>
            void GetDescription( [Out(), MarshalAs( UnmanagedType.LPWStr )] StringBuilder pszName, int cchMaxName );
            /// <summary>Sets the description for a Shell link object. The description can be any application-defined string</summary>
            void SetDescription( [MarshalAs( UnmanagedType.LPWStr )] string pszName );
            /// <summary>Retrieves the name of the working directory for a Shell link object</summary>
            void GetWorkingDirectory( [Out(), MarshalAs( UnmanagedType.LPWStr )] StringBuilder pszDir, int cchMaxPath );
            /// <summary>Sets the name of the working directory for a Shell link object</summary>
            void SetWorkingDirectory( [MarshalAs( UnmanagedType.LPWStr )] string pszDir );
            /// <summary>Retrieves the command-line arguments associated with a Shell link object</summary>
            void GetArguments( [Out(), MarshalAs( UnmanagedType.LPWStr )] StringBuilder pszArgs, int cchMaxPath );
            /// <summary>Sets the command-line arguments for a Shell link object</summary>
            void SetArguments( [MarshalAs( UnmanagedType.LPWStr )] string pszArgs );
            /// <summary>Retrieves the hot key for a Shell link object</summary>
            void GetHotkey( out short pwHotkey );
            /// <summary>Sets a hot key for a Shell link object</summary>
            void SetHotkey( short wHotkey );
            /// <summary>Retrieves the show command for a Shell link object</summary>
            void GetShowCmd( out int piShowCmd );
            /// <summary>Sets the show command for a Shell link object. The show command sets the initial show state of the window.</summary>
            void SetShowCmd( int iShowCmd );
            /// <summary>Retrieves the location (path and index) of the icon for a Shell link object</summary>
            void GetIconLocation( [Out(), MarshalAs( UnmanagedType.LPWStr )] StringBuilder pszIconPath,
                                  int cchIconPath, out int piIcon );
            /// <summary>Sets the location (path and index) of the icon for a Shell link object</summary>
            void SetIconLocation( [MarshalAs( UnmanagedType.LPWStr )] string pszIconPath, int iIcon );
            /// <summary>Sets the relative path to the Shell link object</summary>
            void SetRelativePath( [MarshalAs( UnmanagedType.LPWStr )] string pszPathRel, int dwReserved );
            /// <summary>Attempts to find the target of a Shell link, even if it has been moved or renamed</summary>
            void Resolve( IntPtr hwnd, SLR_FLAGS fFlags );
            /// <summary>Sets the path and file name of a Shell link object</summary>
            void SetPath( [MarshalAs( UnmanagedType.LPWStr )] string pszFile );

        }

        // CLSID_ShellLink from ShlGuid.h 
        [
            ComImport(),
            Guid( "00021401-0000-0000-C000-000000000046" )
        ]
        private class ShellLink
        {
        }

    }
}

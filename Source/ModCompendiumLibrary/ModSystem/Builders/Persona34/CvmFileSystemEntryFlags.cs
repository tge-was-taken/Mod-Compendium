using System;

namespace ModCompendiumLibrary.ModSystem.Builders
{
    [Flags]
    public enum CvmFileSystemEntryFlags : byte
    {
        FileRecord = 1 << 0,
        DirectoryRecord = 1 << 1
    }
}
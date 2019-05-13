using System;
using System.IO;
using ModCompendiumLibrary.Logging;
using ModCompendiumLibrary.VirtualFileSystem;
using CriPakTools;
using ModCompendiumLibrary.Configuration;
using System.Diagnostics;

namespace ModCompendiumLibrary.ModSystem.Builders
{
    [ModBuilder("Catherine Full Body Mod Builder", Game = Game.CatherineFullBody)]
    public abstract class CatherineFullBodyModBuilder : IModBuilder
    {
        protected abstract Game Game { get; }

        public bool OutputUnmodifiedFiles { get; } = true;
        /// <inheritdoc />
        public VirtualFileSystemEntry Build(VirtualDirectory root, string hostOutputPath = null, string gameName = null, bool useCompression = false, bool useExtracted = false)
        {
            gameName = Game.ToString();

            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            // Get game config
            var config = ConfigStore.Get(Game) as CatherineFullBodyGameConfig;
            if (config == null)
            {
                // Unlikely
                throw new InvalidOperationException("Game config is missing.");
            }

            if (string.IsNullOrWhiteSpace(config.CpkRootOrPath))
            {
                throw new InvalidConfigException("CPK path is not specified.");
            }

            // Create temp folder
            var cpkRootDirectoryPath = Path.Combine(Path.GetTempPath(), $"{Game}ModCompilerTemp_" + Path.GetRandomFileName());
            Log.Builder.Trace($"Creating temp output directory: {cpkRootDirectoryPath}");
            Directory.CreateDirectory(cpkRootDirectoryPath);

            // Get files from CPK
            VirtualDirectory cpkRootDirectory;

            Log.Builder.Trace($"{nameof(config.CpkRootOrPath)} = {config.CpkRootOrPath}");

            if (config.CpkRootOrPath.EndsWith(".cpk"))
            {
                // If extraction is enabled, use files from CPK path 
                useExtracted = Convert.ToBoolean(config.Extract);
                if (useExtracted)
                {
                    Log.Builder.Info($"Extracting CPK: {config.CpkRootOrPath}");

                    if (!File.Exists(config.CpkRootOrPath))
                    {
                        throw new InvalidConfigException($"CPK root path references a CPK file that does not exist: {config.CpkRootOrPath}.");
                    }
                
                    string[] args = { "-x", "-i", config.CpkRootOrPath, "-d", cpkRootDirectoryPath };
                    CriPakTools.Program.Main(args);
                }
                // Cpk file found & extracted, convert it to our virtual file system
                cpkRootDirectory = VirtualDirectory.FromHostDirectory(cpkRootDirectoryPath);
                cpkRootDirectory.Name = Path.GetFileNameWithoutExtension(config.CpkRootOrPath);
            }
            else
            {
                Log.Builder.Info($"Mounting directory: {config.CpkRootOrPath}");

                if (!Directory.Exists(config.CpkRootOrPath))
                {
                    throw new InvalidConfigException($"CPK root path references a directory that does not exist: {config.CpkRootOrPath}.");
                }

                // No CPK file found, assume files are extracted
                cpkRootDirectory = VirtualDirectory.FromHostDirectory(config.CpkRootOrPath);
                cpkRootDirectory.Name = Path.GetDirectoryName(config.CpkRootOrPath);
            }

            Log.Builder.Info("Processing mod files");
            foreach (var entry in root)
            {
                if (entry.EntryType == VirtualFileSystemEntryType.Directory)
                {
                    var directory = (VirtualDirectory)entry;
                    var name = directory.Name.ToLowerInvariant();

                    switch (name)
                    {
                        case "mod":
                        case "cache":
                        case "umd0":
                        case "umd1":
                        case "vita":
                        case "patch":
                        case "memst":
                            {
                                // Move files in 'cpk' directory to 'mod' directory
                                LogModFilesInDirectory(directory);

                                foreach (var modFileEntry in directory)
                                {
                                    modFileEntry.CopyTo(cpkRootDirectory);
                                }
                            }
                            break;

                        default:
                            // Move directory to 'mod' directory
                            Log.Builder.Trace($"Adding directory {entry.FullName} to {cpkRootDirectory.Name}.cpk");
                            entry.CopyTo(cpkRootDirectory);
                            break;
                    }
                }
                else
                {
                    // Move file to 'mod' directory
                    Log.Builder.Trace($"Adding file {entry.FullName} to {cpkRootDirectory.Name}.cpk");
                    entry.CopyTo(cpkRootDirectory);
                }
            }

            useCompression = Convert.ToBoolean(config.Compression);

            // Build mod cpk
            var cpkModCompiler = new CpkModBuilder();
            var cpkFilePath = hostOutputPath != null ? Path.Combine(hostOutputPath, $"{cpkRootDirectory.Name}.cpk") : null;
            var cpkFile = cpkModCompiler.Build(cpkRootDirectory, cpkFilePath, gameName, useCompression);
            Log.Builder.Info("Done!");
            return cpkFile;
        }

        private void LogModFilesInDirectory(VirtualDirectory directory)
        {
            foreach (var entry in directory)
            {
                if (entry.EntryType == VirtualFileSystemEntryType.File)
                    Log.Builder.Trace($"Adding mod file: {entry.FullName}");
                else
                    LogModFilesInDirectory((VirtualDirectory)entry);
            }
        }
    }
}
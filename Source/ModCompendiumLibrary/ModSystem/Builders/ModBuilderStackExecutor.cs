using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModCompendiumLibrary.VirtualFileSystem;

namespace ModCompendiumLibrary.ModSystem.Builders
{
    public class ModBuilderStackExecutor
    {
        public Stack<ModBuilderInfo> Stack { get; }

        public ModBuilderStackExecutor()
        {
            Stack = new Stack<ModBuilderInfo>();
        }

        public VirtualFileSystemEntry Execute(VirtualDirectory root, string hostOutputPath = null)
        {
            var input = root;
            VirtualFileSystemEntry output = null;

            while (Stack.Count != 0 )
            {
                var builderInfo = Stack.Pop();
                var builder = builderInfo.Create();
                output = builder.Build( input, hostOutputPath );
                if ( output.EntryType == VirtualFileSystemEntryType.File && Stack.Count != 0 )
                {
                    var temp = new VirtualDirectory();
                    output.MoveTo( temp );
                    input = temp;
                }
            }

            return output;
        }
    }
}

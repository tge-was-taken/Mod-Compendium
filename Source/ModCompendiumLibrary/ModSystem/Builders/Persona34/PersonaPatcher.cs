using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AmicitiaLibrary.FileSystems.CVM;
using AmicitiaLibrary.Utilities;

namespace ModCompendiumLibrary.ModSystem.Builders.Persona34
{
    public class PersonaPatcher
    {
        // Elf size constants
        public const int ELF_SIZE_PERSONA4_NTSC = 0x838C1C;
        public const int ELF_SIZE_PERSONA4_PAL = 0x83C19C;
        public const int ELF_SIZE_PERSONA3FES_NTSC = 0x8ACE9C;
        public const int ELF_SIZE_PERSONA3_NTSC = 0x79DD9C;

        // Cvm listing offset constants
        public const int CVM_LIST_OFFSET_PERSONA4_NTSC = 0x4598C0;
        public const int CVM_LIST_OFFSET_PERSONA4_PAL = 0x45CBC0;
        public const int CVM_LIST_OFFSET_PERSONA3FES_NTSC = 0x4E51D0;
        public const int CVM_LIST_OFFSET_PERSONA3_NTSC = 0x4E5FA0;

        // Cvm order
        public static readonly string[] CVM_ORDER_PERSONA4 = new string[4]
        {
        "DATA", "BGM", "BTL", "ENV"
        };

        public static readonly string[] CVM_ORDER_PERSONA3 = new string[3]
        {
        "DATA", "BGM", "BTL"
        };

        public static Dictionary<int, Tuple<int, string[]>> CvmListDataDictionary = new Dictionary<int, Tuple<int, string[]>>()
    {
        { ELF_SIZE_PERSONA4_NTSC,       Tuple.Create(CVM_LIST_OFFSET_PERSONA4_NTSC,     CVM_ORDER_PERSONA4) },
        { ELF_SIZE_PERSONA4_PAL,        Tuple.Create(CVM_LIST_OFFSET_PERSONA4_PAL,      CVM_ORDER_PERSONA4) },
        { ELF_SIZE_PERSONA3FES_NTSC,    Tuple.Create(CVM_LIST_OFFSET_PERSONA3FES_NTSC,  CVM_ORDER_PERSONA3) },
        { ELF_SIZE_PERSONA3_NTSC,       Tuple.Create(CVM_LIST_OFFSET_PERSONA3_NTSC,     CVM_ORDER_PERSONA3) }
    };

        //args: SLUS path, CVM path
        public static void Patch(string slusPath, string cvmPath)
        {
            // Declare variables
            byte[] elfHeader;
            byte[] elfFooter;
            CvmExecutableListing[] cvmExecutableListings;
            Tuple<int, string[]> data;

            using (FileStream stream = File.OpenRead(slusPath))
            {
                if (!CvmListDataDictionary.TryGetValue((int)stream.Length, out data))
                {
                    return;
                }

                // read data before list
                elfHeader = stream.ReadBytes(data.Item1);

                // Read cvm lists
                cvmExecutableListings = new CvmExecutableListing[data.Item2.Length];
                for (int i = 0; i < cvmExecutableListings.Length; i++)
                {
                    cvmExecutableListings[i] = new CvmExecutableListing(stream);
                }

                // read data after listing
                elfFooter = stream.ReadBytes((int)(stream.Length - stream.Position));
            }

            // Load cvm
            CvmFile cvm = new CvmFile(cvmPath);

            // Get the index from the cvm order
            // Check if the name of the cvm at least contains the original name
            string cvmName = Path.GetFileNameWithoutExtension(cvmPath).ToUpperInvariant();
            int cvmIndex = Array.FindIndex(data.Item2, o => cvmName.Contains(o));

            // Update the listing
            cvmExecutableListings[cvmIndex].Update(cvm);

            // Write the new executable
            using (BinaryWriter writer = new BinaryWriter(File.Create(slusPath)))
            {
                writer.Write(elfHeader);

                foreach (CvmExecutableListing cvmExecutableList in cvmExecutableListings)
                {
                    cvmExecutableList.Save(writer.BaseStream);
                }

                writer.Write(elfFooter);
            }
        }
    }
}

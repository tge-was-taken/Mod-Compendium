using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ModCompendiumLibrary.Configuration
{
    public abstract class ModCpkGameConfig : GameConfig
    {
        public string Compression { get; set; }

        protected override void DeserializeCore(XElement element)
        {
            Compression = element.GetElementValueOrEmpty(nameof(Compression));
        }

        protected override void SerializeCore(XElement element)
        {
            element.AddNameValuePair(nameof(Compression), Compression);
        }
    }

    public class Persona3DancingConfig : ModCpkGameConfig
    {
        public override Game Game => Game.Persona3Dancing;
    }

    public class Persona5DancingConfig : ModCpkGameConfig
    {
        public override Game Game => Game.Persona5Dancing;
    }

    public class Persona5GameConfig : ModCpkGameConfig
    {
        public override Game Game => Game.Persona5;
    }
}

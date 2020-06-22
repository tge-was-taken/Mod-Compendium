using System.Xml.Linq;

namespace ModCompendiumLibrary.Configuration
{
    public class Persona4GoldenConfig : PersonaPortableGameConfig
    {
        public override Game Game => Game.Persona4Golden;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace GTAdhocToolchain.Menu
{
    public class WidgetDefinitions
    {

        public static Dictionary<string, UIDefType> Types = new();

        public record UIFieldTypeOverride(string WidgetName, string FieldName, UIDefType ValueType);
        public static List<UIFieldTypeOverride> TypeOverrides = new();

        static WidgetDefinitions()
        {
            Read();
        }

        public static void Read()
        {
            string currentPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            string currentDir = Path.GetDirectoryName(currentPath);
            var txt = File.ReadAllLines(Path.Combine(currentDir, "GTAdhocToolchain.Menu/UIWidgetDefinitions.txt"));
            foreach (var line in txt)
            {
                if (string.IsNullOrEmpty(line) || line.StartsWith("//"))
                    continue;

                var spl = line.Split('|');
                if (spl.Length <= 1)
                    continue;

                if (spl[0] == "add_field" && spl.Length == 3)
                {
                    if (Enum.TryParse(spl[2], out UIDefType res))
                        Types.Add(spl[1], res);
                }
            }

            txt = File.ReadAllLines(Path.Combine(currentDir, "GTAdhocToolchain.Menu/UIWidgetDefinitionsTypeOverride.txt"));
            foreach (var line in txt)
            {
                if (string.IsNullOrEmpty(line) || line.StartsWith("//"))
                    continue;

                var spl = line.Split('|');
                if (spl.Length <= 1)
                    continue;

                if (spl[0] == "add_override" && spl.Length == 4)
                {
                    if (Enum.TryParse(spl[3], out UIDefType res))
                        TypeOverrides.Add(new UIFieldTypeOverride(spl[1], spl[2], res));
                }
            }
        }
    }

    public enum UIDefType
    {
        Unknown,
        Float,
        UInt,
        Int,
        Long,
        ULong,
        Double,
        Byte,
        SByte,
        Short,
        UShort,
        Bool,
    }
}

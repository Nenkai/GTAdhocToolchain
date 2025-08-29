using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace GTAdhocToolchain.Menu.Fields;

#pragma warning disable IDE1006 // Naming Styles
public abstract class mTypeBase
#pragma warning restore IDE1006 // Naming Styles
{
    public string Name { get; set; }
    public FieldType TypeOld { get; set; }
    public FieldType TypeNew { get; set; }

    public abstract void Read(MBinaryIO io);

    public abstract void Read(MTextIO io);

    public abstract void Write(MBinaryWriter writer);

    public abstract void WriteText(MTextWriter writer);

    public static mTypeBase FromTypeName(string fieldType)
    {
        return fieldType switch
        {
            "RGBA" => new mColor(),
            "color_name" => new mColorName(),
            "string" => new mString(),
            "region" => new mRegion(),
            "vector" => new mVector(),
            "vector3" => new mVector3(),
            "rectangle" => new mRectangle(),
            "ExternalRef" => new mExternalRef(),
            _ => new mNode(),
        };
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Globalization;

using GTAdhocToolchain.Core;

namespace GTAdhocToolchain.Menu.Fields;

[DebuggerDisplay("mRectangle: {Name} (X:{X1},Y:{Y1},W:{Width},H:{Height})")]
#pragma warning disable IDE1006 // Naming Styles
public class mRectangle : mTypeBase
#pragma warning restore IDE1006 // Naming Styles
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }

    public override void Read(MBinaryIO io)
    {
        if (io.Version == 0)
        {
            X = io.Stream.ReadSingle();
            Y = io.Stream.ReadSingle();
            Width = io.Stream.ReadSingle();
            Height = io.Stream.ReadSingle();
        }
        else
        {
            mFloat x = io.ReadNext() as mFloat;
            mFloat y = io.ReadNext() as mFloat;
            mFloat width = io.ReadNext() as mFloat;
            mFloat height = io.ReadNext() as mFloat;

            X = x.Value;
            Y = y.Value;
            Width = width.Value;
            Height = height.Value;
        }
    }

    public override void Read(MTextIO io)
    {
        var x = io.GetNumberToken();
        if (float.TryParse(x, out float xVal))
            X = xVal;
        else
            throw new UISyntaxError($"Unexpected token for mRectangle X. Got {x}.");

        var y = io.GetNumberToken();
        if (float.TryParse(y, out float yVal))
            Y = yVal;
        else
            throw new UISyntaxError($"Unexpected token for mRectangle Y. Got {x}.");

        var w = io.GetNumberToken();
        if (float.TryParse(w, out float wVal))
            Width = wVal;
        else
            throw new UISyntaxError($"Unexpected token for mRectangle Width. Got {w}.");

        var h = io.GetNumberToken();
        if (float.TryParse(h, out float hVal))
            Height = hVal;
        else
            throw new UISyntaxError($"Unexpected token for mRectangle Height. Got {h}.");

        string end = io.GetToken();
        if (end != MTextIO.SCOPE_END.ToString())
            throw new UISyntaxError($"Expected mRectangle scope end ({MTextIO.SCOPE_END}), got {end}");
    }

    public override void Write(MBinaryWriter writer)
    {
        if (writer.Version == 0)
        {
            throw new NotImplementedException();
        }
        else 
        {
            writer.Stream.WriteVarInt((int)FieldType.String);
            writer.Stream.WriteVarString("rectangle");

            writer.Stream.WriteVarInt((int)FieldType.Float);
            writer.Stream.WriteSingle(X);

            writer.Stream.WriteVarInt((int)FieldType.Float);
            writer.Stream.WriteSingle(Y);

            writer.Stream.WriteVarInt((int)FieldType.Float);
            writer.Stream.WriteSingle(Width);

            writer.Stream.WriteVarInt((int)FieldType.Float);
            writer.Stream.WriteSingle(Height);
        }
    }

    public override void WriteText(MTextWriter writer)
    {
        writer.WriteString(Name);
        writer.WriteSpace();
        writer.WriteString("rectangle");
        writer.WriteString("{"); writer.WriteString($"{X} {Y} {Width} {Height}"); writer.WriteString("}");
        writer.SetNeedNewLine();
    }
}

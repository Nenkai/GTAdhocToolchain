using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;
using GTAdhocToolchain.Core;

namespace GTAdhocToolchain.Menu.Fields;

[DebuggerDisplay("mNodeArray: {Name} ({Length} elements)")]
#pragma warning disable IDE1006 // Naming Styles
public class mArray : mTypeBase
#pragma warning restore IDE1006 // Naming Styles
{
    public int Length { get; set; }
    public List<mTypeBase> Elements { get; set; } = [];

    public override void Read(MBinaryIO io)
    {
        Length = (int)io.Stream.DecodeBitsAndAdvance();
        Elements = new List<mTypeBase>(Length);
        for (int i = 0; i < Length; i++)
        {
            if (io.Version == 0)
            {
                int endScope = io.Stream.ReadInt32();
                byte[] typeName = io.Stream.Read7BitStringBytes();

                mTypeBase element;
                if (typeName.Length == 1)
                {
                    element = (FieldTypeOld)typeName[0] switch
                    {
                        FieldTypeOld.String => new mString(),
                        FieldTypeOld.Array => new mArray(),
                        FieldTypeOld.Bool => new mBool(),
                        FieldTypeOld.Byte => new mUByte(),
                        FieldTypeOld.Double => new mDouble(),
                        FieldTypeOld.Float => new mFloat(),
                        FieldTypeOld.Long => new mLong(),
                        FieldTypeOld.SByte => new mSByte(),
                        FieldTypeOld.Short => new mShort(),
                        FieldTypeOld.UShort => new mUShort(),
                        FieldTypeOld.ULong => new mULong(),
                        FieldTypeOld.Int => new mInt(),
                        FieldTypeOld.UInt => new mUInt(),
                        _ => throw new NotSupportedException($"Received unsupported array element type {(FieldTypeOld)typeName[0]}"),
                    };
                }
                else
                {
                    string customFieldType = Encoding.UTF8.GetString(typeName);
                    element = mTypeBase.FromTypeName(customFieldType);

                    if (element is mNode nodeElement)
                    {
                        nodeElement.EndScopeOffset = endScope;
                        nodeElement.TypeName = customFieldType;
                    }
                    
                }

                element.Read(io);
                Elements.Add(element);
            }
            else
            {
                mTypeBase field;
                var type = (FieldType)io.Stream.ReadByte();
                field = type switch
                {
                    FieldType.UByte => new mUByte(),
                    FieldType.Bool => new mBool(),
                    FieldType.Short => new mShort(),
                    FieldType.UShort => new mUShort(),
                    FieldType.Float => new mFloat(),
                    FieldType.ULong => new mULong(),
                    FieldType.Long => new mLong(),
                    FieldType.Int => new mInt(),
                    FieldType.UInt => new mUInt(),
                    FieldType.String => new mString(),
                    FieldType.ArrayMaybe => new mArray(),
                    FieldType.ScopeStart => new mNode(),
                    FieldType.ScopeEnd => null,
                    FieldType.SByte => new mSByte(),
                    FieldType.ExternalRef => new mExternalRef(),
                    _ => throw new Exception($"Type: {type} not supported"),
                };

                if (type != FieldType.ScopeEnd)
                {
                    field.Read(io);
                    if (field is mString str)
                    {
                        if (str.String == "RGBA")
                        {
                            // Array of colors
                            field = new mColor();
                            field.Read(io);
                        }
                        else if (str.String == "color_name")
                        {
                            field = new mColorName();
                            field.Read(io);
                        }
                        else if (str.String == "vector")
                        {
                            field = new mVector();
                            field.Read(io);
                        }
                        else if (str.String == "vector3")
                        {
                            field = new mVector3();
                            field.Read(io);
                        }
                        else if (str.String == "region")
                        {
                            field = new mRegion();
                            field.Read(io);
                        }
                    }

                    Elements.Add(field);
                }
            }
        }
    }

    public override void Read(MTextIO io)
    {
        Elements = new List<mTypeBase>(Length);
        for (int i = 0; i < Length; i++)
        {
            string token = io.GetToken();
            string token2 = io.GetToken();

            mTypeBase element;
            if (token2 != MTextIO.SCOPE_START.ToString())
                throw new UISyntaxError($"Expected array element scope start, got {token2}.");

            // We only have the type
            if (token == "digit")
            {
                if (WidgetDefinitions.Types.TryGetValue(Name, out UIDefType digitType) && digitType != UIDefType.Unknown)
                {
                    element = digitType switch
                    {
                        UIDefType.Int => new mInt(),
                        UIDefType.UInt => new mUInt(),
                        UIDefType.Long => new mLong(),
                        UIDefType.ULong => new mULong(),
                        UIDefType.Short => new mShort(),
                        UIDefType.UShort => new mUShort(),
                        UIDefType.Byte => new mUByte(),
                        UIDefType.SByte => new mSByte(),
                        UIDefType.Float => new mFloat(),
                        UIDefType.Double => new mDouble(),
                        UIDefType.Bool => new mBool(),
                        _ => new mInt(),
                    };
                }
                else
                {
                    Console.WriteLine($"Missing digit type for '{Name}' in , assuming Int");
                    element = new mInt();
                }
            }
            else if (token.StartsWith(MTextIO.ARRAY_START)) // Array def
            {
                int arrLen = int.Parse(token.AsSpan(1, token.Length - 2));
                if (arrLen > byte.MaxValue)
                    throw new UISyntaxError($"Array length can only be {byte.MaxValue} elements maximum. Got {arrLen}.");

                element = new mArray();
                ((mArray)element).Length = (byte)arrLen;

                if (token2 != MTextIO.SCOPE_START.ToString())
                    throw new Exception($"Expected '{MTextIO.SCOPE_START}' character for node array field definition.");
            }
            else
            {
                element = mTypeBase.FromTypeName(token);

                if (element is mNode node)
                {
                    node.TypeName = token;
                }
            }
            
            element.Read(io);
            Elements.Add(element);
        }

        string endToken = io.GetToken();
        if (endToken != MTextIO.SCOPE_END.ToString())
            throw new UISyntaxError($"Expected array scope end, got {endToken}.");
    }

    public override void Write(MBinaryWriter writer)
    {
        if (writer.Version == 0)
        {
            throw new NotImplementedException();
        }
        else
        {
            writer.Stream.WriteVarInt((int)FieldType.ArrayMaybe);
            writer.Stream.WriteVarInt(Elements.Count);

            foreach (var element in Elements)
            {
                element.Write(writer);
            }
        }
    }

    public override void WriteText(MTextWriter writer)
    {
        writer.WriteString(Name);
        writer.WriteString($"[{Elements.Count}]");
        writer.WriteOpenScope();

        foreach (var elem in Elements)
        {
            elem.WriteText(writer);
        }

        writer.WriteEndScope();
    }

}

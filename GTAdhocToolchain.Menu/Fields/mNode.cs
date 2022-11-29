using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using GTAdhocToolchain.Menu.Fields;
using GTAdhocToolchain.Core;

namespace GTAdhocToolchain.Menu
{
    [DebuggerDisplay("mNode: {Name} ({TypeName})")]
    public class mNode : mTypeBase
    {
        public string TypeName { get; set; }

        public int EndScopeOffset { get; set; }

        public List<mTypeBase> Child { get; set; } = new List<mTypeBase>();

        // For old version reading
        public bool IsRoot { get; set; }

        public static HashSet<string> mStrings = new HashSet<string>();


        public override void Read(MBinaryIO io)
        {
            if (io.Version == 0)
            {
                if (IsRoot)
                {
                    EndScopeOffset = io.Stream.ReadInt32();
                    TypeName = io.Stream.Read7BitString();
                }

                while (io.Stream.Position + 2 < EndScopeOffset)
                {
                    string fieldName = io.Stream.Read7BitString();

                    int endOffset = io.Stream.ReadInt32();

                    mTypeBase field;
                    byte[] typeName = io.Stream.Read7BitStringBytes();
                    if (typeName.Length == 1)
                    {
                        FieldTypeOld old = (FieldTypeOld)typeName[0];
                        field = old switch
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
                            _ => throw new NotSupportedException($"Received unsupported node type {old}"),
                        };
                    }
                    else
                    {
                        string customFieldType = Encoding.UTF8.GetString(typeName);

                        field = customFieldType switch
                        {
                            "rectangle" => new mRectangle(),
                            "RGBA" => new mColor(),
                            "color_name" => new mColorName(),
                            "vector" => new mVector(),
                            "vector3" => new mVector3(),
                            "region" => new mRegion(),
                            _ => null,
                        };

                        if (field is null)
                        {
                            field = new mNode();
                            ((mNode)field).EndScopeOffset = endOffset;
                            ((mNode)field).TypeName = customFieldType;
                        }
                    }

                    field.Name = fieldName;
                    //Console.WriteLine($"Reading: {field.Name} ({field})");
                    field.Read(io);

                    Child.Add(field);
                }

                Debug.Assert(io.Stream.ReadInt16() == 0x18d, "Scope terminator did not match");
            }
            else if (io.Version == 1)
            {
                TypeName = io.Stream.Read7BitString();

                // Grab roots
                if (TypeName.StartsWith("ComponentsProject"))
                {
                    string[] spl = TypeName.Split("::");
                    if (spl.Length == 2)
                    {
                        if (!mStrings.Contains(spl[^1]))
                            mStrings.Add(spl[^1]);
                    }
                }

                mTypeBase field = null;
                do
                {
                    field = io.ReadNext();
                    if (field is mString str) // Key Name
                    {
                        io.CurrentKeyName = str.String;
                        field = io.ReadNext();

                        // Grab roots
                        if (TypeName == "RootWindow" && field is mString)
                        {
                            var rootName = ((mString)field).String.Substring(((mString)field).String.IndexOf("::") + 1).TrimStart(':');

                            if (!rootName.StartsWith("alias_") && !mStrings.Contains(rootName))
                                mStrings.Add(rootName);
                        }

                        if (field is mString str2 && io.CurrentKeyName != "name")
                        {
                            // Specific types, kind of hardcoded
                            if (str2.String == "rectangle")
                            {
                                field = new mRectangle();
                                field.Read(io);
                            }
                            else if (str2.String == "RGBA")
                            {
                                field = new mColor();
                                field.Read(io);
                            }
                            else if (str2.String == "color_name")
                            {
                                field = new mColorName();
                                field.Read(io);
                            }
                            else if (str2.String == "vector")
                            {
                                field = new mVector();
                                field.Read(io);
                            }
                            else if (str2.String == "vector3")
                            {
                                field = new mVector3();
                                field.Read(io);
                            }
                            else if (str2.String == "region")
                            {
                                field = new mRegion();
                                field.Read(io);
                            }
                        }
                        
                        field.Name = str.String;
                    }

                    if (field != null)
                        Child.Add(field);

                } while (field != null);
            }
        }

        public override void Read(MTextIO io)
        {
            string token = null;
            string token2 = null;

            if (IsRoot)
            {
                token = io.GetToken();
                token2 = io.GetToken();

                if (token2 == MTextIO.SCOPE_START.ToString())
                {
                    // We only have the type
                    TypeName = token;
                }
                else
                {
                    Name = token;
                    TypeName = token2;
                    string token3 = io.GetToken();
                    if (token3 != MTextIO.SCOPE_START.ToString())
                        throw new Exception($"Expected '{MTextIO.SCOPE_START}' character for node definition.");

                }
            }

            mTypeBase field = null;
            while (true)
            {
                // Loop through all fields, now
                token = io.GetToken();
                if (token == MTextIO.SCOPE_END.ToString())
                    break;

                token2 = io.GetToken();

                if (token2 == MTextIO.SCOPE_START.ToString())
                {
                    // We only have the type
                    field = new mNode();
                    ((mNode)field).TypeName = token;
                }
                else if (token2.StartsWith(MTextIO.ARRAY_START)) // Array def
                {
                    int arrLen = int.Parse(token2.AsSpan(1, token2.Length - 2));
                    if (arrLen > byte.MaxValue)
                        throw new UISyntaxError($"Array length can only be {byte.MaxValue} elements maximum. Got {arrLen}.");

                    field = new mArray();
                    ((mArray)field).Length = (byte)arrLen;

                    if (io.GetToken() != MTextIO.SCOPE_START.ToString())
                        throw new Exception($"Expected '{MTextIO.SCOPE_START}' character for node array field definition.");

                    field.Name = token;
                }
                else
                {
                    string fieldName = token;
                    string fieldType = token2;
                    string token3 = io.GetToken();
                    if (token3 != MTextIO.SCOPE_START.ToString())
                        throw new Exception($"Expected '{MTextIO.SCOPE_START}' character for node field definition.");

                    if (fieldType == "digit")
                    {
                        // Search potentially colliding field names with different types
                        UIDefType digitType;
                        var potentialOverride = WidgetDefinitions.TypeOverrides.FirstOrDefault(e => e.WidgetName == TypeName && e.FieldName == fieldName);
                        if (potentialOverride != null)
                            digitType = potentialOverride.ValueType;
                        else 
                            WidgetDefinitions.Types.TryGetValue(fieldName, out digitType); // No collision, just try to find it by name

                        /* No real easier way, the component types i.e 'DialogParts::DialogFrame::Pane::Head::Close::Cross' do not expose their actual type */

                        if (digitType != UIDefType.Unknown)
                        {
                            field = digitType switch
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
                            Console.WriteLine($"Missing digit type for '{fieldName}', assuming Int");
                            field = new mInt();
                        }
                    }
                    else
                    {
                        field = fieldType switch
                        {
                            "RGBA" => new mColor(),
                            "color_name" => new mColorName(),
                            "string" => new mString(),
                            "region" => new mRegion(),
                            "vector" => new mVector(),
                            "vector3" => new mVector3(),
                            "rectangle" => new mRectangle(),
                            _ => new mNode(),
                        };

                    }

                    if (field is mNode)
                    {
                        ((mNode)field).TypeName = token2;
                    }

                    field.Name = token;
                }

                field.Read(io);
                Child.Add(field);
            }
        }

        public override void Write(MBinaryWriter writer)
        {
            if (writer.Version == 0)
                throw new NotImplementedException();
            else
            {
                writer.Stream.WriteVarInt((int)FieldType.ScopeStart);
                writer.Stream.WriteVarString(TypeName);

                foreach (var field in Child)
                {
                    writer.Stream.WriteVarInt((int)FieldType.String);
                    writer.Stream.WriteVarString(field.Name);
                    field.Write(writer);
                }

                writer.Stream.WriteVarInt((int)FieldType.ScopeEnd);
            }
        }

        public override void WriteText(MTextWriter writer)
        {
            if (!string.IsNullOrEmpty(Name))
            {
                writer.WriteString(Name);
                writer.WriteSpace();
            }

            if (TypeName.Contains("::"))
                writer.WriteString($"\'{TypeName}\'");
            else
                writer.WriteString(TypeName);

            writer.WriteOpenScope();

            foreach (var node in Child)
                node.WriteText(writer);

            writer.WriteEndScope();
        }
    }
}

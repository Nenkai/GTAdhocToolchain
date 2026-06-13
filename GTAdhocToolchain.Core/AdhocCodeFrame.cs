using GTAdhocToolchain.Core.Instructions;
using GTAdhocToolchain.Core.Variables;

using Syroot.BinaryData;

using System.Text;

namespace GTAdhocToolchain.Core;

/// <summary>
/// Represents a frame of instructions. May be a function, method, or main.
/// </summary>
public class AdhocCodeFrame
{
    public Dictionary<AdhocSymbol, Variable> Variables { get; set; } = [];

    /// <summary>
    /// GT7.
    /// </summary>
    public const int ADHOC_VERSION_LATEST = 15;

    /// <summary>
    /// Default adhoc version, 12. Compatible with GT5P, GT5, GTPSP, GT6, GT7SP.
    /// </summary>
    public const int ADHOC_VERSION_DEFAULT = 12;

    public AdhocVersion Version { get; set; } = new AdhocVersion(12);

    /// <summary>
    /// Current instructions for this block.
    /// </summary>
    public List<InstructionBase> Instructions { get; set; } = [];

    /// <summary>
    /// Function or method parameters
    /// </summary>
    public List<AdhocSymbol> FunctionParameters { get; set; } = [];

    /// <summary>
    /// Captured variables for function consts
    /// </summary>
    public List<(int StackIndex, AdhocSymbol Symbol)> CapturedCallbackVariables { get; set; } = [];

    public int MaxLocalIndex { get; set; }
    public int MaxStaticIndex { get; set; }
    public int MaxStackIndex { get; set; }

    /// <summary>
    /// Source file for this block.
    /// </summary>
    public AdhocSymbol SourceFilePath { get; set; }

    public uint InstructionCountOffset { get; set; }
    public bool HasDebuggingInformation { get; set; } = true;

    /// <summary>
    /// Version 12 only. Indicatesd a subroutine with a rest/params[] element/argument.
    /// </summary>
    public bool HasRestElement { get; set; }

    public AdhocCodeFrame(AdhocVersion version)
    {
        Version = version;
    }

    public void SetSourcePath(AdhocSymbol symbol)
    {
        SourceFilePath = symbol;
    }

    public int GetInstructionCount()
    {
        return Instructions.Count;
    }

    public void Write(AdhocStream stream)
    {
        if (Version.VersionNumber >= 8 && Version.VersionNumber <= 12)
        {
            stream.WriteBoolean(HasDebuggingInformation);
            stream.WriteByte((byte)Version.VersionNumber);
        }

        stream.WriteSymbol(SourceFilePath);

        if (Version.SupportsRestElement())
            stream.WriteBoolean(HasRestElement); // Not sure for Version 14

        if (Version.VersionNumber > 3)
        {
            stream.WriteInt32(FunctionParameters.Count);
            for (int i = 0; i < FunctionParameters.Count; i++)
            {
                stream.WriteSymbol(FunctionParameters[i]);

                if (Version.VersionNumber >= 8)
                    stream.WriteInt32(1 + i); // TODO: Proper Index?
            }
        }

        if (Version.VersionNumber >= 8)
        {
            stream.WriteInt32(CapturedCallbackVariables.Count);
            for (int i = 0; i < CapturedCallbackVariables.Count; i++)
            {
                (int StackIndex, AdhocSymbol Symbol) capturedVariable = CapturedCallbackVariables[i];
                stream.WriteSymbol(capturedVariable.Symbol);
                stream.WriteInt32(capturedVariable.StackIndex);
            }

            stream.WriteInt32(0); // Some stack variable index
        }


        if (Version.UsesNewSplitStack())
        {
            // Actual stack size
            stream.WriteInt32(MaxStackIndex);

            /* These two are combined to make the size of the storage for variables */
            stream.WriteInt32(MaxLocalIndex);
            stream.WriteInt32(MaxStaticIndex);
        }
        else
        {
            stream.WriteInt32(MaxLocalIndex);
            stream.WriteInt32(MaxStackIndex);
        }

        stream.WriteInt32(Instructions.Count);
        foreach (var instruction in Instructions)
        {
            stream.WriteUInt32(instruction.LineNumber);
            stream.WriteByte((byte)instruction.InstructionType);
            instruction.Serialize(stream);
        }
    }

    public void Read(AdhocStream stream)
    {
        if (Version.VersionNumber < 8)
        {
            HasDebuggingInformation = true; // Not in code paths, but its forced to read
            SourceFilePath = stream.ReadSymbol();

            if (Version.VersionNumber > 3)
            {
                uint argCount = stream.ReadUInt32();
                for (int i = 0; i < argCount; i++)
                    FunctionParameters.Add(stream.ReadSymbol());
            }
        }
        else
        {

            HasDebuggingInformation = stream.ReadBoolean();
            Version = new AdhocVersion((uint)stream.ReadByte());

            if (Version.VersionNumber != 8) // Why PDI? Changed your mind after 8?
            {
                if (HasDebuggingInformation)
                    SourceFilePath = stream.ReadSymbol();
            }


            if (Version.SupportsRestElement())
                HasRestElement = stream.ReadBoolean();

            uint argCount = stream.ReadUInt32();

            if (argCount > 0)
            {
                for (int i = 0; i < argCount; i++)
                {
                    var symbol = stream.ReadSymbol();
                    FunctionParameters.Add(new AdhocSymbol(stream.ReadInt32(), symbol.Name));
                }
            }

            uint funcArgs = stream.ReadUInt32();
            if (funcArgs > 0)
            {
                for (int i = 0; i < funcArgs; i++)
                {
                    var symbol = stream.ReadSymbol();
                    int stackIndex = stream.ReadInt32();
                    CapturedCallbackVariables.Add((stackIndex, symbol));
                }
            }

            uint unkVarStackIndex = stream.ReadUInt32();
        }

        if (!Version.UsesNewSplitStack())
        {
            MaxStackIndex = stream.ReadInt32();
            MaxLocalIndex = stream.ReadInt32();
        }
        else
        {
            MaxStackIndex = stream.ReadInt32();
            MaxLocalIndex = stream.ReadInt32();
            MaxStackIndex = stream.ReadInt32();
        }

        InstructionCountOffset = (uint)stream.Position;
        uint instructionCount = stream.ReadUInt32();
        if (instructionCount < 0x40000000)
        {
            for (int i = 0; i < instructionCount; i++)
            {
                uint originalLineNumber = 0;
                if (HasDebuggingInformation)
                    originalLineNumber = stream.ReadUInt32();

                AdhocInstructionType type = (AdhocInstructionType)stream.ReadByte();

                ReadInstruction(stream, originalLineNumber, type);
            }
        }
    }

    public void ReadInstruction(AdhocStream stream, uint lineNumber, AdhocInstructionType type)
    {
        InstructionBase ins = InstructionBase.GetByType(type);
        if (ins != null)
        {
            ins.InstructionOffset = (uint)stream.Position + 4;
            ins.LineNumber = lineNumber;
            ins.Deserialize(stream);
            Instructions.Add(ins);
        }
    }

    public string Dissasemble(bool asCompareMode = false)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append('(');
        if (FunctionParameters.Count != 0)
        {
            for (int i = 0; i < FunctionParameters.Count; i++)
            {
                sb.Append(FunctionParameters[i].Name);//.Append($"[{FunctionParameters[i].Id}]");
                if (i != FunctionParameters.Count - 1)
                    sb.Append(", ");
            }

            if (HasRestElement)
                sb.Append("...");
        }

        sb.Append(')');
        if (CapturedCallbackVariables.Count != 0)
        {
            sb.Append('[');
            for (int i = 0; i < CapturedCallbackVariables.Count; i++)
            {
                sb.Append(CapturedCallbackVariables[i].Symbol);//.Append($"[{CapturedCallbackVariables[i].Id}]");
                if (i != CapturedCallbackVariables.Count - 1)
                    sb.Append(", ");
            }
            sb.Append(']');
        }

        sb.AppendLine();

        sb.Append("  > Instruction Count: ").Append(Instructions.Count);
        if (!asCompareMode)
            sb.Append(" (").Append(InstructionCountOffset.ToString("X2")).Append(')');
        sb.AppendLine();

        sb.Append($"  > Stack Size: {MaxStackIndex} - MaxLocalIndex: {MaxLocalIndex} - MaxStaticIndex: {(!Version.UsesNewSplitStack() ? "=MaxLocalIndex" : $"{MaxStaticIndex}")}");

        return sb.ToString();
    }
}

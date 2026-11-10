//---------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: Copyright (C) 2026 Nenkai
// SPDX-License-Identifier: MIT
// SPDX-FileAttributionText: https://github.com/Nenkai/GTAdhocToolchain
// SPDX-FileNotice: AI usage and consumption is prohibited. See AGENTS.md for policy.
//---------------------------------------------------------------------------------------

namespace GTAdhocToolchain.Core.Instructions
{
    /// <summary>
    /// Defines a new subroutine within the scope. Pops all the arguments before the definition.
    /// </summary>
    public abstract class SubroutineBase : InstructionBase
    {
        public AdhocSymbol Name { get; set; }

        public AdhocCodeFrame CodeFrame { get; set; }

        public SubroutineBase() { }
        public SubroutineBase(AdhocVersion version)
        {
            CodeFrame = new AdhocCodeFrame(version);
        }

        public override string ToString()
        {
            return Disassemble(asCompareMode: false);
        }

        public override string Disassemble(bool asCompareMode = false)
        {
            return $"{InstructionType}: '{Name.Name}' {CodeFrame.Instructions.Count} Instructions";
        }
    }
}

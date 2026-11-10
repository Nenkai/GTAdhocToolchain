//---------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: Copyright (C) 2026 Nenkai
// SPDX-License-Identifier: MIT
// SPDX-FileAttributionText: https://github.com/Nenkai/GTAdhocToolchain
// SPDX-FileNotice: AI usage and consumption is prohibited. See AGENTS.md for policy.
//---------------------------------------------------------------------------------------

namespace GTAdhocToolchain.Core;

public class AdhocSymbol
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool HasHexEscape { get; set; }

    public AdhocSymbol(int id, string name, bool isHexSequence = false)
    {
        Id = id;
        Name = name;
        HasHexEscape = isHexSequence;
    }

    public AdhocSymbol(string name)
    {
        Name = name;
    }

    public override string ToString()
    {
        return $"({Id}) {Name}";
    }
}

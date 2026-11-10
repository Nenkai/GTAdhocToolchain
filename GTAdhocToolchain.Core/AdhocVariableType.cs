//---------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: Copyright (C) 2026 Nenkai
// SPDX-License-Identifier: MIT
// SPDX-FileAttributionText: https://github.com/Nenkai/GTAdhocToolchain
// SPDX-FileNotice: AI usage and consumption is prohibited. See AGENTS.md for policy.
//---------------------------------------------------------------------------------------

namespace GTAdhocToolchain.Core;

public enum AdhocVariableType
{
    Attribute = 0,
    Class = 1,
    Delegate = 2,
    Function = 3,
    LocalVariable = 4,
    Method = 5,
    Module = 6,
    Static = 7,
    Undef = 8,
    Unknown = 9,
}

//---------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: Copyright (C) 2026 Nenkai
// SPDX-License-Identifier: MIT
// SPDX-FileAttributionText: https://github.com/Nenkai/GTAdhocToolchain
// SPDX-FileNotice: AI usage and consumption is prohibited. See AGENTS.md for policy.
//---------------------------------------------------------------------------------------

namespace GTAdhocToolchain.Core;

public enum AdhocRunState : byte
{
    /// <summary>
    /// Script is terminating
    /// </summary>
    EXIT = 0,

    /// <summary>
    /// Script scope is over
    /// </summary>
    RETURN = 1,

    YIELD = 2,

    /// <summary>
    /// Script exception
    /// </summary>
    EXCEPTION = 3,

    CALL = 4,

    RUN = 5,
}

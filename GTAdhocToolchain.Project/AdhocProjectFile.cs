//---------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: Copyright (C) 2026 Nenkai
// SPDX-License-Identifier: MIT
// SPDX-FileAttributionText: https://github.com/Nenkai/GTAdhocToolchain
// SPDX-FileNotice: AI usage and consumption is prohibited. See AGENTS.md for policy.
//---------------------------------------------------------------------------------------

namespace GTAdhocToolchain.Project;

public class AdhocProjectFile
{
    public string FullPath { get; set; }
    public string SourcePath { get; set; }
    public string Name { get; set; }

    /// <summary>
    /// Whether this is the main file of a project.
    /// </summary>
    public bool IsMain { get; set; }

    /// <summary>
    /// Whether this is a project component.
    /// </summary>
    public bool ProjectComponent { get; set; }
}

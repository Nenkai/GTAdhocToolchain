//---------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: Copyright (C) 2026 Nenkai
// SPDX-License-Identifier: MIT
// SPDX-FileAttributionText: https://github.com/Nenkai/GTAdhocToolchain
// SPDX-FileNotice: AI usage and consumption is prohibited. See AGENTS.md for policy.
//---------------------------------------------------------------------------------------

namespace GTAdhocToolchain.Packaging;

public class AdhocPackageFile
{
    public uint FileNameOffset { get; set; }
    public uint CompressedDataOffset { get; set; }
    public uint CompressedDataSize { get; set; }
    public string RawFileName { get; set; }
    public string ProjectFileName { get; set; }
}

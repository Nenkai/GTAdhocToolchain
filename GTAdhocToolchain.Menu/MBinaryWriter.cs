//---------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: Copyright (C) 2026 Nenkai
// SPDX-License-Identifier: MIT
// SPDX-FileAttributionText: https://github.com/Nenkai/GTAdhocToolchain
// SPDX-FileNotice: AI usage and consumption is prohibited. See AGENTS.md for policy.
//---------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Syroot.BinaryData;

using GTAdhocToolchain.Core;
using GTAdhocToolchain.Menu.Fields;

namespace GTAdhocToolchain.Menu;

public class MBinaryWriter : IDisposable
{
    public string OutputFileName { get; set; }

    public bool Debug { get; set; }

    public AdhocStream Stream { get; set; }

    public int Version { get; set; }

    public MBinaryWriter(string fileName)
    {
        OutputFileName = fileName;
    }

    public void WriteNode(mNode node)
    {
        using var fs = new FileStream(OutputFileName, FileMode.Create);
        Stream = new AdhocStream(fs, new AdhocVersion(1)) { BigEndian = true };

        Stream.WriteString("MPRJ", StringCoding.Raw);
        Stream.WriteVarInt((int)Version);

        node.Write(this);
        Stream.Flush();
    }

    public void Dispose()
    {
        Stream?.Dispose();
        GC.SuppressFinalize(this);
    }
}

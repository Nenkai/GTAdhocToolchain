﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

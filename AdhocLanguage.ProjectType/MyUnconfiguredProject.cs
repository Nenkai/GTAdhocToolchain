/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

namespace AdhocLanguage
{
    using System;
    using System.ComponentModel.Composition;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;

    using Microsoft.VisualStudio.ProjectSystem;
    using Microsoft.VisualStudio.ProjectSystem.VS;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Task = System.Threading.Tasks.Task;

    [Export]
    [AppliesTo(MyUnconfiguredProject.UniqueCapability)]
    [ProjectTypeRegistration(AdhocPackage.ProjectTypeGuid, "Adhoc", "Adhoc Project Files (*.adproj);*.adproj", ProjectExtension, Language, resourcePackageGuid: AdhocPackage.PackageGuidString, PossibleProjectExtensions = ProjectExtension, ProjectTemplatesDir = @"..\..\Templates\Projects\MyCustomProject")]
    [ProvideProjectItem(AdhocPackage.ProjectTypeGuid, "My Items", @"..\..\Templates\ProjectItems\MyCustomProject", 500)]
    internal class MyUnconfiguredProject
    {
        /// <summary>
        /// The file extension used by your project type.
        /// This does not include the leading period.
        /// </summary>
        internal const string ProjectExtension = "adproj";

        /// <summary>
        /// A project capability that is present in your project type and none others.
        /// This is a convenient constant that may be used by your extensions so they
        /// only apply to instances of your project type.
        /// </summary>
        /// <remarks>
        /// This value should be kept in sync with the capability as actually defined in your .targets.
        /// </remarks>
        internal const string UniqueCapability = "AdhocLanguage";

        internal const string Language = "Adhoc";

        [ImportingConstructor]
        public MyUnconfiguredProject(UnconfiguredProject unconfiguredProject)
        {
            this.ProjectHierarchies = new OrderPrecedenceImportCollection<IVsHierarchy>(projectCapabilityCheckProvider: unconfiguredProject);
        }

        public MyUnconfiguredProject()
        {
            ;
        }

        [Import]
        internal UnconfiguredProject UnconfiguredProject { get; set; }

        [Import]
        internal IActiveConfiguredProjectSubscriptionService SubscriptionService { get; set; }

        [Import]
        internal IProjectThreadingService ThreadHandling { get; set; }

        [Import]
        internal ActiveConfiguredProject<ConfiguredProject> ActiveConfiguredProject { get; set; }

        [Import]
        internal ActiveConfiguredProject<MyConfiguredProject> MyActiveConfiguredProject { get; set; }

        [ImportMany(ExportContractNames.VsTypes.IVsProject, typeof(IVsProject))]
        internal OrderPrecedenceImportCollection<IVsHierarchy> ProjectHierarchies { get; }

        internal IVsHierarchy ProjectHierarchy
        {
            get { return this.ProjectHierarchies.Single().Value; }
        }

        private async 
    }
}

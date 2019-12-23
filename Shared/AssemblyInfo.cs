using System.Reflection;
using System.Runtime.InteropServices;

/*
 * This information is shared across all projects, so it is included in the
 * template project and (hopefully) all other projects.
 */
[assembly: AssemblyProduct("Assorted Plugins for paint.net")]
[assembly: AssemblyCompany("Sam McCreery")]
[assembly: AssemblyCopyright("© 2019 Sam McCreery")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: ComVisible(false)]

using System;                           
using System.Reflection;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("ghgl")]
[assembly: AssemblyDescription("OpenGL shader components for GH")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Robert McNeel & Associates")]
[assembly: AssemblyProduct("ghgl")]
[assembly: AssemblyCopyright("Copyright ©  2019")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("48d4ff98-7772-4b59-88e2-7176f2e177cb")]

[assembly: AssemblyVersion("7.0.0.0")]


namespace ghgl
{
    public class LibraryInfo : Grasshopper.Kernel.GH_AssemblyInfo
    {
        public override string Name => "ghgl";

        /// <summary>Return a 24x24 pixel bitmap to represent this GHA library</summary>
        public override System.Drawing.Bitmap Icon => null;

        /// <summary>Return a short string describing the purpose of this GHA library</summary>
        public override string Description => "OpenGL shader components for GH";

        public override Guid Id => new Guid("835c3e29-712f-4585-9d0b-8192359ad167");

        public override string AuthorName => "Robert McNeel & Associates";

        public override string AuthorContact => "https://github.com/mcneel/ghgl";

        public override string AssemblyVersion
        {
            get
            {
                var t = typeof(LibraryInfo).Assembly.GetName().Version;
                return $"{t.Major}.{t.Minor}.{t.Build}";
            }
        }
    }
}

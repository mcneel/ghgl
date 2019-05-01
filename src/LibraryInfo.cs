using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace ghgl
{
    public class LibraryInfo : GH_AssemblyInfo
    {
        public override string Name => "ghgl";

        /// <summary>Return a 24x24 pixel bitmap to represent this GHA library</summary>
        public override Bitmap Icon => null;

        /// <summary>Return a short string describing the purpose of this GHA library</summary>
        public override string Description => "OpenGL shader components for GH";

        public override Guid Id => new Guid("835c3e29-712f-4585-9d0b-8192359ad167");

        public override string AuthorName => "Robert McNeel & Associates";

        public override string AuthorContact => "https://github.com/mcneel/ghgl";

        public override string AssemblyVersion {
            get {
                var t = typeof(LibraryInfo).Assembly.GetName().Version;
                return $"{t.Major}.{t.Minor}.{t.Build}";
            }
        }
    }
}

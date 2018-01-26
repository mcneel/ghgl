using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace ghgl
{
    public class LibraryInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "ghgl";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return null;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "OpenGL shader components for GH";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("835c3e29-712f-4585-9d0b-8192359ad167");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "Robert McNeel & Associates";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "https://github.com/mcneel/ghgl";
            }
        }
    }
}

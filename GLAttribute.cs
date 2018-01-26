using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ghgl
{
    class GLAttribute<T>
    {
        uint _vboHandle;
        public GLAttribute(string name, int location, T[] items)
        {
            Name = name;
            Location = location;
            Items = items;
        }
        public string Name { get; }
        public int Location { get; set; }
        public T[] Items { get; }
        public uint VboHandle
        {
            get { return _vboHandle; }
            set
            {
                if (_vboHandle != value)
                {
                    GLRecycleBin.AddVboToDeleteList(_vboHandle);
                    _vboHandle = value;
                }
            }
        }
    }
}

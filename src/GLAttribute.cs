
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

        public string ToJsonString(int indent)
        {
            var sb = new System.Text.StringBuilder();
            string padding = "".PadLeft(indent);
            sb.AppendLine(padding + $"{Name} : new Float32Array([");
            padding = "".PadLeft(indent + 2);
            int lineBreakOn = 6;
            bool startLine = true;
            for (int i = 0; i < Items.Length; i++)
            {
                if (startLine)
                    sb.Append(padding);
                startLine = false;
                sb.Append(Items[i].ToString());
                if (i < (Items.Length - 1))
                    sb.Append(",");
                if (i % lineBreakOn == lineBreakOn)
                {
                    sb.AppendLine();
                    startLine = true;
                }
            }
            if (!startLine)
                sb.AppendLine();
            
            sb.Append("".PadLeft(indent) + "])");
            return sb.ToString();
        }

    }
}

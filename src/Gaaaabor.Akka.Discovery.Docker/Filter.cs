using System.Collections.Generic;

namespace Gaaaabor.Akka.Discovery.Docker
{
    public class Filter
    {
        public string Name { get; }
        public List<string> Values { get; } = new List<string>();

        public Filter(string name)
        {
            Name = name;
        }

        public Filter(string name, List<string> values) : this(name)
        {
            Values = values;
        }

        public Filter(string name, string value) : this(name)
        {
            Values.Add(value);
        }
    }
}

using System.Collections.Generic;

namespace Generator.CustomAttribute.T4
{
    partial class TemplateClassCollector
    {
        public string Namespace { get; set; }
        public string Name { get; set; }

        public List<(int codeId, string effectCodeImpl)> Methods { get; set; } = new();
    }
}

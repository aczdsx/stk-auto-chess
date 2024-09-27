using System.Collections.Generic;

namespace Generator.InheritOverrideFlag.T4
{
    partial class TemplateInheritOverrideFlagGetter
    {
        public string Namespace { get; set; }
        public string Name { get; set; }
        public string EnumFlagName { get; set; }

        public List<string> Flags { get; set; } = new();
    }
}

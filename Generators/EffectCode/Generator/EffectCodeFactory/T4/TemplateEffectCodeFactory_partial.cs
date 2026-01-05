using System.Collections.Generic;

namespace Generator.EffectCodeFactory.T4
{
    partial class TemplateEffectCodeFactory
    {
        public string Namespace { get; set; }
        public string Name { get; set; }

        public string EffectCodeBaseClassName { get; set; }
        
        public List<(int codeId, string effectCodeImpl)> Methods { get; set; } = new();
    }
}

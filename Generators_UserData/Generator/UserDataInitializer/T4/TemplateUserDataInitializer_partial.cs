using System.Collections.Generic;

namespace Generator.UserDataInitializer.T4
{
    partial class TemplateUserDataInitializer
    {
        public string Namespace { get; set; }
        public string Name { get; set; }

        public bool HasUniTask { get; set; }
        
        public string DataCategoryEnumNamespace { get; set; }
        public string DataCategoryEnumName { get; set; }
        
        public Source Source { get; set; }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Com.Cookapps.Sampleteambattle;
using Google.Protobuf.Reflection;

public static class DataStructureExtensions
{
    private static Dictionary<DataCategory, string> cachedCategoryStrings = new ();

    public static string ToCategoryString(this DataCategory category)
    {
        if (!cachedCategoryStrings.TryGetValue(category, out string cached))
        {
            MemberInfo[] memberInfos = typeof(DataCategory).GetMember(category.ToString());
            MemberInfo memberInfo = memberInfos.FirstOrDefault(m => m.DeclaringType == typeof(DataCategory));
            object[] attribute = memberInfo.GetCustomAttributes(typeof(OriginalNameAttribute), false);
            cached = attribute.Length > 0 ? ((OriginalNameAttribute) attribute[0]).Name : category.ToString();
            cachedCategoryStrings.Add(category, cached);
        }

        return cached;
    }
}

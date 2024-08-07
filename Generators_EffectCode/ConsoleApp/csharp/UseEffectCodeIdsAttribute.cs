using System;

public class UseEffectCodeIdsAttribute : Attribute
{
    public int[] CodeIds { get; }

    public UseEffectCodeIdsAttribute(params int[] codeIds)
    {
#if UNITY_EDITOR
        CodeIds = codeIds;
#endif
    }
}

using System.Collections.Generic;

namespace Entitas.Generators.Data;

public struct ComponentWithContexts
{
    public readonly ComponentData ComponentData;
    public readonly List<ContextData> ContextDatas;
    public ComponentWithContexts(ComponentData componentData, List<ContextData> contextDatas)
    {
        ComponentData = componentData;
        ContextDatas = contextDatas;
    }
}

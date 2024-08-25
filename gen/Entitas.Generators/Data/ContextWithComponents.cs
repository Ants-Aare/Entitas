using System.Collections.Generic;

namespace Entitas.Generators.Data;

public struct ContextWithComponents
{
    public readonly ContextData ContextData;
    public readonly List<ComponentData> ComponentDatas;
    public ContextWithComponents(ContextData contextData, List<ComponentData> componentDatas)
    {
        ContextData = contextData;
        ComponentDatas = componentDatas;
    }
}

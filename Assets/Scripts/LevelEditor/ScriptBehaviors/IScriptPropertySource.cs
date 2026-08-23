using System.Collections.Generic;

public interface IScriptPropertySource
{
    List<PropertyDef> GetExposedProperties(EntityDefinition definition);
}

# CubaRest.Tests

> Tests for [CubaRest](https://github.com/beas-team/CubaRest) library. Checks if your project-specific client-side entities model matches server-side Cuba entities model as well.

### Prerequisites
* C# 7
* Add [RestSharp](https://github.com/restsharp/RestSharp) as NuGet package
* [CubaRest](https://github.com/beas-team/CubaRest)
* [Cuba](https://www.cuba-platform.com/) 6.8 or higher at server side

## Usage example

You need a valid REST API connection to run tests.
Rename RestApiConnection_Template.json file to RestApiConnection.json and put there your REST API endpoint and connection credentials.

### Model matching tests

The main idea is to check automatically that client side entities model matches server-side model.
You define which existing classes to check. So you need to create them beforehand.

CubaRest.Tests knows nothing about your model. Create a unit test project and derive your test class from CubaTypeMappingTestsEngine.
Search your assembly for Entity-derived classes and enums with CubaName attribute. Check matching of each of them.

```
[TestClass]
public class MyCubaTypeMappingTests : CubaTypeMappingTestsEngine
{
    [TestMethod]
    public void TestPredefinedEntityMappings_Succeeds()
    {
        var entityTypes = Assembly
            .GetAssembly(typeof(>>>YourEntityType<<<))
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Entity)) && t.GetCustomAttributes<CubaNameAttribute>(false).Any());

        Assert.IsTrue(entityTypes != null && entityTypes.Count() > 0,
            "Entity-derived classes with CubaName attribute are not found");

        foreach (var entityType in entityTypes)
            CheckEntityMapping(entityType);
    }

    [TestMethod]
    public void TestPredefinedEnumMappings_Succeeds()
    {
        var enums = Assembly
            .GetAssembly(typeof(>>>YourEnum<<<))
            .GetTypes()
            .Where(t => t.IsEnum && t.GetCustomAttributes<CubaNameAttribute>(false).Any());

        Assert.IsTrue(enums != null && enums.Count() > 0,
            "Enums with CubaName attribute are not found");

        foreach (var enumType in enums)
            CheckEnumMapping(enumType);
    }        
}
```



## Built With
* [RestSharp](https://github.com/restsharp/RestSharp)

## Codegeneration

Consider using [CubaRest.Codegenerator](https://github.com/beas-team/CubaRest.Codegenerator) to create numerous classes and enums of client-side model.

## License

This project is licensed under the Apache License 2.0.

## Meta

Sergey Larionov

https://github.com/Zidar
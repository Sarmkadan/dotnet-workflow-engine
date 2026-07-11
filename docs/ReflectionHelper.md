# ReflectionHelper
The `ReflectionHelper` class provides a set of static methods for working with reflection in .NET. It offers a range of functionality, including invoking methods, getting and setting property values, creating instances of types, and checking for attributes. This class is designed to simplify the process of using reflection in .NET, making it easier to work with types and objects at runtime.

## API
* `public static object? InvokeMethod`: Invokes a method on an object. Parameters: the object to invoke the method on, the method name, and an array of parameters. Returns: the result of the method invocation, or `null` if the method returns `void`. Throws: `Exception` if the method invocation fails.
* `public static object? GetPropertyValue`: Gets the value of a property on an object. Parameters: the object to get the property value from, the property name. Returns: the value of the property, or `null` if the property does not exist. Throws: `PropertyAccessException` if the property access fails.
* `public static void SetPropertyValue`: Sets the value of a property on an object. Parameters: the object to set the property value on, the property name, the new value. Throws: `PropertyAccessException` if the property access fails.
* `public static IEnumerable<PropertyInfo> GetProperties`: Gets a list of properties on a type. Parameters: the type to get properties from. Returns: a list of `PropertyInfo` objects representing the properties on the type.
* `public static IEnumerable<MethodInfo> GetMethods`: Gets a list of methods on a type. Parameters: the type to get methods from. Returns: a list of `MethodInfo` objects representing the methods on the type.
* `public static IEnumerable<Type> FindTypesImplementing`: Finds types that implement a given interface. Parameters: the interface type to find implementing types for. Returns: a list of types that implement the interface.
* `public static object? CreateInstance`: Creates an instance of a type. Parameters: the type to create an instance of. Returns: an instance of the type, or `null` if the type cannot be instantiated. Throws: `Exception` if the instantiation fails.
* `public static object CreateInstanceWithParameters`: Creates an instance of a type with the given parameters. Parameters: the type to create an instance of, an array of parameters. Returns: an instance of the type. Throws: `Exception` if the instantiation fails.
* `public static bool HasAttribute<T>`: Checks if an object has a specific attribute. Parameters: the object to check, the attribute type to check for. Returns: `true` if the object has the attribute, `false` otherwise.
* `public static T? GetAttribute<T>`: Gets an attribute of a specific type from an object. Parameters: the object to get the attribute from, the attribute type to get. Returns: the attribute value, or `null` if the attribute does not exist.
* `public static IEnumerable<T> GetAttributes<T>`: Gets all attributes of a specific type from an object. Parameters: the object to get attributes from, the attribute type to get. Returns: a list of attribute values.
* `public static bool IsNullable`: Checks if a type is nullable. Parameters: the type to check. Returns: `true` if the type is nullable, `false` otherwise.
* `public static Type GetUnderlyingType`: Gets the underlying type of a nullable type. Parameters: the type to get the underlying type for. Returns: the underlying type.
* `public static bool IsSimpleType`: Checks if a type is a simple type (e.g. integer, string, etc.). Parameters: the type to check. Returns: `true` if the type is simple, `false` otherwise.
* `public static bool IsCollectionType`: Checks if a type is a collection type (e.g. list, array, etc.). Parameters: the type to check. Returns: `true` if the type is a collection, `false` otherwise.
* `public static Type? GetCollectionElementType`: Gets the element type of a collection type. Parameters: the type to get the element type for. Returns: the element type, or `null` if the type is not a collection.

## Usage
The following examples demonstrate how to use the `ReflectionHelper` class:
```csharp
// Create an instance of a type
var instance = ReflectionHelper.CreateInstance(typeof(MyClass));

// Invoke a method on the instance
var result = ReflectionHelper.InvokeMethod(instance, "MyMethod", new object[] { "param1", 2 });

// Get the value of a property on the instance
var propertyValue = ReflectionHelper.GetPropertyValue(instance, "MyProperty");

// Set the value of a property on the instance
ReflectionHelper.SetPropertyValue(instance, "MyProperty", "new value");
```

```csharp
// Find types that implement an interface
var implementingTypes = ReflectionHelper.FindTypesImplementing(typeof(IMyInterface));

// Check if an object has a specific attribute
var hasAttribute = ReflectionHelper.HasAttribute<MyAttribute>(instance);

// Get an attribute from an object
var attribute = ReflectionHelper.GetAttribute<MyAttribute>(instance);
```

## Notes
When using the `ReflectionHelper` class, be aware of the following edge cases:
* If a method or property does not exist on an object, an exception will be thrown.
* If a type cannot be instantiated, an exception will be thrown.
* If an attribute does not exist on an object, `null` will be returned.
* The `IsNullable` and `GetUnderlyingType` methods only work with nullable types.
* The `IsSimpleType` and `IsCollectionType` methods use a heuristic approach to determine if a type is simple or a collection, and may not always return the expected result.
* The `GetCollectionElementType` method only works with collection types that have a single element type.
* The `ReflectionHelper` class is not thread-safe, and should not be used concurrently from multiple threads.

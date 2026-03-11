// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DotNetWorkflowEngine.Utilities;

/// <summary>
/// Helper class for reflection-based operations such as dynamically invoking methods,
/// accessing properties, and inspecting types. Used for activity execution where
/// activity implementations are loaded dynamically at runtime.
/// </summary>
public static class ReflectionHelper
{
    /// <summary>
    /// Dynamically invokes a method on an instance by name.
    /// Supports both public and private methods.
    /// </summary>
    public static object? InvokeMethod(object instance, string methodName, params object?[] parameters)
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));

        if (string.IsNullOrEmpty(methodName))
            throw new ArgumentException("Method name cannot be null or empty");

        var method = instance.GetType().GetMethod(
            methodName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.IgnoreCase);

        if (method == null)
            throw new MethodAccessException($"Method '{methodName}' not found on type '{instance.GetType().Name}'");

        try
        {
            return method.Invoke(instance, parameters);
        }
        catch (TargetInvocationException ex)
        {
            throw ex.InnerException ?? ex;
        }
    }

    /// <summary>
    /// Dynamically gets a property value from an instance by name.
    /// </summary>
    public static object? GetPropertyValue(object instance, string propertyName)
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));

        var property = instance.GetType().GetProperty(
            propertyName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.IgnoreCase);

        if (property == null)
            throw new PropertyAccessException($"Property '{propertyName}' not found on type '{instance.GetType().Name}'");

        return property.GetValue(instance);
    }

    /// <summary>
    /// Dynamically sets a property value on an instance by name.
    /// </summary>
    public static void SetPropertyValue(object instance, string propertyName, object? value)
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));

        var property = instance.GetType().GetProperty(
            propertyName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.IgnoreCase);

        if (property == null)
            throw new PropertyAccessException($"Property '{propertyName}' not found on type '{instance.GetType().Name}'");

        if (!property.CanWrite)
            throw new PropertyAccessException($"Property '{propertyName}' is read-only");

        try
        {
            property.SetValue(instance, value);
        }
        catch (Exception ex)
        {
            throw new PropertyAccessException($"Cannot set property '{propertyName}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets all properties of a type that match specified criteria.
    /// </summary>
    public static IEnumerable<PropertyInfo> GetProperties(
        Type type,
        bool includePrivate = false,
        bool includeStatic = false)
    {
        var bindingFlags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;

        if (includePrivate)
            bindingFlags |= System.Reflection.BindingFlags.NonPublic;

        if (includeStatic)
            bindingFlags |= System.Reflection.BindingFlags.Static;

        return type.GetProperties(bindingFlags);
    }

    /// <summary>
    /// Gets all methods of a type that match specified criteria.
    /// </summary>
    public static IEnumerable<MethodInfo> GetMethods(
        Type type,
        bool includePrivate = false,
        bool includeStatic = false)
    {
        var bindingFlags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;

        if (includePrivate)
            bindingFlags |= System.Reflection.BindingFlags.NonPublic;

        if (includeStatic)
            bindingFlags |= System.Reflection.BindingFlags.Static;

        return type.GetMethods(bindingFlags);
    }

    /// <summary>
    /// Finds all types in an assembly that implement or inherit from a given type.
    /// </summary>
    public static IEnumerable<Type> FindTypesImplementing(Assembly assembly, Type targetType)
    {
        return assembly.GetTypes()
            .Where(t => targetType.IsAssignableFrom(t) && t != targetType);
    }

    /// <summary>
    /// Creates an instance of a type using its parameterless constructor.
    /// Returns null if the type has no parameterless constructor.
    /// </summary>
    public static object? CreateInstance(Type type)
    {
        try
        {
            return Activator.CreateInstance(type);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Creates an instance of a type using a constructor with specified parameters.
    /// </summary>
    public static object CreateInstanceWithParameters(Type type, params object?[] parameters)
    {
        try
        {
            return Activator.CreateInstance(type, parameters)
                ?? throw new InvalidOperationException($"Failed to create instance of {type.Name}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Cannot create instance of {type.Name}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Checks if a type has a specific attribute.
    /// </summary>
    public static bool HasAttribute<T>(Type type) where T : Attribute
    {
        return type.GetCustomAttribute<T>() != null;
    }

    /// <summary>
    /// Gets the value of a specific attribute from a type.
    /// </summary>
    public static T? GetAttribute<T>(Type type) where T : Attribute
    {
        return type.GetCustomAttribute<T>();
    }

    /// <summary>
    /// Gets all attributes of a specific type from a member (type, method, property, etc.).
    /// </summary>
    public static IEnumerable<T> GetAttributes<T>(MemberInfo member) where T : Attribute
    {
        return member.GetCustomAttributes<T>();
    }

    /// <summary>
    /// Determines if a type is nullable (either Nullable<T> or a reference type).
    /// </summary>
    public static bool IsNullable(Type type)
    {
        if (!type.IsValueType)
            return true; // Reference types are nullable

        return Nullable.GetUnderlyingType(type) != null;
    }

    /// <summary>
    /// Gets the underlying type of a nullable type. Returns the original type if not nullable.
    /// </summary>
    public static Type GetUnderlyingType(Type type)
    {
        return Nullable.GetUnderlyingType(type) ?? type;
    }

    /// <summary>
    /// Checks if a type is a simple type (primitive, string, decimal, etc.).
    /// </summary>
    public static bool IsSimpleType(Type type)
    {
        return type.IsValueType || type == typeof(string) || type == typeof(object);
    }

    /// <summary>
    /// Checks if a type is a collection type (array, IEnumerable, List, etc.).
    /// </summary>
    public static bool IsCollectionType(Type type)
    {
        if (type.IsArray)
            return true;

        var enumerableInterface = type.GetInterface("IEnumerable");
        return enumerableInterface != null && type != typeof(string);
    }

    /// <summary>
    /// Gets the element type of a collection type (e.g., T from List<T>).
    /// </summary>
    public static Type? GetCollectionElementType(Type type)
    {
        if (type.IsArray)
            return type.GetElementType();

        if (type.IsGenericType)
        {
            var genericArgs = type.GetGenericArguments();
            if (genericArgs.Length > 0)
                return genericArgs[0];
        }

        return null;
    }
}

/// <summary>
/// Exception thrown when attempting to access a property that doesn't exist or is inaccessible.
/// </summary>
public class PropertyAccessException : Exception
{
    public PropertyAccessException(string message) : base(message) { }
    public PropertyAccessException(string message, Exception innerException) : base(message, innerException) { }
}

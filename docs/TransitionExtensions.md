# TransitionExtensions
The `TransitionExtensions` class provides a set of static methods for working with transitions in a workflow engine. These methods enable developers to inspect and manipulate transitions, including determining their conditionality, display labels, and relationships with other transitions.

## API
* `public static bool IsConditional`: Determines whether a transition is conditional. Returns `true` if the transition is conditional, `false` otherwise.
* `public static bool IsUnconditional`: Determines whether a transition is unconditional. Returns `true` if the transition is unconditional, `false` otherwise.
* `public static string GetDisplayLabel`: Retrieves the display label of a transition. Returns a string representing the display label.
* `public static Transition WithProperties`: Creates a new transition with the specified properties. Returns a new `Transition` instance.
* `public static bool PointsTo`: Determines whether a transition points to a specific target. Returns `true` if the transition points to the target, `false` otherwise.
* `public static bool ComesFrom`: Determines whether a transition comes from a specific source. Returns `true` if the transition comes from the source, `false` otherwise.

## Usage
The following examples demonstrate how to use the `TransitionExtensions` class:
```csharp
// Example 1: Determine if a transition is conditional
var transition = new Transition();
if (TransitionExtensions.IsConditional(transition))
{
    Console.WriteLine("The transition is conditional.");
}

// Example 2: Create a new transition with properties
var newTransition = TransitionExtensions.WithProperties(new { Label = "My Transition" });
Console.WriteLine(newTransition.GetDisplayLabel());  // Output: My Transition
```

## Notes
When using the `TransitionExtensions` class, note that the `IsConditional` and `IsUnconditional` methods may throw a `NullReferenceException` if the input transition is null. Additionally, the `WithProperties` method may throw an `ArgumentException` if the input properties are invalid. The `PointsTo` and `ComesFrom` methods are thread-safe, as they only depend on the state of the input transitions. However, the `GetDisplayLabel` method may not be thread-safe if the display label is dynamically generated and depends on external state. It is recommended to use these methods in a thread-safe manner to avoid unexpected behavior.

# SerializationHelperTests

Unit test class for `SerializationHelper` providing coverage of JSON serialization, deserialization, deep cloning, and object merging operations. Validates correct behavior across success and edge cases including null inputs, malformed JSON, and mutation safety.

## API

### `ToJson_SerializesObjectToJson`
Verifies that `SerializationHelper.ToJson` correctly converts a non-null object into a JSON string representation. The test asserts that the output is a valid JSON string and matches the expected structure of the input object.

### `ToJson_WithNull_ReturnsNullJson`
Ensures that passing `null` to `SerializationHelper.ToJson` returns the string `"null"` rather than throwing an exception or returning an empty string.

### `ToJsonPretty_SerializesWithFormatting`
Confirms that `SerializationHelper.ToJson` with formatting enabled produces a human-readable, indented JSON output while preserving the correctness of the serialized data.

### `FromJson_DeserializesJsonToObject`
Tests that `SerializationHelper.FromJson` successfully reconstructs an object from a valid JSON string, matching the original object's structure and values.

### `FromJson_WithNullOrEmpty_ReturnsNull`
Validates that `SerializationHelper.FromJson` returns `null` when given `null`, empty string, or whitespace-only input, without throwing exceptions.

### `FromJson_WithInvalidJson_ThrowsSerializationException`
Checks that `SerializationHelper.FromJson` throws a `SerializationException` when provided with syntactically invalid JSON input.

### `FromJsonToDict_DeserializesToDictionary`
Ensures that `SerializationHelper.FromJsonToDict` converts a JSON string representing an object into a `Dictionary<string, object>` with keys and values matching the JSON structure.

### `FromJsonToDict_WithNullOrEmpty_ReturnsNull`
Confirms that `SerializationHelper.FromJsonToDict` returns `null` when given `null`, empty, or whitespace-only input, without throwing exceptions.

### `FromJsonToDict_WithInvalidJson_ThrowsSerializationException`
Verifies that `SerializationHelper.FromJsonToDict` throws a `SerializationException` when the input JSON is malformed.

### `TryFromJson_WithValidJson_ReturnsObject`
Tests that `SerializationHelper.TryFromJson` returns a non-null object when given valid JSON input, and that the object matches the expected structure.

### `TryFromJson_WithInvalidJson_ReturnsNull`
Ensures that `SerializationHelper.TryFromJson` returns `null` when provided with invalid JSON, rather than throwing an exception.

### `TryFromJson_WithNullOrEmpty_ReturnsNull`
Validates that `SerializationHelper.TryFromJson` returns `null` for `null`, empty, or whitespace-only input, maintaining consistent null-handling behavior.

### `DeepClone_CreatesCompleteClone`
Confirms that `SerializationHelper.DeepClone` produces a deep copy of the input object, such that the clone is structurally identical but independent of the original.

### `DeepClone_WithNull_ReturnsNull`
Ensures that `SerializationHelper.DeepClone` returns `null` when given a `null` input, without throwing exceptions.

### `DeepClone_ModifyingClone_DoesNotAffectOriginal`
Validates that modifications to a cloned object do not affect the original object, confirming true deep cloning behavior.

### `Merge_CombinesTwoObjects`
Tests that `SerializationHelper.Merge` correctly combines properties from two objects, with values from the second object overriding those in the first where keys overlap.

### `Merge_WithFirstNull_ReturnsSecond`
Ensures that when the first argument to `SerializationHelper.Merge` is `null`, the method returns the second object unchanged.

### `Merge_WithSecondNull_ReturnsFirst`
Confirms that when the second argument to `SerializationHelper.Merge` is `null`, the method returns the first object unchanged.

### `Merge_WithBothNull_ReturnsNull`
Validates that `SerializationHelper.Merge` returns `null` when both input objects are `null`.

### `FromJsonElement_DeserializesJsonElement`
Verifies that `SerializationHelper.FromJsonElement` correctly deserializes a `JsonElement` into the target object type, preserving structure and values.

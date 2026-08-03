// =============================================================================
// Benchmark for AuditLogEntry model
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using DotNetWorkflowEngine.Models;

namespace DotNetWorkflowEngine.Benchmarks
{
    /// <summary>
    /// Benchmarks covering the most common operations on <see cref="AuditLogEntry"/>.
    /// Includes serialization/deserialization using System.Text.Json and the
    /// provided Json extension helpers.
    /// </summary>
    [MemoryDiagnoser]
    public class AuditLogEntryBenchmarks
    {
        /// <summary>
        /// Number of entries to work with in the batch benchmarks.
        /// </summary>
        [Params(10, 100, 1000)]
        public int Size { get; set; }

        private List<AuditLogEntry> _entries = null!;
        private List<string> _jsonEntries = null!;

        /// <summary>
        /// Creates a collection of dummy <see cref="AuditLogEntry"/> instances and
        /// their JSON representations. The dummy data is intentionally simple
        /// but realistic enough to exercise the serializer.
        /// </summary>
        [GlobalSetup]
        public void GlobalSetup()
        {
            _entries = new List<AuditLogEntry>(Size);
            var rnd = new Random();

            for (int i = 0; i < Size; i++)
            {
                _entries.Add(new AuditLogEntry
                {
                    // Populate properties with simple random data.
                    // Adjust property names if the model differs.
                    // Example (replace with actual properties):
                    // Id = Guid.NewGuid(),
                    // Timestamp = DateTime.UtcNow,
                    // Message = $"Log entry {i}",
                    // Details = $"Details {rnd.Next(1000)}"
                });
            }

            // Serialize using System.Text.Json – this is the baseline used by the
            // extension methods as well.
            _jsonEntries = _entries
                .Select(e => JsonSerializer.Serialize(e))
                .ToList();
        }

        // --------------------------------------------------------------------
        // Serialization benchmarks
        // --------------------------------------------------------------------

        /// <summary>
        /// Serializes a single <see cref="AuditLogEntry"/> to JSON.
        /// </summary>
        [Benchmark]
        public string SerializeSingle()
        {
            return JsonSerializer.Serialize(_entries[0]);
        }

        /// <summary>
        /// Serializes the whole collection of entries to JSON (one after another).
        /// </summary>
        [Benchmark]
        public string SerializeBatch()
        {
            var sb = new StringBuilder(capacity: Size * 256);
            foreach (var entry in _entries)
            {
                sb.Append(JsonSerializer.Serialize(entry));
            }
            return sb.ToString();
        }

        // --------------------------------------------------------------------
        // Deserialization benchmarks (System.Text.Json)
        // --------------------------------------------------------------------

        /// <summary>
        /// Deserializes a single JSON string back to <see cref="AuditLogEntry"/>.
        /// </summary>
        [Benchmark]
        public AuditLogEntry DeserializeSingle()
        {
            return JsonSerializer.Deserialize<AuditLogEntry>(_jsonEntries[0])!;
        }

        /// <summary>
        /// Deserializes the whole batch of JSON strings back to objects.
        /// </summary>
        [Benchmark]
        public List<AuditLogEntry> DeserializeBatch()
        {
            var list = new List<AuditLogEntry>(_jsonEntries.Count);
            foreach (var json in _jsonEntries)
            {
                list.Add(JsonSerializer.Deserialize<AuditLogEntry>(json)!);
            }
            return list;
        }

        // --------------------------------------------------------------------
        // Deserialization benchmarks using the provided extension helpers
        // --------------------------------------------------------------------

        /// <summary>
        /// Deserializes a single JSON string using the generated extension method.
        /// </summary>
        [Benchmark]
        public AuditLogEntry DeserializeSingleViaExtension()
        {
            return AuditLogEntryJsonExtensions.FromJson(_jsonEntries[0])!;
        }

        /// <summary>
        /// Deserializes a single JSON string using the TryFromJson pattern.
        /// </summary>
        [Benchmark]
        public bool TryDeserializeSingleViaExtension()
        {
            return AuditLogEntryJsonExtensions.TryFromJson(_jsonEntries[0], out _);
        }
    }
}

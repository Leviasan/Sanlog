using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Leviasan.Sanlog.EntityFrameworkCore
{
    /// <summary>
    /// Represents <see cref="EntityEntry"/> Json conveter to or from object.
    /// </summary>
    internal sealed class EntityEntryJsonConverter : JsonConverter<EntityEntry>
    {
        /// <inheritdoc/>
        /// <exception cref="NotSupportedException">The operation is not supported.</exception>
        public override EntityEntry? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotSupportedException();
        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, EntityEntry value, JsonSerializerOptions options) => writer.WriteStringValue(value.DebugView.ShortView);
    }
}
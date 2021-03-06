﻿using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Json.Schema
{
	/// <summary>
	/// Handles `$schema`.
	/// </summary>
	[SchemaKeyword(Name)]
	[SchemaPriority(long.MinValue)]
	[SchemaDraft(Draft.Draft6)]
	[SchemaDraft(Draft.Draft7)]
	[SchemaDraft(Draft.Draft201909)]
	[Vocabulary(Vocabularies.Core201909Id)]
	[JsonConverter(typeof(SchemaKeywordJsonConverter))]
	public class SchemaKeyword : IJsonSchemaKeyword
	{
		internal const string Name = "$schema";

		/// <summary>
		/// The meta-schema ID.
		/// </summary>
		public Uri Schema { get; }

		/// <summary>
		/// Creates a new <see cref="SchemaKeyword"/>.
		/// </summary>
		/// <param name="schema">The meta-schema ID.</param>
		public SchemaKeyword(Uri schema)
		{
			Schema = schema;
		}

		/// <summary>
		/// Provides validation for the keyword.
		/// </summary>
		/// <param name="context">Contextual details for the validation process.</param>
		public void Validate(ValidationContext context)
		{
			if (!context.Options.ValidateMetaSchema)
			{
				context.IsValid = true;
				return;
			}

			var metaSchema = context.Options.SchemaRegistry.Get(Schema);
			if (metaSchema == null)
			{
				context.Message = $"Could not resolve schema `{Schema.OriginalString}` for meta-schema validation";
				context.IsValid = false;
				return;
			}

			var schemaAsJson = JsonDocument.Parse(JsonSerializer.Serialize(context.LocalSchema)).RootElement;
			var newOptions = ValidationOptions.From(context.Options);
			var results = metaSchema.Validate(schemaAsJson, newOptions);

			context.IsValid = results.IsValid;
			if (!context.IsValid)
				context.Message = $"Cannot validate current schema against meta-schema `{Schema.OriginalString}`";
		}
	}

	internal class SchemaKeywordJsonConverter : JsonConverter<SchemaKeyword>
	{
		public override SchemaKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.String)
				throw new JsonException("Expected string");

			var uriString = reader.GetString();
			if (!Uri.TryCreate(uriString, UriKind.Absolute, out var uri))
				throw new JsonException("Expected absolute URI");

			return new SchemaKeyword(uri);
		}

		public override void Write(Utf8JsonWriter writer, SchemaKeyword value, JsonSerializerOptions options)
		{
			writer.WriteString(SchemaKeyword.Name, value.Schema.OriginalString);
		}
	}
}
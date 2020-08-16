﻿using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Json.Schema
{
	[SchemaKeyword(Name)]
	[JsonConverter(typeof(ElseKeywordJsonConverter))]
	public class ElseKeyword : IJsonSchemaKeyword
	{
		internal const string Name = "else";

		public JsonSchema Schema { get; }

		public ElseKeyword(JsonSchema value)
		{
			Schema = value;
		}

		public void Validate(ValidationContext context)
		{
			var annotation = context.TryGetAnnotation(IfKeyword.Name);
			if (annotation == null || (bool) annotation)
			{
				context.IsValid = true;
				return;
			}

			var subContext = ValidationContext.From(context);
			Schema.ValidateSubschema(subContext);
			context.NestedContexts.Add(subContext);

			context.ConsolidateAnnotations();
			context.IsValid = subContext.IsValid;
		}
	}

	public class ElseKeywordJsonConverter : JsonConverter<ElseKeyword>
	{
		public override ElseKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var schema = JsonSerializer.Deserialize<JsonSchema>(ref reader, options);

			return new ElseKeyword(schema);
		}
		public override void Write(Utf8JsonWriter writer, ElseKeyword value, JsonSerializerOptions options)
		{
			writer.WritePropertyName(ElseKeyword.Name);
			JsonSerializer.Serialize(writer, value.Schema, options);
		}
	}
}
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Json.Pointer;

namespace Json.Schema
{
	[SchemaKeyword(Name)]
	[JsonConverter(typeof(PatternPropertiesKeywordJsonConverter))]
	public class PatternPropertiesKeyword : IJsonSchemaKeyword
	{
		internal const string Name = "patternProperties";

		public IReadOnlyDictionary<Regex, JsonSchema> Patterns { get; }

		static PatternPropertiesKeyword()
		{
			ValidationContext.RegisterConsolidationMethod(ConsolidateAnnotations);
		}

		public PatternPropertiesKeyword(IReadOnlyDictionary<Regex, JsonSchema> values)
		{
			Patterns = values;
		}

		public ValidationResults Validate(ValidationContext context)
		{
			if (context.Instance.ValueKind != JsonValueKind.Object)
				return null;
			
			var subResults = new List<ValidationResults>();
			var overallResult = true;
			var evaluatedProperties = new List<string>();
			var instanceProperties = context.Instance.EnumerateObject().ToList();
			foreach (var entry in Patterns)
			{
				var schema = entry.Value;
				var pattern = entry.Key;
				foreach (var instanceProperty in instanceProperties.Where(p => pattern.IsMatch(p.Name)))
				{
					var subContext = ValidationContext.From(context,
						context.InstanceLocation.Combine(PointerSegment.Create($"{instanceProperty.Name}")),
						instanceProperty.Value,
						context.SchemaLocation.Combine(PointerSegment.Create($"{pattern}")));
					var results = schema.ValidateSubschema(subContext);
					overallResult &= results.IsValid;
					subResults.Add(results);
					evaluatedProperties.Add(instanceProperty.Name);
				}
			}

			context.Annotations[Name] = evaluatedProperties.Distinct().ToList();
			var result = overallResult
				? ValidationResults.Success(context)
				: ValidationResults.Fail(context);
			result.AddNestedResults(subResults);
			return result;
		}

		private static void ConsolidateAnnotations(IEnumerable<ValidationContext> sourceContexts, ValidationContext destContext)
		{
			object value;
			var allAnnotations = sourceContexts.Select(c => c.TryGetAnnotation(Name))
				.Where(a => a != null)
				.ToList();
			if (allAnnotations.OfType<bool>().Any())
				value = true;
			else
				value = allAnnotations.OfType<int>().DefaultIfEmpty(-1).Max();
			if (!Equals(value, -1))
				destContext.Annotations[Name] = value;
		}
	}

	public class PatternPropertiesKeywordJsonConverter : JsonConverter<PatternPropertiesKeyword>
	{
		public override PatternPropertiesKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.StartObject)
				throw new JsonException("Expected object");

			var schemas = JsonSerializer.Deserialize<Dictionary<string, JsonSchema>>(ref reader, options)
				.ToDictionary(kvp => new Regex(kvp.Key), kvp => kvp.Value);
			return new PatternPropertiesKeyword(schemas);
		}
		public override void Write(Utf8JsonWriter writer, PatternPropertiesKeyword value, JsonSerializerOptions options)
		{
			writer.WritePropertyName(PatternPropertiesKeyword.Name);
			writer.WriteStartArray();
			foreach (var schema in value.Patterns)
			{
				JsonSerializer.Serialize(writer, schema, options);
			}
			writer.WriteEndArray();
		}
	}
}
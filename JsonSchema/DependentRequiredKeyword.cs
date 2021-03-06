﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Json.Schema
{
	/// <summary>
	/// Handles `dependentRequired`.
	/// </summary>
	[SchemaPriority(10)]
	[SchemaKeyword(Name)]
	[SchemaDraft(Draft.Draft201909)]
	[Vocabulary(Vocabularies.Validation201909Id)]
	[JsonConverter(typeof(DependentRequiredKeywordJsonConverter))]
	public class DependentRequiredKeyword : IJsonSchemaKeyword
	{
		internal const string Name = "dependentRequired";

		/// <summary>
		/// The collection of "required"-type dependencies.
		/// </summary>
		public IReadOnlyDictionary<string, IReadOnlyList<string>> Requirements { get; }

		static DependentRequiredKeyword()
		{
			ValidationContext.RegisterConsolidationMethod(ConsolidateAnnotations);
		}
		/// <summary>
		/// Creates a new <see cref="DependentRequiredKeyword"/>.
		/// </summary>
		/// <param name="values">The collection of "required"-type dependencies.</param>
		public DependentRequiredKeyword(IReadOnlyDictionary<string, IReadOnlyList<string>> values)
		{
			Requirements = values;
		}

		/// <summary>
		/// Provides validation for the keyword.
		/// </summary>
		/// <param name="context">Contextual details for the validation process.</param>
		public void Validate(ValidationContext context)
		{
			if (context.LocalInstance.ValueKind != JsonValueKind.Object)
			{
				context.IsValid = true;
				return;
			}

			var overallResult = true;
			var missingDependencies = new Dictionary<string, List<string>>();
			foreach (var property in Requirements)
			{
				var dependencies = property.Value;
				var name = property.Key;
				if (!context.LocalInstance.TryGetProperty(name, out _)) continue;

				foreach (var dependency in dependencies)
				{
					if (context.LocalInstance.TryGetProperty(dependency, out _)) continue;

					overallResult = false;
					if (context.ApplyOptimizations) break;
					if (!missingDependencies.TryGetValue(name, out var list)) 
						list = missingDependencies[name] = new List<string>();
					list.Add(dependency);
				}
				if (!overallResult && context.ApplyOptimizations) break;
			}

			context.IsValid = overallResult;
			if (!context.IsValid)
				context.Message = $"Some required property dependencies are missing: {JsonSerializer.Serialize(missingDependencies)}";
		}

		private static void ConsolidateAnnotations(IEnumerable<ValidationContext> sourceContexts, ValidationContext destContext)
		{
			var allDependentRequired = sourceContexts.Select(c => c.TryGetAnnotation(Name))
				.Where(a => a != null)
				.Cast<List<string>>()
				.SelectMany(a => a)
				.Distinct()
				.ToList();
			if (destContext.TryGetAnnotation(Name) is List<string> annotation)
				annotation.AddRange(allDependentRequired);
			else if (allDependentRequired.Any())
				destContext.SetAnnotation(Name, allDependentRequired);
		}
	}

	internal class DependentRequiredKeywordJsonConverter : JsonConverter<DependentRequiredKeyword>
	{
		public override DependentRequiredKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.StartObject)
				throw new JsonException("Expected object");

			var requirements = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(ref reader, options);
			return new DependentRequiredKeyword(requirements.ToDictionary(x => x.Key, x => (IReadOnlyList<string>) x.Value));
		}
		public override void Write(Utf8JsonWriter writer, DependentRequiredKeyword value, JsonSerializerOptions options)
		{
			writer.WritePropertyName(DependentRequiredKeyword.Name);
			writer.WriteStartObject();
			foreach (var kvp in value.Requirements)
			{
				writer.WritePropertyName(kvp.Key);
				JsonSerializer.Serialize(writer, kvp.Value, options);
			}
			writer.WriteEndObject();
		}
	}
}
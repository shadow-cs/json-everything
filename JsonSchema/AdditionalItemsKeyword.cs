﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Json.Pointer;

namespace Json.Schema
{
	/// <summary>
	/// Handles `additionalItems`.
	/// </summary>
	[Applicator]
	[SchemaPriority(10)]
	[SchemaKeyword(Name)]
	[SchemaDraft(Draft.Draft6)]
	[SchemaDraft(Draft.Draft7)]
	[SchemaDraft(Draft.Draft201909)]
	[Vocabulary(Vocabularies.Applicator201909Id)]
	[JsonConverter(typeof(AdditionalItemsKeywordJsonConverter))]
	public class AdditionalItemsKeyword : IJsonSchemaKeyword, IRefResolvable
	{
		internal const string Name = "additionalItems";

		/// <summary>
		/// The schema by which to validation additional items.
		/// </summary>
		public JsonSchema Schema { get; }

		static AdditionalItemsKeyword()
		{
			ValidationContext.RegisterConsolidationMethod(ConsolidateAnnotations);
		}
		/// <summary>
		/// Creates a new <see cref="AdditionalItemsKeyword"/>.
		/// </summary>
		/// <param name="value">The keyword's schema.</param>
		public AdditionalItemsKeyword(JsonSchema value)
		{
			Schema = value;
		}

		/// <summary>
		/// Provides validation for the keyword.
		/// </summary>
		/// <param name="context">Contextual details for the validation process.</param>
		public void Validate(ValidationContext context)
		{
			if (context.LocalInstance.ValueKind != JsonValueKind.Array)
			{
				context.IsValid = true;
				return;
			}

			var overallResult = true;
			var annotation = context.TryGetAnnotation(ItemsKeyword.Name);
			if (annotation == null)
			{
				context.IsValid = true;
				return;
			}
			if (annotation is bool)
			{
				context.IsValid = true;
				return;
			}
			var startIndex = (int) annotation;

			for (int i = startIndex; i < context.LocalInstance.GetArrayLength(); i++)
			{
				var item = context.LocalInstance[i];
				var subContext = ValidationContext.From(context,
					context.InstanceLocation.Combine(PointerSegment.Create($"{i}")),
					item);
				Schema.ValidateSubschema(subContext);
				overallResult &= subContext.IsValid;
				if (!overallResult && context.ApplyOptimizations) break;
			}

			if (overallResult)
				context.SetAnnotation(Name, true);
			context.IsValid = overallResult;
		}

		private static void ConsolidateAnnotations(IEnumerable<ValidationContext> sourceContexts, ValidationContext destContext)
		{
			if (sourceContexts.Select(c => c.TryGetAnnotation(Name)).OfType<bool>().Any())
				destContext.SetAnnotation(Name, true);
		}

		IRefResolvable IRefResolvable.ResolvePointerSegment(string value)
		{
			return value == null ? Schema : null;
		}

		void IRefResolvable.RegisterSubschemas(SchemaRegistry registry, Uri currentUri)
		{
			Schema.RegisterSubschemas(registry, currentUri);
		}
	}

	internal class AdditionalItemsKeywordJsonConverter : JsonConverter<AdditionalItemsKeyword>
	{
		public override AdditionalItemsKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var schema = JsonSerializer.Deserialize<JsonSchema>(ref reader, options);

			return new AdditionalItemsKeyword(schema);
		}
		public override void Write(Utf8JsonWriter writer, AdditionalItemsKeyword value, JsonSerializerOptions options)
		{
			writer.WritePropertyName(AdditionalItemsKeyword.Name);
			JsonSerializer.Serialize(writer, value.Schema, options);
		}
	}
}
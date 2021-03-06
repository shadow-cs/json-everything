﻿using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Json.Schema
{
	/// <summary>
	/// Handles `maximum`.
	/// </summary>
	[SchemaKeyword(Name)]
	[SchemaDraft(Draft.Draft6)]
	[SchemaDraft(Draft.Draft7)]
	[SchemaDraft(Draft.Draft201909)]
	[Vocabulary(Vocabularies.Validation201909Id)]
	[JsonConverter(typeof(MaximumKeywordJsonConverter))]
	public class MaximumKeyword : IJsonSchemaKeyword
	{
		internal const string Name = "maximum";

		/// <summary>
		/// The maximum expected value.
		/// </summary>
		public decimal Value { get; }

		/// <summary>
		/// Creates a new <see cref="MaximumKeyword"/>.
		/// </summary>
		/// <param name="value">The maximum expected value.</param>
		public MaximumKeyword(decimal value)
		{
			Value = value;
		}

		/// <summary>
		/// Provides validation for the keyword.
		/// </summary>
		/// <param name="context">Contextual details for the validation process.</param>
		public void Validate(ValidationContext context)
		{
			if (context.LocalInstance.ValueKind != JsonValueKind.Number)
			{
				context.IsValid = true;
				return;
			}

			var number = context.LocalInstance.GetDecimal();
			context.IsValid = Value >= number;
			if (!context.IsValid)
				context.Message = $"{number} is greater than or equal to {Value}";
		}
	}

	internal class MaximumKeywordJsonConverter : JsonConverter<MaximumKeyword>
	{
		public override MaximumKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.Number)
				throw new JsonException("Expected number");

			var number = reader.GetDecimal();

			return new MaximumKeyword(number);
		}
		public override void Write(Utf8JsonWriter writer, MaximumKeyword value, JsonSerializerOptions options)
		{
			writer.WriteNumber(MaximumKeyword.Name, value.Value);
		}
	}
}
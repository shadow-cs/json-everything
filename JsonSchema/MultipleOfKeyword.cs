﻿using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Json.Schema
{
	/// <summary>
	/// Handles `multipleOf`.
	/// </summary>
	[SchemaKeyword(Name)]
	[SchemaDraft(Draft.Draft6)]
	[SchemaDraft(Draft.Draft7)]
	[SchemaDraft(Draft.Draft201909)]
	[Vocabulary(Vocabularies.Validation201909Id)]
	[JsonConverter(typeof(MultipleOfKeywordJsonConverter))]
	public class MultipleOfKeyword : IJsonSchemaKeyword
	{
		internal const string Name = "multipleOf";

		/// <summary>
		/// The expected divisor of a value.
		/// </summary>
		public decimal Value { get; }

		/// <summary>
		/// Creates a new <see cref="MultipleOfKeyword"/>.
		/// </summary>
		/// <param name="value">The expected divisor of a value.</param>
		public MultipleOfKeyword(decimal value)
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
			context.IsValid = number % Value == 0;
			if (!context.IsValid)
				context.Message = $"{number} a multiple of {Value}";
		}
	}

	internal class MultipleOfKeywordJsonConverter : JsonConverter<MultipleOfKeyword>
	{
		public override MultipleOfKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.Number)
				throw new JsonException("Expected number");

			var number = reader.GetDecimal();

			return new MultipleOfKeyword(number);
		}
		public override void Write(Utf8JsonWriter writer, MultipleOfKeyword value, JsonSerializerOptions options)
		{
			writer.WriteNumber(MultipleOfKeyword.Name, value.Value);
		}
	}
}
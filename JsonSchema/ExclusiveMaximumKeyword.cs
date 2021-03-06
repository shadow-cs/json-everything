﻿using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Json.Schema
{
	/// <summary>
	/// Handles `exclusiveMaximum`.
	/// </summary>
	[SchemaKeyword(Name)]
	[SchemaDraft(Draft.Draft6)]
	[SchemaDraft(Draft.Draft7)]
	[SchemaDraft(Draft.Draft201909)]
	[Vocabulary(Vocabularies.Validation201909Id)]
	[JsonConverter(typeof(ExclusiveMaximumKeywordJsonConverter))]
	public class ExclusiveMaximumKeyword : IJsonSchemaKeyword
	{
		internal const string Name = "exclusiveMaximum";
	
		/// <summary>
		/// The maximum value.
		/// </summary>
		public decimal Value { get; }

		/// <summary>
		/// Creates a new <see cref="ExclusiveMaximumKeyword"/>.
		/// </summary>
		/// <param name="value">The maximum value.</param>
		public ExclusiveMaximumKeyword(decimal value)
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
			context.IsValid = Value > number;
			if (!context.IsValid)
				context.Message = $"{number} is not greater than {Value}";
		}
	}

	internal class ExclusiveMaximumKeywordJsonConverter : JsonConverter<ExclusiveMaximumKeyword>
	{
		public override ExclusiveMaximumKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.Number)
				throw new JsonException("Expected number");

			var number = reader.GetDecimal();

			return new ExclusiveMaximumKeyword(number);
		}
		public override void Write(Utf8JsonWriter writer, ExclusiveMaximumKeyword value, JsonSerializerOptions options)
		{
			writer.WriteNumber(ExclusiveMaximumKeyword.Name, value.Value);
		}
	}
}
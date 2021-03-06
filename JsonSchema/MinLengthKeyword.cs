﻿using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Json.Schema
{
	/// <summary>
	/// Handles `minLength`.
	/// </summary>
	[SchemaKeyword(Name)]
	[SchemaDraft(Draft.Draft6)]
	[SchemaDraft(Draft.Draft7)]
	[SchemaDraft(Draft.Draft201909)]
	[Vocabulary(Vocabularies.Validation201909Id)]
	[JsonConverter(typeof(MinLengthKeywordJsonConverter))]
	public class MinLengthKeyword : IJsonSchemaKeyword
	{
		internal const string Name = "minLength";

		/// <summary>
		/// The minimum expected string length.
		/// </summary>
		public uint Value { get; }

		/// <summary>
		/// Creates a new <see cref="MinLengthKeyword"/>.
		/// </summary>
		/// <param name="value">The minimum expected string length.</param>
		public MinLengthKeyword(uint value)
		{
			Value = value;
		}

		/// <summary>
		/// Provides validation for the keyword.
		/// </summary>
		/// <param name="context">Contextual details for the validation process.</param>
		public void Validate(ValidationContext context)
		{
			if (context.LocalInstance.ValueKind != JsonValueKind.String)
			{
				context.IsValid = true;
				return;
			}

			var length = new StringInfo(context.LocalInstance.GetString()).LengthInTextElements;
			context.IsValid = Value <= length;
			if (!context.IsValid)
				context.Message = $"Value is not longer than or equal to {Value} characters";
		}
	}

	internal class MinLengthKeywordJsonConverter : JsonConverter<MinLengthKeyword>
	{
		public override MinLengthKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.Number)
				throw new JsonException("Expected number");

			var number = reader.GetUInt32();

			return new MinLengthKeyword(number);
		}
		public override void Write(Utf8JsonWriter writer, MinLengthKeyword value, JsonSerializerOptions options)
		{
			writer.WriteNumber(MinLengthKeyword.Name, value.Value);
		}
	}
}
﻿using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Json.Schema
{
	/// <summary>
	/// Handles `pattern`.
	/// </summary>
	[SchemaKeyword(Name)]
	[SchemaDraft(Draft.Draft6)]
	[SchemaDraft(Draft.Draft7)]
	[SchemaDraft(Draft.Draft201909)]
	[Vocabulary(Vocabularies.Validation201909Id)]
	[JsonConverter(typeof(PatternKeywordJsonConverter))]
	public class PatternKeyword : IJsonSchemaKeyword
	{
		internal const string Name = "pattern";

		/// <summary>
		/// The regular expression.
		/// </summary>
		public Regex Value { get; }

		/// <summary>
		/// Creates a new <see cref="PatternKeyword"/>.
		/// </summary>
		/// <param name="value">The regular expression.</param>
		public PatternKeyword(Regex value)
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

			var str = context.LocalInstance.GetString();
			context.IsValid = Value.IsMatch(str);
			if (!context.IsValid)
				context.Message = "The string value was not a match for the indicated regular expression";
		}
	}

	internal class PatternKeywordJsonConverter : JsonConverter<PatternKeyword>
	{
		public override PatternKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.String)
				throw new JsonException("Expected string");

			var str = reader.GetString();
			var regex = new Regex(str, RegexOptions.ECMAScript | RegexOptions.Compiled);

			return new PatternKeyword(regex);
		}
		public override void Write(Utf8JsonWriter writer, PatternKeyword value, JsonSerializerOptions options)
		{
			writer.WriteString(PatternKeyword.Name, value.Value.ToString());
		}
	}
}
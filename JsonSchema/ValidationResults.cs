﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Json.Pointer;

namespace Json.Schema
{
	/// <summary>
	/// The results object for validations.
	/// </summary>
	[JsonConverter(typeof(ValidationResultsJsonConverter))]
	public class ValidationResults
	{
		private List<ValidationResults> _nestedResults;
		private Uri _currentUri;
		private Uri _absoluteUri;
		private bool _required;

		/// <summary>
		/// Indicates whether the validation passed or failed.
		/// </summary>
		public bool IsValid { get; internal set; }
		/// <summary>
		/// The collection of annotations from this node.
		/// </summary>
		public IReadOnlyList<Annotation> Annotations { get; internal set; }
		/// <summary>
		/// The error message, if any.
		/// </summary>
		public string Message { get; internal set; }
		/// <summary>
		/// The schema location that generated this node.
		/// </summary>
		public JsonPointer SchemaLocation { get; internal set; }
		/// <summary>
		/// The instance location that was processed.
		/// </summary>
		public JsonPointer InstanceLocation { get; internal set; }

		/// <summary>
		/// The absolute schema location.  Only available if the schema had an absolute URI ID.
		/// </summary>
		public Uri AbsoluteSchemaLocation => _absoluteUri ??= _BuildAbsoluteUri();
		/// <summary>
		/// The collection of nested results.
		/// </summary>
		public IReadOnlyList<ValidationResults> NestedResults => _nestedResults;

		private bool Keep => Message != null || Annotations.Any() || NestedResults.Any(r => r.Keep) || _required;

		internal ValidationResults(ValidationContext context)
		{
			// TODO: apply format stuff here, not in JsonSchema
			IsValid = context.IsValid;
			Annotations = context.IsValid
				? context.Annotations.ToList()
				: new List<Annotation>();
			Message = context.Message;
			SchemaLocation = context.SchemaLocation;
			_currentUri = context.CurrentUri;
			InstanceLocation = context.InstanceLocation;
			_nestedResults = context.HasNestedContexts
				? context.NestedContexts.Select(c => new ValidationResults(c)).ToList()
				: new List<ValidationResults>();
			_required = context.RequiredInResult;
		}

		/// <summary>
		/// Transforms the results to the `details` format.
		/// </summary>
		public void ToDetailed()
		{
			if (!Annotations.Any() && Message == null && NestedResults.Count == 0) return;
			if (!_required && NestedResults.Count == 1)
			{
				NestedResults[0].ToDetailed();
				CopyFrom(NestedResults[0]);
				return;
			}

			var condensed = new List<ValidationResults>();
			foreach (var result in NestedResults)
			{
				result.ToDetailed();
				if (_required || result.Keep)
					condensed.Add(result);
			}

			_nestedResults.Clear();

			if (!_required && condensed.Count == 1)
				CopyFrom(condensed[0]);
			else
				_nestedResults.AddRange(condensed);
		}

		/// <summary>
		/// Transforms the results to the `basic` format.
		/// </summary>
		public void ToBasic()
		{
			var children = _GetAllChildren().ToList();
			if (!children.Any()) return;

			children.Remove(this);
			_nestedResults.Clear();
			_nestedResults.AddRange(children);
		}

		/// <summary>
		/// Transforms the results to the `flag` format.
		/// </summary>
		public void ToFlag()
		{
			_nestedResults.Clear();
		}

		private void CopyFrom(ValidationResults other)
		{
			IsValid = other.IsValid;
			Annotations = other.Annotations;
			Message = other.Message;
			SchemaLocation = other.SchemaLocation;
			_currentUri = other._currentUri;
			InstanceLocation = other.InstanceLocation;
			_nestedResults = other._nestedResults;
			_absoluteUri = other._absoluteUri;
			_required = other._required;
		}

		private Uri _BuildAbsoluteUri()
		{
			if (_currentUri == null || !_currentUri.IsAbsoluteUri) return null;
			if (SchemaLocation.Segments.All(s => s.Value != RefKeyword.Name &&
			                                     s.Value != RecursiveRefKeyword.Name))
				return null;

			return new Uri(_currentUri, SchemaLocation.ToString());
		}

		private IEnumerable<ValidationResults> _GetAllChildren()
		{
			var all = new List<ValidationResults>();
			if (Annotations.Any() || Message != null) all.Add(this);
			all.AddRange(NestedResults.SelectMany(r => r._GetAllChildren()));

			_nestedResults.Clear();

			return all;
		}
	}

	internal class ValidationResultsJsonConverter : JsonConverter<ValidationResults>
	{
		public override ValidationResults Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			throw new NotImplementedException();
		}

		public override void Write(Utf8JsonWriter writer, ValidationResults value, JsonSerializerOptions options)
		{
			writer.WriteStartObject();

			writer.WriteBoolean("valid", value.IsValid);

			writer.WritePropertyName("keywordLocation");
			JsonSerializer.Serialize(writer, value.SchemaLocation);

			if (value.AbsoluteSchemaLocation != null)
			{
				writer.WritePropertyName("absoluteKeywordLocation");
				JsonSerializer.Serialize(writer, value.AbsoluteSchemaLocation);
			}

			writer.WritePropertyName("instanceLocation");
			JsonSerializer.Serialize(writer, value.InstanceLocation);

			if (!value.IsValid)
			{
				if (value.Message != null)
					writer.WriteString("error", value.Message);

				if (value.NestedResults.Any())
				{
					writer.WritePropertyName("errors");
					JsonSerializer.Serialize(writer, value.NestedResults);
				}
			}
			else if (value.Annotations.Any() || value.NestedResults.Any())
			{
				writer.WritePropertyName("annotations");
				writer.WriteStartArray();
				foreach (var annotation in value.Annotations.Where(a => Equals(a.Source, value.SchemaLocation)))
				{
					writer.WriteStartObject();

					writer.WriteBoolean("valid", value.IsValid);

					writer.WritePropertyName("keywordLocation");
					JsonSerializer.Serialize(writer, value.SchemaLocation);

					if (value.AbsoluteSchemaLocation != null)
					{
						writer.WritePropertyName("absoluteKeywordLocation");
						JsonSerializer.Serialize(writer, value.AbsoluteSchemaLocation);
					}

					writer.WritePropertyName("instanceLocation");
					JsonSerializer.Serialize(writer, value.InstanceLocation);

					writer.WritePropertyName("annotation");
					JsonSerializer.Serialize(writer, annotation.Value);
				
					writer.WriteEndObject();
				}

				foreach (var result in value.NestedResults)
				{
					JsonSerializer.Serialize(writer, result);
				}
				writer.WriteEndArray();
			}

			writer.WriteEndObject();
		}
	}
}
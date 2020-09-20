﻿using System;
using System.Text.Json;
using Json.More;

namespace Json.Path.QueryExpressions
{
	internal class LessThanOperator : IQueryExpressionOperator
	{
		public QueryExpressionType GetOutputType(QueryExpressionNode left, QueryExpressionNode right)
		{
			if (left.OutputType != right.OutputType) return QueryExpressionType.Invalid;
			return left.OutputType switch
			{
				QueryExpressionType.Number => QueryExpressionType.Boolean,
				QueryExpressionType.String => QueryExpressionType.Boolean,
				_ => QueryExpressionType.Invalid
			};
		}

		public JsonElement Evaluate(QueryExpressionNode left, QueryExpressionNode right, JsonElement element)
		{
			return left.OutputType switch
			{
				QueryExpressionType.Number => (left.Evaluate(element).GetDecimal() < right.Evaluate(element).GetDecimal()).AsJsonElement(),
				QueryExpressionType.String => (string.Compare(left.Evaluate(element).GetString(), right.Evaluate(element).GetString(), StringComparison.Ordinal) < 0).AsJsonElement(),
				_ => default
			};
		}

		public string ToString(QueryExpressionNode left, QueryExpressionNode right)
		{
			return $"{left}<{right}";
		}
	}
}
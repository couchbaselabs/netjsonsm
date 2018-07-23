using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using netjsonsm.Expressions;
using Newtonsoft.Json.Linq;

namespace netjsonsm.Parsing
{
    public class SimpleParser : IParser
    {
        public IExpression ParseJsonExpression(byte[] bytes)
        {
            var json = JArray.Parse(Encoding.UTF8.GetString(bytes));
            return ParseJsonSubExpression(json);
        }

        private IExpression ParseJsonSubExpression(JToken json)
        {
            var expressionType = json.First.Value<string>();
            switch (expressionType)
            {
                case "equals":
                    return ParseJsonEquals(json);
                case "field":
                    return ParseJsonField(json);
                case "value":
                    return ParseJsonValue(json);
                case "not":
                    return ParseJsonNot(json);
                case "or":
                    return ParseJsonOr(json);
                case "and":
                    return ParseJsonAnd(json);
                case "anyin":
                    return ParseJsonAnyIn(json);
                case "everyin":
                    return ParseJsonEveryIn(json);
                case "anyeveryin":
                    return ParseJsonAnyEveryIn(json);
                case "lessthan":
                    return ParseJsonLessThan(json);
                case "greaterequal":
                    return ParseJsonGreaterEqual(json);
                default:
                    throw new ArgumentException(nameof(expressionType));
            }
        }

        private IExpression ParseJsonGreaterEqual(JToken json)
        {
            var lhs = ParseJsonSubExpression(json[1]);
            var rhs = ParseJsonSubExpression(json[2]);

            return new GreaterEqualExpression(lhs, rhs);
        }

        private IExpression ParseJsonLessThan(JToken json)
        {
            var lhs = ParseJsonSubExpression(json[1]);
            var rhs = ParseJsonSubExpression(json[2]);

            return new LessThanExpression(lhs, rhs);
        }

        private IExpression ParseJsonAnd(JToken json)
        {
            var expressions = new List<IExpression>();
            for (int index = 0; index < json.Count(); index++)
            {
                expressions.Add(ParseJsonSubExpression(json[index]));
            }

            return new AndExpression(expressions);
        }

        private IExpression ParseJsonOr(JToken json)
        {
            var expressions = new List<IExpression>();
            for (int index = 0; index < json.Count(); index++)
            {
                expressions.Add(ParseJsonSubExpression(json[index]));
            }

            return new OrExpression(expressions);
        }

        private IExpression ParseJsonAnyEveryIn(JToken json)
        {
            var variableIndex = json[1].Value<int>();
            var inExpression = ParseJsonSubExpression(json[2]);
            var subExpression = ParseJsonSubExpression(json[3]);

            return new AnyEveryInExpression(variableIndex, inExpression, subExpression);
        }

        private IExpression ParseJsonEveryIn(JToken json)
        {
            var variableIndex = json[1].Value<int>();
            var inExpression = ParseJsonSubExpression(json[2]);
            var subExpression = ParseJsonSubExpression(json[3]);

            return new EveryInExpression(variableIndex, inExpression, subExpression);
        }

        private IExpression ParseJsonAnyIn(JToken json)
        {
            var variableIndex = json[1].Value<int>();
            var inExpression = ParseJsonSubExpression(json[2]);
            var subExpression = ParseJsonSubExpression(json[3]);

            return new AnyInExpression(variableIndex, inExpression, subExpression);
        }

        private IExpression ParseJsonNot(JToken json)
        {
            var subExpression = ParseJsonSubExpression(json[1]);
            return new NotExpression(subExpression);
        }

        private IExpression ParseJsonValue(JToken json)
        {
            var value = json[1].Value<dynamic>();
            return new ValueExpression(value);
        }

        private IExpression ParseJsonField(JToken json)
        {
            var root = 0;

            var pos = 1;
            if (json[pos].Type == JTokenType.Integer)
            {
                root = json[pos].Value<int>();
                pos++;
            }

            var path = "";
            for (; pos < json.Count(); pos++)
            {
                path += json[pos].Value<string>();
            }

            return new FieldExpression(root, path);
        }

        private IExpression ParseJsonEquals(JToken json)
        {
            var lhs = ParseJsonSubExpression(json[1]);
            var rhs = ParseJsonSubExpression(json[2]);

            return new EqualsExpression(lhs, rhs);
        }
    }
}

// Copyright 2018 Couchbase, Inc. All rights reserved.

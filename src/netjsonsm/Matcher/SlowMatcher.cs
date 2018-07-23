using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using netjsonsm.Expressions;
using Newtonsoft.Json.Linq;

namespace netjsonsm.Matcher
{
    public class SlowMatcher : IMatcher
    {
        public IList<IExpression> Expressions { get; }
        public IList<bool> ExpressionMatches { get; }
        public IDictionary<int, object> Variables { get; }

        public SlowMatcher(IEnumerable<IExpression> expressions)
        {
            Expressions = new List<IExpression>(expressions.ToList());
            ExpressionMatches = new List<bool>(Enumerable.Range(0, Expressions.Count).Select(i => false));
            Variables = new Dictionary<int, object>();
        }

        public bool Match(byte[] data)
        {
            try
            {
                Variables[0] = JObject.Parse(Encoding.UTF8.GetString(data));
            }
            catch
            {
                return false;
            }

            var matched = false;
            for (var index = 0; index < Expressions.Count; index++)
            {
                var expression = Expressions[index];
                try
                {
                    var result = MatchRootExpression(expression);
                    ExpressionMatches[index] = result;

                    if (index == 0 && result)
                    {
                        matched = true;
                    }
                    else if(!result)
                    {
                        matched = false;
                    }
                }
                catch
                {
                    return false;
                }
            }

            Variables.Clear();
            return matched;
        }

        public bool ExpressionMatched(int index)
        {
            return ExpressionMatches[index];
        }

        public void Reset()
        {
            Variables.Clear();
            for (var i = 0; i < ExpressionMatches.Count; i++)
            {
                ExpressionMatches[i] = false;
            }
        }

        private bool MatchRootExpression(IExpression expression)
        {
            switch (expression.Type)
            {
                case ExpressionType.True:
                    return true;
                case ExpressionType.False:
                    return false;
            }

            return MatchOne(expression);
        }

        private bool MatchOne(IExpression expression)
        {
            try
            {
                switch (expression.Type)
                {
                    case ExpressionType.LessThan:
                        return MatchLessThanExpression(expression as LessThanExpression);
                    case ExpressionType.GreaterEqual:
                        return MatchGreaterEqualExpression(expression as GreaterEqualExpression);
                    case ExpressionType.Equals:
                        return MatchEqualsExpression(expression as EqualsExpression);
                    case ExpressionType.And:
                        return MatchAndExpression(expression as AndExpression);
                    case ExpressionType.Or:
                        return MatchOrExpression(expression as OrExpression);
                    case ExpressionType.Not:
                        return MatchNotExpression(expression as NotExpression);
                    case ExpressionType.AnyIn:
                        return MatchAnyInExpression(expression as AnyInExpression);
                    case ExpressionType.EveryIn:
                        return MatchEveryInExpression(expression as EveryInExpression);
                    default:
                        throw new ArgumentException(nameof(expression.Type));
                }
            }
            catch (Exception e)
            {
                return false;
            }
        }

        private dynamic ResolveParam(IExpression expression)
        {
            JToken token = null;
            if (expression is FieldExpression fieldExpression)
            {
                var root = (JToken) Variables[fieldExpression.Root];
                if (!string.IsNullOrEmpty(fieldExpression.Path))
                {
                    token = root.SelectToken(fieldExpression.Path);
                }
                else
                {
                    token = root;
                }
            }
            else if (expression is ValueExpression valueExpression)
            {
                token = (JToken) valueExpression.Value;
            }

            if (token != null)
            {
                switch (token.Type)
                {
                    case JTokenType.String:
                        return token.Value<string>();
                    case JTokenType.Integer:
                        return token.Value<int>();
                    case JTokenType.Boolean:
                        return token.Value<bool>();
                    case JTokenType.Float:
                        return token.Value<double>();
                    case JTokenType.Array:
                        return token.Values<JToken>();
                }
            }

            throw new ArgumentException(nameof(expression.Type));
        }

        private int CompareExpressions(IExpression lhs, IExpression rhs)
        {
            var lhsValue = ResolveParam(lhs);
            var rhsValue = ResolveParam(rhs);

            if (lhsValue is string lhsStr)
            {
                if (rhsValue is string rhsStr)
                {
                    return string.CompareOrdinal(lhsStr, rhsStr);
                }
                throw new Exception("invalid type comparison");
            }

            if (lhsValue is int lhsInt)
            {
                if (rhsValue is int rhsInt)
                {
                    if (lhsInt < rhsInt)
                    {
                        return -1;
                    }

                    if (lhsInt > rhsInt)
                    {
                        return 1;
                    }

                    return 0;
                }
                throw new Exception("invalid type comparison");
            }

            if (lhsValue is double lhsDouble)
            {
                if (rhsValue is double rhsDouble)
                {
                    if (lhsDouble < rhsDouble)
                    {
                        return -1;
                    }
                    if (lhsDouble > rhsDouble)
                    {
                        return 1;
                    }
                    return 0;
                }
                throw new Exception("invalid type comparison");
            }

            if (lhsValue is bool lhsBool)
            {
                if (rhsValue is bool rhsBool)
                {
                    if (lhsBool && !rhsBool)
                    {
                        return 1;
                    }

                    if (!lhsBool && rhsBool)
                    {
                        return -1;
                    }

                    return 0;
                }
                throw new Exception("invalid type comparison");
            }

            if (lhsValue is null)
            {
                if (rhsValue is null)
                {
                    return 0;
                }

                return -1;
            }

            throw new Exception("invalid lhs type");
        }

        private bool MatchGreaterEqualExpression(GreaterEqualExpression expression)
        {
            return CompareExpressions(expression.LeftHandSide, expression.RightHandSide) >= 0;
        }

        private bool MatchLessThanExpression(LessThanExpression expression)
        {
            return CompareExpressions(expression.LeftHandSide, expression.RightHandSide) < 0;
        }

        private bool MatchEqualsExpression(EqualsExpression expression)
        {
            return CompareExpressions(expression.LeftHandSide, expression.RightHandSide) == 0;
        }

        private bool MatchAndExpression(AndExpression expression)
        {
            if (!expression.Expressions.Any())
            {
                return false;
            }

            return expression.Expressions.All(MatchOne);
        }

        private bool MatchOrExpression(OrExpression expression)
        {
            return expression.Expressions.Any(MatchOne);
        }

        private bool MatchNotExpression(NotExpression expression)
        {
            return !MatchOne(expression.Expression);
        }

        private bool MatchAnyInExpression(AnyInExpression expression)
        {
            var value = ResolveParam(expression.InExpression);

            foreach (var val in value)
            {
                Variables[expression.VariableIndex] = val;
                var result = MatchOne(expression.SubExpression);
                Variables.Remove(expression.VariableIndex);

                if (result)
                {
                    return true;
                }
            }

            return false;
        }

        private bool MatchEveryInExpression(EveryInExpression expression)
        {
            var value = ResolveParam(expression.InExpression);

            foreach (var val in value)
            {
                Variables[expression.VariableIndex] = val;
                var result = MatchOne(expression.SubExpression);
                Variables.Remove(expression.VariableIndex);

                if (!result)
                {
                    return true;
                }
            }

            return true;
        }
    }
}

// Copyright 2018 Couchbase, Inc. All rights reserved.

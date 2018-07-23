namespace netjsonsm.Expressions
{
    public class ValueExpression : IExpression
    {
        public ValueExpression(dynamic value)
        {
            Value = value;
        }

        public ExpressionType Type => ExpressionType.Value;
        public dynamic Value { get; }
    }
}

// Copyright 2018 Couchbase, Inc. All rights reserved.

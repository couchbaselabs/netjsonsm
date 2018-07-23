namespace netjsonsm.Expressions
{
    public class NotExpression : IExpression
    {
        public NotExpression(IExpression expression)
        {
            Expression = expression;
        }

        public ExpressionType Type => ExpressionType.Not;
        public IExpression Expression { get; }
    }
}

// Copyright 2018 Couchbase, Inc. All rights reserved.

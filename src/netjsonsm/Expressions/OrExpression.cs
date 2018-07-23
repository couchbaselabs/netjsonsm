using System.Collections.Generic;

namespace netjsonsm.Expressions
{
    public class OrExpression : IExpression
    {
        public OrExpression(params IExpression[] expressions)
        {
            Expressions = expressions;
        }

        public OrExpression(IEnumerable<IExpression> expressions)
        {
            Expressions = expressions;
        }

        public ExpressionType Type => ExpressionType.Or;
        public IEnumerable<IExpression> Expressions { get; }
    }
}

// Copyright 2018 Couchbase, Inc. All rights reserved.

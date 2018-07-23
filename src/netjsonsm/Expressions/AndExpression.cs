using System.Collections.Generic;

namespace netjsonsm.Expressions
{
    public class AndExpression : IExpression
    {
        public AndExpression(params IExpression[] expressions)
        {
            Expressions = expressions;
        }

        public AndExpression(IEnumerable<IExpression> expressions)
        {
            Expressions = expressions;
        }

        public ExpressionType Type => ExpressionType.And;
        public IEnumerable<IExpression> Expressions { get; }
    }
}

// Copyright 2018 Couchbase, Inc. All rights reserved.

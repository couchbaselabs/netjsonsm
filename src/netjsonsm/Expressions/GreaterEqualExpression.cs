namespace netjsonsm.Expressions
{
    public class GreaterEqualExpression : IExpression
    {
        public GreaterEqualExpression(IExpression leftHandSide, IExpression rightHandSide)
        {
            LeftHandSide = leftHandSide;
            RightHandSide = rightHandSide;
        }

        public ExpressionType Type => ExpressionType.GreaterEqual;
        public IExpression LeftHandSide { get; }
        public IExpression RightHandSide { get; }
    }
}

// Copyright 2018 Couchbase, Inc. All rights reserved.

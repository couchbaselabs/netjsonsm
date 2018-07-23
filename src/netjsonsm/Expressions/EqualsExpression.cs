namespace netjsonsm.Expressions
{
    public class EqualsExpression : IExpression
    {
        public EqualsExpression(IExpression leftHandSide, IExpression rightHandSide)
        {
            LeftHandSide = leftHandSide;
            RightHandSide = rightHandSide;
        }

        public ExpressionType Type => ExpressionType.Equals;
        public IExpression LeftHandSide { get; }
        public IExpression RightHandSide { get; }
    }
}

// Copyright 2018 Couchbase, Inc. All rights reserved.

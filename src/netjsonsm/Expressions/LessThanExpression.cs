namespace netjsonsm.Expressions
{
    public class LessThanExpression : IExpression
    {
        public LessThanExpression(IExpression leftHandSide, IExpression rightHandSide)
        {
            LeftHandSide = leftHandSide;
            RightHandSide = rightHandSide;
        }

        public ExpressionType Type => ExpressionType.LessThan;
        public IExpression LeftHandSide { get; }
        public IExpression RightHandSide { get; }
    }
}

// Copyright 2018 Couchbase, Inc. All rights reserved.

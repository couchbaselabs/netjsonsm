namespace netjsonsm.Expressions
{
    public class AnyInExpression : IExpression
    {
        public AnyInExpression(int variableIndex, IExpression inExpression, IExpression subExpression)
        {
            VariableIndex = variableIndex;
            InExpression = inExpression;
            SubExpression = subExpression;
        }

        public ExpressionType Type => ExpressionType.AnyIn;
        public int VariableIndex { get; }
        public IExpression InExpression { get; }
        public IExpression SubExpression { get; }
    }
}

// Copyright 2018 Couchbase, Inc. All rights reserved.

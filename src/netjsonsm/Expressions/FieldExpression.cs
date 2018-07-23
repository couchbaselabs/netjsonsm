namespace netjsonsm.Expressions
{
    public class FieldExpression : IExpression
    {
        public FieldExpression(int root, string path)
        {
            Root = root;
            Path = path;
        }

        public ExpressionType Type => ExpressionType.Field;
        public int Root { get; }
        public string Path { get; }
    }
}

// Copyright 2018 Couchbase, Inc. All rights reserved.

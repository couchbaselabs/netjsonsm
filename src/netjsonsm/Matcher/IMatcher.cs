namespace netjsonsm.Matcher
{
    public interface IMatcher
    {
        bool Match(byte[] data);
        bool ExpressionMatched(int expressionIndex);
        void Reset();
    }
}

// Copyright 2018 Couchbase, Inc. All rights reserved.

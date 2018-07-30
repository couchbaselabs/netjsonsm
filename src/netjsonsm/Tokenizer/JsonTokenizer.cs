using System;

namespace netjsonsm.Tokenizer
{
    public class JsonTokenizer : ITokenizer
    {
        private byte[] _data;

        public JsonTokenizer(byte[] data)
        {
            _data = data;
        }

        public int Position { get; private set; }

        public void Reset(byte[] data)
        {
            _data = data;
            Position = 0;
        }

        public void Seek(int position)
        {
            Position = position;
        }

        public TokenizerResult Step()
        {
            if (Position >= _data.Length)
            {
                return new TokenizerResult(TokenType.End);
            }

            // remember start position so we can return the tokens data
            var startPosition = Position;
            var tokenType = TokenType.Unknown;
            var state = TokenizerState.BeginValue;
            var stringHasEscapes = false;
            var numberIsNonInteger = false;

            var exitLoop = false;
            while (!exitLoop)
            {
                if (Position >= _data.Length)
                {
                    switch (state)
                    {
                        case TokenizerState.One:
                        case TokenizerState.Zero:
                        case TokenizerState.DotZero:
                        case TokenizerState.EZero:
                            tokenType = TokenType.Number;
                            break;
                        default:
                            tokenType = TokenType.End;
                            break;
                    }
                    exitLoop = true;
                    continue;
                }

                var c = ReadCharacter();

                switch (state)
                {
                    case TokenizerState.BeginValueOrEmpty:
                        if (char.IsWhiteSpace(c))
                        {
                            startPosition = Position;
                            continue;
                        }

                        if (c == ']')
                        {
                            tokenType = TokenType.ArrayEnd;
                            exitLoop = true;
                            continue;
                        }

                        goto case TokenizerState.BeginValue;
                    case TokenizerState.BeginValue:
                        if (char.IsWhiteSpace(c))
                        {
                            startPosition = Position;
                            continue;
                        }

                        switch (c)
                        {
                            case '{':
                                tokenType = TokenType.ObjectStart;
                                exitLoop = true;
                                continue;
                            case '}':
                                tokenType = TokenType.ObjectEnd;
                                exitLoop = true;
                                continue;
                            case ':':
                                tokenType = TokenType.ObjectKeyDelim;
                                exitLoop = true;
                                continue;
                            case '[':
                                tokenType = TokenType.ArrayStart;
                                exitLoop = true;
                                continue;
                            case ']':
                                tokenType = TokenType.ArrayEnd;
                                exitLoop = true;
                                continue;
                            case ',':
                                tokenType = TokenType.ListDelim;
                                exitLoop = true;
                                continue;
                            case '"':
                                state = TokenizerState.InString;
                                continue;
                            case '-':
                                state = TokenizerState.Negative;
                                continue;
                            case '0':
                                state = TokenizerState.Zero;
                                continue;
                            case 't':
                                state = TokenizerState.T;
                                continue;
                            case 'f':
                                state = TokenizerState.F;
                                continue;
                            case 'n':
                                state = TokenizerState.N;
                                continue;
                            default:
                                if ('1' <= c && c <= '9')
                                {
                                    state = TokenizerState.One;
                                    continue;
                                }

                                return new TokenizerResult(TokenType.Unknown);
                        }
                    case TokenizerState.BeginStringOrEmpty:
                        if (char.IsWhiteSpace(c))
                        {
                            startPosition = Position;
                            continue;
                        }

                        if (c == '}')
                        {
                            tokenType = TokenType.ObjectEnd;
                            exitLoop = true;
                            continue;
                        }

                        goto case TokenizerState.BeginString;
                    case TokenizerState.BeginString:
                        if (char.IsWhiteSpace(c))
                        {
                            startPosition = Position;
                            continue;
                        }

                        if (c == '"')
                        {
                            state = TokenizerState.InString;
                            continue;
                        }

                        return new TokenizerResult(TokenType.Unknown);
                    case TokenizerState.InString:
                        if (c == '"')
                        {
                            tokenType = TokenType.EscapeString;
                            exitLoop = true;
                            continue;
                        }

                        if (c == '\\')
                        {
                            state = TokenizerState.InStringEsc;
                            continue;
                        }

                        if (c == '\n' || char.IsControl(c) || char.IsSymbol(c))
                        {
                            stringHasEscapes = true;
                            continue;
                        }

                        if (c < 0x20)
                        {
                            return new TokenizerResult(TokenType.Unknown);
                        }

                        // continue with current state
                        continue;
                    case TokenizerState.InStringEsc:
                        stringHasEscapes = true;

                        switch (c)
                        {
                            case 'b':
                            case 'f':
                            case 'n':
                            case 'r':
                            case 't':
                            case '\\':
                            case '/':
                            case '"':
                                state = TokenizerState.InString;
                                continue;
                            case 'u':
                                state = TokenizerState.InStringEscU;
                                continue;
                            default:
                                return new TokenizerResult(TokenType.Unknown);
                        }

                    // TODO - would be nice to handle known literals (true, false, null, etc) better
                    case TokenizerState.InStringEscU:
                        if (IsHexCharacter(c))
                        {
                            state = TokenizerState.InStringEscU1;
                            continue;
                        }

                        return new TokenizerResult(TokenType.Unknown);
                    case TokenizerState.InStringEscU1:
                        if (IsHexCharacter(c))
                        {
                            state = TokenizerState.InStringEscU12;
                            continue;
                        }

                        return new TokenizerResult(TokenType.Unknown);
                    case TokenizerState.InStringEscU12:
                        if (IsHexCharacter(c))
                        {
                            state = TokenizerState.InStringEscU123;
                            continue;
                        }

                        return new TokenizerResult(TokenType.Unknown);
                    case TokenizerState.InStringEscU123:
                        if (IsHexCharacter(c))
                        {
                            state = TokenizerState.InString;
                            continue;
                        }

                        return new TokenizerResult(TokenType.Unknown);
                    case TokenizerState.Negative:
                        if (c == '0')
                        {
                            state = TokenizerState.Zero;
                            continue;
                        }

                        if ('1' <= c && c <= '9')
                        {
                            state = TokenizerState.One;
                            continue;
                        }

                        return new TokenizerResult(TokenType.Unknown);
                    case TokenizerState.One:
                        if ('0' <= c && c <= '9')
                        {
                            state = TokenizerState.One;
                            continue;
                        }

                        goto case TokenizerState.Zero;
                    case TokenizerState.Zero:
                        if (c == '.')
                        {
                            state = TokenizerState.Dot;
                            continue;
                        }

                        if (c == 'e' || c == 'E')
                        {
                            state = TokenizerState.E;
                            continue;
                        }

                        // need to rewind one character since this was non-numeric
                        Position--;
                        Seek(Position);
                        tokenType = TokenType.Number;
                        exitLoop = true;
                        continue;
                    case TokenizerState.Dot:
                        numberIsNonInteger = true;

                        if ('0' <= c && c <= '9')
                        {
                            state = TokenizerState.DotZero;
                            continue;
                        }

                        return new TokenizerResult(TokenType.Unknown);
                    case TokenizerState.DotZero:
                        if ('0' <= c && c <= '9')
                        {
                            continue;
                        }

                        if (c == 'e' || c == 'E')
                        {
                            state = TokenizerState.E;
                            continue;
                        }

                        // need to rewind one character since this was non-numeric
                        Position--;
                        Seek(Position);
                        tokenType = TokenType.Number;
                        exitLoop = true;
                        continue;
                    case TokenizerState.E:
                        numberIsNonInteger = true;

                        if (c == '+' || c == '-')
                        {
                            state = TokenizerState.ESign;
                            continue;
                        }

                        goto case TokenizerState.ESign;
                    case TokenizerState.ESign:
                        if ('0' <= c && c <= '9')
                        {
                            state = TokenizerState.EZero;
                            continue;
                        }

                        return new TokenizerResult(TokenType.Unknown);
                    case TokenizerState.EZero:
                        if ('0' <= c && c <= '9')
                        {
                            // continue parsing numbers...
                            continue;
                        }

                        // need to rewind one character since this was non-numeric
                        Position--;
                        Seek(Position);
                        tokenType = TokenType.Number;
                        exitLoop = true;
                        continue;
                    case TokenizerState.T:
                        if (c == 'r')
                        {
                            state = TokenizerState.Tr;
                            continue;
                        }

                        return new TokenizerResult(TokenType.Unknown);
                    case TokenizerState.Tr:
                        if (c == 'u')
                        {
                            state = TokenizerState.Tru;
                            continue;
                        }

                        return new TokenizerResult(TokenType.Unknown);
                    case TokenizerState.Tru:
                        if (c == 'e')
                        {
                            tokenType = TokenType.True;
                            exitLoop = true;
                            continue;
                        }

                        return new TokenizerResult(TokenType.Unknown);
                    case TokenizerState.F:
                        if (c == 'a')
                        {
                            state = TokenizerState.Fa;
                            continue;
                        }

                        return new TokenizerResult(TokenType.Unknown);

                    case TokenizerState.Fa:
                        if (c == 'l')
                        {
                            state = TokenizerState.Fal;
                            continue;
                        }

                        return new TokenizerResult(TokenType.Unknown);

                    case TokenizerState.Fal:
                        if (c == 's')
                        {
                            state = TokenizerState.Fals;
                            continue;
                        }

                        return new TokenizerResult(TokenType.Unknown);

                    case TokenizerState.Fals:
                        if (c == 'e')
                        {
                            tokenType = TokenType.False;
                            exitLoop = true;
                            continue;
                        }

                        return new TokenizerResult(TokenType.Unknown);
                    case TokenizerState.N:
                        if (c == 'u')
                        {
                            state = TokenizerState.Nu;
                            continue;
                        }

                        return new TokenizerResult(TokenType.Unknown);
                    case TokenizerState.Nu:
                        if (c == 'l')
                        {
                            state = TokenizerState.Nul;
                            continue;
                        }

                        return new TokenizerResult(TokenType.Unknown);
                    case TokenizerState.Nul:
                        if (c == 'l')
                        {
                            tokenType = TokenType.Null;
                            exitLoop = true;
                            continue;
                        }

                        return new TokenizerResult(TokenType.Unknown);
                    default:
                        throw new ArgumentException(nameof(state));
                }
            }

            if (tokenType == TokenType.Number && !numberIsNonInteger)
            {
                tokenType = TokenType.Integer;
            }

            if (tokenType == TokenType.EscapeString && !stringHasEscapes)
            {
                tokenType = TokenType.String;
            }

            var data = new byte[Position - startPosition];
            if (data.Length > 0)
            {
                Buffer.BlockCopy(_data, startPosition, data, 0, data.Length);
            }

            return new TokenizerResult(tokenType, data);
        }

        private char ReadCharacter()
        {
            var c = Convert.ToChar(_data[Position]);
            Position++;
            return c;
        }

        private static bool IsHexCharacter(char c)
        {
            return '0' <= c && c <= '9' || 'a' <= c && c <= 'f' || 'A' <= c && c <= 'F';
        }
    }
}

// Copyright 2018 Couchbase, Inc. All rights reserved.

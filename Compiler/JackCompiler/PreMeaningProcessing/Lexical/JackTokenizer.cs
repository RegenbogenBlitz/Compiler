namespace JackCompiler.PreMeaningProcessing.Lexical
{
    using System.Collections.Generic;

    internal static class JackTokenizer
    {
        private static readonly Tokenizer tokenizer;

        static JackTokenizer()
        {
            UntilEndOfLineComment = new IgnoredLexicalElement("comment", @"//.*");
            ClosedComment = new IgnoredLexicalElement("comment", @"/\*(.|\n)*?\*/");
            ApiComment = new IgnoredLexicalElement("comment", @"/\*\*(.|\n)*?\*/");

            Whitespace = new IgnoredLexicalElement("whitespace", @"\s+");

            ClassKeyword = new FixedLexicalElement("keyword", "class");
            ConstructorKeyword = new FixedLexicalElement("keyword", "constructor");
            FunctionKeyword = new FixedLexicalElement("keyword", "function");
            MethodKeyword = new FixedLexicalElement("keyword", "method");
            FieldKeyword = new FixedLexicalElement("keyword", "field");
            StaticKeyword = new FixedLexicalElement("keyword", "static");
            VarKeyword = new FixedLexicalElement("keyword", "var");
            IntKeyword = new FixedLexicalElement("keyword", "int");
            CharKeyword = new FixedLexicalElement("keyword", "char");
            BooleanKeyword = new FixedLexicalElement("keyword", "boolean");
            VoidKeyword = new FixedLexicalElement("keyword", "void");
            TrueKeyword = new FixedLexicalElement("keyword", "true");
            FalseKeyword = new FixedLexicalElement("keyword", "false");
            NullKeyword = new FixedLexicalElement("keyword", "null");
            ThisKeyword = new FixedLexicalElement("keyword", "this");
            LetKeyword = new FixedLexicalElement("keyword", "let");
            DoKeyword = new FixedLexicalElement("keyword", "do");
            IfKeyword = new FixedLexicalElement("keyword", "if");
            ElseKeyword = new FixedLexicalElement("keyword", "else");
            WhileKeyword = new FixedLexicalElement("keyword", "while");
            ReturnKeyword = new FixedLexicalElement("keyword", "return");

            OpenCurlyBracketSymbol = new FixedLexicalElement("symbol", "{");
            CloseCurlyBracketSymbol = new FixedLexicalElement("symbol", "}");
            OpenRoundBracketSymbol = new FixedLexicalElement("symbol", "(");
            CloseRoundBracketSymbol = new FixedLexicalElement("symbol", ")");
            OpenSquareBracketSymbol = new FixedLexicalElement("symbol", "[");
            CloseSquareBracketSymbol = new FixedLexicalElement("symbol", "]");
            PeriodSymbol = new FixedLexicalElement("symbol", ".");
            CommaSymbol = new FixedLexicalElement("symbol", ",");
            SemicolonSymbol = new FixedLexicalElement("symbol", ";");
            PlusSymbol = new FixedLexicalElement("symbol", "+");
            MinusSymbol = new FixedLexicalElement("symbol", "-");
            MultiplySymbol = new FixedLexicalElement("symbol", "*");
            DivideSymbol = new FixedLexicalElement("symbol", "/");
            AndSymbol = new FixedLexicalElement("symbol", "&");
            OrSymbol = new FixedLexicalElement("symbol", "|");
            LessThanSymbol = new FixedLexicalElement("symbol", "<");
            GreaterThanSymbol = new FixedLexicalElement("symbol", ">");
            EqualsSymbol = new FixedLexicalElement("symbol", "=");
            NotSymbol = new FixedLexicalElement("symbol", "~");

            IntegerConstantLexicalElement = new FlexibleLexicalElement("integerConstant", @"(?<!\w+)\d{1,5}(?!\w+)");
            StringConstantLexicalElement = new FlexibleLexicalElement("stringConstant", "\".*\"", "(?<=\").*(?=\")");
            IdentifierLexicalElement = new FlexibleLexicalElement("identifier", @"(?<!\d+\w*)[a-zA-Z_]\w*");

            var lexicalElementDefinitions = new ILexicalElementDefinition[]
            {
                UntilEndOfLineComment,
                ClosedComment,
                ApiComment,

                Whitespace,

                ClassKeyword, ConstructorKeyword, FunctionKeyword, 
                MethodKeyword, FieldKeyword, StaticKeyword, 
                VarKeyword, IntKeyword, CharKeyword, 
                BooleanKeyword, VoidKeyword, TrueKeyword,
                FalseKeyword, NullKeyword, ThisKeyword, 
                LetKeyword, DoKeyword, IfKeyword, 
                ElseKeyword, WhileKeyword, ReturnKeyword,
                
                OpenCurlyBracketSymbol, CloseCurlyBracketSymbol, 
                OpenRoundBracketSymbol, CloseRoundBracketSymbol, 
                OpenSquareBracketSymbol, CloseSquareBracketSymbol, 
                PeriodSymbol, CommaSymbol, SemicolonSymbol, 
                PlusSymbol, MinusSymbol, 
                MultiplySymbol, DivideSymbol, 
                AndSymbol, OrSymbol, 
                LessThanSymbol, GreaterThanSymbol, 
                EqualsSymbol, NotSymbol,
                
                IntegerConstantLexicalElement,
                StringConstantLexicalElement,
                IdentifierLexicalElement,
            };

            tokenizer = new Tokenizer(lexicalElementDefinitions);
        }

        private static IgnoredLexicalElement UntilEndOfLineComment { get; set; }
        private static IgnoredLexicalElement ClosedComment { get; set; }
        private static IgnoredLexicalElement ApiComment { get; set; }
        private static IgnoredLexicalElement Whitespace { get; set; }

        internal static FixedLexicalElement ClassKeyword { get; private set; }
        internal static FixedLexicalElement ConstructorKeyword { get; private set; }
        internal static FixedLexicalElement FunctionKeyword { get; private set; }
        internal static FixedLexicalElement MethodKeyword { get; private set; }
        internal static FixedLexicalElement FieldKeyword { get; private set; }
        internal static FixedLexicalElement StaticKeyword { get; private set; }
        internal static FixedLexicalElement VarKeyword { get; private set; }
        internal static FixedLexicalElement IntKeyword { get; private set; }
        internal static FixedLexicalElement CharKeyword { get; private set; }
        internal static FixedLexicalElement BooleanKeyword { get; private set; }
        internal static FixedLexicalElement VoidKeyword { get; private set; }
        internal static FixedLexicalElement TrueKeyword { get; private set; }
        internal static FixedLexicalElement FalseKeyword { get; private set; }
        internal static FixedLexicalElement NullKeyword { get; private set; }
        internal static FixedLexicalElement ThisKeyword { get; private set; }
        internal static FixedLexicalElement LetKeyword { get; private set; }
        internal static FixedLexicalElement DoKeyword { get; private set; }
        internal static FixedLexicalElement IfKeyword { get; private set; }
        internal static FixedLexicalElement ElseKeyword { get; private set; }
        internal static FixedLexicalElement WhileKeyword { get; private set; }
        internal static FixedLexicalElement ReturnKeyword { get; private set; }

        internal static FixedLexicalElement OpenCurlyBracketSymbol { get; private set; }
        internal static FixedLexicalElement CloseCurlyBracketSymbol { get; private set; }
        internal static FixedLexicalElement OpenRoundBracketSymbol { get; private set; }
        internal static FixedLexicalElement CloseRoundBracketSymbol { get; private set; }
        internal static FixedLexicalElement OpenSquareBracketSymbol { get; private set; }
        internal static FixedLexicalElement CloseSquareBracketSymbol { get; private set; }
        internal static FixedLexicalElement PeriodSymbol { get; private set; }
        internal static FixedLexicalElement CommaSymbol { get; private set; }
        internal static FixedLexicalElement SemicolonSymbol { get; private set; }
        internal static FixedLexicalElement PlusSymbol { get; private set; }
        internal static FixedLexicalElement MinusSymbol { get; private set; }
        internal static FixedLexicalElement MultiplySymbol { get; private set; }
        internal static FixedLexicalElement DivideSymbol { get; private set; }
        internal static FixedLexicalElement AndSymbol { get; private set; }
        internal static FixedLexicalElement OrSymbol { get; private set; }
        internal static FixedLexicalElement LessThanSymbol { get; private set; }
        internal static FixedLexicalElement GreaterThanSymbol { get; private set; }
        internal static FixedLexicalElement EqualsSymbol { get; private set; }
        internal static FixedLexicalElement NotSymbol { get; private set; }

        internal static FlexibleLexicalElement IntegerConstantLexicalElement { get; private set; }
        internal static FlexibleLexicalElement StringConstantLexicalElement { get; private set; }
        internal static FlexibleLexicalElement IdentifierLexicalElement { get; private set; }
        
        internal static IEnumerable<LexicalElement> Tokenize(string[] fileContent)
        {
            return tokenizer.Tokenize(fileContent);
        }
    }
}

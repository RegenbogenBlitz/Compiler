namespace JackCompiler.PreMeaningProcessing.Syntactical
{
    using System.Collections.Generic;
    using Lexical;

    internal static class JackSyntacticalAnalyiser
    {
        private static readonly SyntaxElementDefinition classDefinition;

        static JackSyntacticalAnalyiser()
        {
            var termGrabbers = new List<IPhraseGrabber>();
            var expressionGrabbers = new List<IPhraseGrabber>();
            var expressionListGrabbers = new List<IPhraseGrabber>();
            var subroutineCallGrabbers = new List<IPhraseGrabber>();

            var termDefinition =
                new SyntaxElementDefinition(
                    "term",
                     new AnyPhraseGrabber(termGrabbers));

            var expressionDefinition =
                new SyntaxElementDefinition(
                    "expression",
                     expressionGrabbers);

            var expressionListDefinition =
                new SyntaxElementDefinition(
                    "expressionList",
                     new NoughtOnePhraseGrabber(expressionListGrabbers));

            var subroutineCallDefinition =
                new SyntaxElementDefinition(
                    "subroutineCall",
                     subroutineCallGrabbers);

            termGrabbers.AddRange(new IPhraseGrabber[]
            {
                JackTokenizer.IntegerConstantLexicalElement,
                JackTokenizer.StringConstantLexicalElement,
                JackTokenizer.TrueKeyword,
                JackTokenizer.FalseKeyword,
                JackTokenizer.NullKeyword,
                JackTokenizer.ThisKeyword,
                subroutineCallDefinition,
                new SequencePhraseGrabber(new IPhraseGrabber[]
                {
                    JackTokenizer.IdentifierLexicalElement, // varName
                    JackTokenizer.OpenSquareBracketSymbol,
                    expressionDefinition,
                    JackTokenizer.CloseSquareBracketSymbol
                }),
                JackTokenizer.IdentifierLexicalElement, // varName
                new SequencePhraseGrabber(new IPhraseGrabber[]
                {
                    JackTokenizer.OpenRoundBracketSymbol,
                    expressionDefinition,
                    JackTokenizer.CloseRoundBracketSymbol
                }),
                new SequencePhraseGrabber(new IPhraseGrabber[]
                {
                    new AnyPhraseGrabber(new IPhraseGrabber[]
                    {
                        JackTokenizer.MinusSymbol,
                        JackTokenizer.NotSymbol
                    }),
                    termDefinition
                })
            });

            expressionGrabbers.AddRange(new IPhraseGrabber[]
            {
                termDefinition,
                new NoughtManyPhraseGrabber(new IPhraseGrabber[]
                {
                    new AnyPhraseGrabber(new IPhraseGrabber[]
                    {
                        JackTokenizer.PlusSymbol,
                        JackTokenizer.MinusSymbol,
                        JackTokenizer.MultiplySymbol,
                        JackTokenizer.DivideSymbol,
                        JackTokenizer.AndSymbol,
                        JackTokenizer.OrSymbol,
                        JackTokenizer.LessThanSymbol,
                        JackTokenizer.GreaterThanSymbol,
                        JackTokenizer.EqualsSymbol
                    }),
                    termDefinition
                })
            });

            expressionListGrabbers.AddRange(new IPhraseGrabber[]
            {
                expressionDefinition,
                new NoughtManyPhraseGrabber(new IPhraseGrabber[]
                {
                    JackTokenizer.CommaSymbol,
                    expressionDefinition
                })
            });

            subroutineCallGrabbers.AddRange(new IPhraseGrabber[]
            {
                new NoughtOnePhraseGrabber(new IPhraseGrabber[]
                {
                    new AnyPhraseGrabber(new IPhraseGrabber[]
                    {
                        JackTokenizer.IdentifierLexicalElement, // className/varName
                    }),
                    JackTokenizer.PeriodSymbol
                }),
                JackTokenizer.IdentifierLexicalElement, // subroutineName
                JackTokenizer.OpenRoundBracketSymbol,
                expressionListDefinition,
                JackTokenizer.CloseRoundBracketSymbol
            });

            var statementGrabbers = new List<IPhraseGrabber>();

            var statementsDefinition = new SyntaxElementDefinition(
                "statements",
                new NoughtManyPhraseGrabber(new AnyPhraseGrabber(statementGrabbers)));

            var letStatementDefinition =
                new SyntaxElementDefinition(
                    "letStatement",
                    new IPhraseGrabber[]
                    {
                        JackTokenizer.LetKeyword,
                        JackTokenizer.IdentifierLexicalElement, // varName
                        new NoughtOnePhraseGrabber(new IPhraseGrabber[]
                        {
                            JackTokenizer.OpenSquareBracketSymbol,
                            expressionDefinition,
                            JackTokenizer.CloseSquareBracketSymbol
                        }),
                        JackTokenizer.EqualsSymbol,
                        expressionDefinition,
                        JackTokenizer.SemicolonSymbol
                    });

            var ifStatementDefinition =
                new SyntaxElementDefinition(
                    "ifStatement",
                    new IPhraseGrabber[]
                    {
                        JackTokenizer.IfKeyword,
                        JackTokenizer.OpenRoundBracketSymbol,
                        expressionDefinition,
                        JackTokenizer.CloseRoundBracketSymbol,
                        JackTokenizer.OpenCurlyBracketSymbol,
                        statementsDefinition,
                        JackTokenizer.CloseCurlyBracketSymbol,
                        new NoughtOnePhraseGrabber(new IPhraseGrabber[]
                        {
                            JackTokenizer.ElseKeyword,
                            JackTokenizer.OpenCurlyBracketSymbol,
                            statementsDefinition,
                            JackTokenizer.CloseCurlyBracketSymbol
                        })
                    });

            var whileStatementDefinition =
                new SyntaxElementDefinition(
                    "whileStatement",
                    new IPhraseGrabber[]
                    {
                        JackTokenizer.WhileKeyword,
                        JackTokenizer.OpenRoundBracketSymbol,
                        expressionDefinition,
                        JackTokenizer.CloseRoundBracketSymbol,
                        JackTokenizer.OpenCurlyBracketSymbol,
                        statementsDefinition,
                        JackTokenizer.CloseCurlyBracketSymbol
                    });

            var doStatementDefinition =
                new SyntaxElementDefinition(
                    "doStatement",
                    new IPhraseGrabber[]
                    {
                        JackTokenizer.DoKeyword,
                        subroutineCallDefinition,
                        JackTokenizer.SemicolonSymbol
                    });

            var returnStatementDefinition =
                new SyntaxElementDefinition(
                    "returnStatement",
                    new IPhraseGrabber[]
                    {
                        JackTokenizer.ReturnKeyword,
                        new NoughtOnePhraseGrabber(expressionDefinition),
                        JackTokenizer.SemicolonSymbol
                    });

            statementGrabbers.AddRange(new IPhraseGrabber[]
            {
                letStatementDefinition,
                ifStatementDefinition,
                whileStatementDefinition,
                doStatementDefinition,
                returnStatementDefinition
            });

            var typeDefinition = new SyntaxElementDefinition(
                "type",
                new AnyPhraseGrabber(
                    new IPhraseGrabber[]
                    {
                        JackTokenizer.IntKeyword, 
                        JackTokenizer.CharKeyword,
                        JackTokenizer.BooleanKeyword, 
                        JackTokenizer.IdentifierLexicalElement // className
                    }));

            var parameterDefiniton = new SyntaxElementDefinition(
                "parameter",
                new IPhraseGrabber[]
                {
                    typeDefinition,
                    JackTokenizer.IdentifierLexicalElement // varName
                });

            var parameterListDefiniton = new SyntaxElementDefinition(
                "parameterList",
                new IPhraseGrabber[]
                {
                    new NoughtOnePhraseGrabber(new IPhraseGrabber[]
                    {
                        parameterDefiniton,
                        new NoughtManyPhraseGrabber(new IPhraseGrabber[]
                        {
                           JackTokenizer.CommaSymbol,
                           parameterDefiniton
                        })   
                    })
                });

            var varDecDefinition = new SyntaxElementDefinition(
                "varDec",
                new IPhraseGrabber[]
                {
                    JackTokenizer.VarKeyword,
                    typeDefinition,
                    JackTokenizer.IdentifierLexicalElement, // varName
                    new NoughtManyPhraseGrabber(new IPhraseGrabber[]
                    {
                        JackTokenizer.CommaSymbol,
                        JackTokenizer.IdentifierLexicalElement // varName
                    }),
                    JackTokenizer.SemicolonSymbol
                });

            var classVarDecDefinition = new SyntaxElementDefinition(
                "classVarDec",
                new IPhraseGrabber[]
                {
                    new AnyPhraseGrabber(new IPhraseGrabber[]
                    {
                        JackTokenizer.StaticKeyword,
                        JackTokenizer.FieldKeyword
                    }),
                    typeDefinition,
                    JackTokenizer.IdentifierLexicalElement, // varName
                    new NoughtManyPhraseGrabber(new IPhraseGrabber[]
                    {
                        JackTokenizer.CommaSymbol,
                        JackTokenizer.IdentifierLexicalElement // varName
                    }),
                    JackTokenizer.SemicolonSymbol 
                });

            var subroutineBodyDefinition = new SyntaxElementDefinition(
                 "subroutineBody",
                 new IPhraseGrabber[]
                {
                    JackTokenizer.OpenCurlyBracketSymbol,
                    new NoughtManyPhraseGrabber(varDecDefinition),
                    statementsDefinition,
                    JackTokenizer.CloseCurlyBracketSymbol
                });

            var subroutineDecDefinition = new SyntaxElementDefinition(
                 "subroutineDec",
                 new IPhraseGrabber[]
                {
                    new AnyPhraseGrabber(new IPhraseGrabber[]
                    {
                        JackTokenizer.ConstructorKeyword,
                        JackTokenizer.FunctionKeyword,
                        JackTokenizer.MethodKeyword,
                    }),
                    new AnyPhraseGrabber(new IPhraseGrabber[]
                    {
                        JackTokenizer.VoidKeyword,
                        typeDefinition
                    }),
                    JackTokenizer.IdentifierLexicalElement, // subroutineName
                    JackTokenizer.OpenRoundBracketSymbol,
                    parameterListDefiniton,
                    JackTokenizer.CloseRoundBracketSymbol,
                    subroutineBodyDefinition
                });

            classDefinition = new SyntaxElementDefinition(
                "class",
                new IPhraseGrabber[]
                {
                    JackTokenizer.ClassKeyword,
                    JackTokenizer.IdentifierLexicalElement, // className
                    JackTokenizer.OpenCurlyBracketSymbol,
                    new NoughtManyPhraseGrabber(classVarDecDefinition),
                    new NoughtManyPhraseGrabber(subroutineDecDefinition),
                    JackTokenizer.CloseCurlyBracketSymbol,
                });
        }

        internal static SyntaxAnalysisResult Analyse(
            BookmarkedArray<LexicalElement> remainingLexcialElements)
        {
            return classDefinition.GrabPhrases(remainingLexcialElements);
        }
    }
}

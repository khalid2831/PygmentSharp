using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

using PygmentSharp.Core.Tokens;

namespace PygmentSharp.Core
{
    public class RegexLexerContext
    {
        public int Position { get; set; }

        public Match Match { get; }

        public Stack<string> StateStack { get; }
        public TokenType RuleTokenType { get; }

        public RegexLexerContext(int position, Match match, Stack<string> stateStack, TokenType ruleTokenType)
        {
            Position = position;
            Match = match;
            StateStack = stateStack;
            RuleTokenType = ruleTokenType;
        }
    }

    public abstract class RegexLexer : Lexer
    {
        protected override IEnumerable<Token> GetTokensUnprocessed(string text)
        {
            var rules = GetStateRules();
            int pos = 0;
            var stateStack = new Stack<string>(50);
            stateStack.Push("root");
            var currentStateRules = rules[stateStack.Peek()];

            while (true)
            {
                bool found = false;
                foreach (var rule in currentStateRules)
                {
                    var m = rule.Regex.Match(text, pos);
                    if (m.Success)
                    {
                        var context = new RegexLexerContext(pos, m, stateStack, rule.TokenType);
                        Debug.Assert(m.Index == pos, $"Regex \"{rule.Regex}\" should have matched at position {pos} but matched at {m.Index}");

                        var tokens = rule.Action.Execute(context);

                        foreach (var token in tokens)
                            yield return token;

                        pos = context.Position;
                        currentStateRules = rules[stateStack.Peek()];
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    if (pos >= text.Length)
                        break;

                    if (text[pos] == '\n')
                    {
                        stateStack.Clear();
                        stateStack.Push("root");
                        currentStateRules = rules["root"];
                        yield return new Token(pos, TokenTypes.Text, "\n");
                        pos++;
                        continue;
                    }

                    yield return new Token(pos, TokenTypes.Error, text[pos].ToString());
                    pos++;
                }
            }


        }

        /// <summary>
        /// Gets the state transition rules for the lexer. Each time a regex is matched,
        /// the internal state machine can be bumped to a new state which determines what
        /// regexes become valid again
        /// </summary>
        /// <returns></returns>
        protected abstract IDictionary<string, StateRule[]> GetStateRules();
    }
}
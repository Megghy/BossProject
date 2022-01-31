using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WorldEdit.Expressions
{
	public static class Parser
	{
		public static Expression ParseExpression(IEnumerable<Token> postfix)
		{
			var stack = new Stack<Expression>();
			foreach (var token in postfix)
			{
				switch (token.Type)
				{
					case Token.TokenType.BinaryOperator:
						switch ((Token.OperatorType)token.Value)
						{
							case Token.OperatorType.And:
								stack.Push(new AndExpression(stack.Pop(), stack.Pop()));
								continue;
							case Token.OperatorType.Or:
								stack.Push(new OrExpression(stack.Pop(), stack.Pop()));
								continue;
							case Token.OperatorType.Xor:
								stack.Push(new XorExpression(stack.Pop(), stack.Pop()));
								continue;
							default:
								return null;
						}
					case Token.TokenType.Test:
						stack.Push(new TestExpression((Test)token.Value));
						continue;
					case Token.TokenType.UnaryOperator:
						switch ((Token.OperatorType)token.Value)
						{
							case Token.OperatorType.Not:
								stack.Push(new NotExpression(stack.Pop()));
								continue;
							default:
								return null;
						}
					default:
						return null;
				}
			}
			return stack.Pop();
		}
		public static List<Token> ParseInfix(string str)
		{
			str = str.Replace(" ", "").ToLowerInvariant();
			var tokens = new List<Token>();

			for (int i = 0; i < str.Length; i++)
			{
				switch (str[i])
				{
					case '&':
						if (str[i + 1] == '&')
							i++;
						tokens.Add(new Token { Type = Token.TokenType.BinaryOperator, Value = Token.OperatorType.And });
						continue;
					case '!':
						tokens.Add(new Token { Type = Token.TokenType.UnaryOperator, Value = Token.OperatorType.Not });
						continue;
					case '|':
						if (str[i + 1] == '|')
							i++;
						tokens.Add(new Token { Type = Token.TokenType.BinaryOperator, Value = Token.OperatorType.Or });
						continue;
					case '^':
						tokens.Add(new Token { Type = Token.TokenType.BinaryOperator, Value = Token.OperatorType.Xor });
						continue;
					case '(':
						tokens.Add(new Token { Type = Token.TokenType.OpenParentheses });
						continue;
					case ')':
						tokens.Add(new Token { Type = Token.TokenType.CloseParentheses });
						continue;
				}

				var test = new StringBuilder();
				while (i < str.Length && (char.IsLetterOrDigit(str[i]) || str[i] == '!' || str[i] == '='))
					test.Append(str[i++]);
				i--;

				string[] expression = test.ToString().Split('=');
				string lhs = expression[0];
				string rhs = "";
				bool negated = false;

				if (expression.Length > 1)
				{
					if (lhs[lhs.Length - 1] == '!')
					{
						lhs = lhs.Substring(0, lhs.Length - 1);
						negated = true;
					}
					rhs = expression[1];
				}
				tokens.Add(new Token { Type = Token.TokenType.Test, Value = ParseTest(lhs, rhs, negated) });
			}
			return tokens;
		}
		public static List<Token> ParsePostfix(IEnumerable<Token> infix)
		{
			var postfix = new List<Token>();
			var stack = new Stack<Token>();

			foreach (var token in infix)
			{
				switch (token.Type)
				{
					case Token.TokenType.BinaryOperator:
					case Token.TokenType.OpenParentheses:
					case Token.TokenType.UnaryOperator:
						stack.Push(token);
						break;
					case Token.TokenType.CloseParentheses:
						while (stack.Peek().Type != Token.TokenType.OpenParentheses)
							postfix.Add(stack.Pop());
						stack.Pop();

						if (stack.Count > 0 && stack.Peek().Type == Token.TokenType.UnaryOperator)
							postfix.Add(stack.Pop());
						break;
					case Token.TokenType.Test:
						postfix.Add(token);
						break;
				}
			}

			while (stack.Count > 0)
				postfix.Add(stack.Pop());
			return postfix;
		}
		public static Test ParseTest(string lhs, string rhs, bool negated)
		{
			Test test;
			switch (lhs)
            {
                case "lh":
                case "honey":
                    return test = t => t.liquid > 0 && t.liquidType() == 2;
                case "nlh":
                case "nhoney":
                    return test = t => t.liquidType() != 2;
                case "ll":
                case "lava":
					return test = t => t.liquid > 0 && t.liquidType() == 1;
                case "nll":
                case "nlava":
                    return test = t => t.liquidType() != 1;
                case "li":
                case "liquid":
                    return test = t => t.liquid > 0;
                case "nli":
                case "nliquid":
                    return test = t => t.liquid == 0;
                case "t":
				case "tile":
                    {
                        if (string.IsNullOrEmpty(rhs))
                            return test = t => t.active();

                        List<int> tiles = Tools.GetTileID(rhs);
                        if (tiles.Count == 0 || tiles.Count > 1)
                            throw new ArgumentException();
                        return test = t => (t.active() && t.type == tiles[0]) != negated;
                    }
                case "nt":
                case "ntile":
                    return test = t => !t.active();
                case "tp":
				case "tilepaint":
					{
						if (string.IsNullOrEmpty(rhs))
							return test = t => t.active() && t.color() != 0;

						var colors = Tools.GetColorID(rhs);
						if (colors.Count == 0 || colors.Count > 1)
							throw new ArgumentException();
						return test = t => (t.active() && t.color() == colors[0]) != negated;
                    }
                case "ntp":
                case "ntilepaint":
                    return test = t => t.color() == 0;
                case "w":
				case "wall":
                    {
                        if (string.IsNullOrEmpty(rhs))
                            return test = t => t.wall != 0;

                        var walls = Tools.GetTileID(rhs);
                        if (walls.Count == 0 || walls.Count > 1)
                            throw new ArgumentException();
                        return test = t => (t.wall == walls[0]) != negated;
                    }
                case "nw":
                case "nwall":
                    return test = t => t.wall == 0;
                case "wp":
				case "wallpaint":
					{
						if (string.IsNullOrEmpty(rhs))
							return test = t => t.wall > 0 && t.wallColor() != 0;

						var colors = Tools.GetColorID(rhs);
						if (colors.Count == 0 || colors.Count > 1)
							throw new ArgumentException();
						return test = t => (t.wall > 0 && t.wallColor() == colors[0]) != negated;
                    }
                case "nwp":
                case "nwallpaint":
                    return test = t => t.wallColor() == 0;
                case "lw":
                case "water":
                    return test = t => t.liquid > 0 && t.liquidType() == 0;
                case "nlw":
                case "nwater":
                    return test = t => t.liquidType() != 0;
                case "wire":
				case "wire1":
				case "wirered":
				case "redwire":
					return test = t => t.wire();
                case "nwire":
                case "nwire1":
                case "nwirered":
                case "nredwire":
                    return test = t => !t.wire();
                case "wire2":
				case "wireblue":
				case "bluewire":
					return test = t => t.wire2();
                case "nwire2":
                case "nwireblue":
                case "nbluewire":
                    return test = t => !t.wire2();
                case "wire3":
				case "wiregreen":
				case "greenwire":
					return test = t => t.wire3();
                case "nwire3":
                case "nwiregreen":
                case "ngreenwire":
                    return test = t => !t.wire3();
                case "wire4":
				case "wireyellow":
				case "yellowwire":
					return test = t => t.wire4();
                case "nwire4":
                case "nwireyellow":
                case "nyellowwire":
                    return test = t => !t.wire4();
                case "a":
				case "active":
					return test = t => t.active() && !t.inActive();
                case "na":
				case "nactive":
					return test = t => t.inActive();
				case "s":
				case "slope":
					{
						if (string.IsNullOrEmpty(rhs))
							return test = t => ((t.slope() != 0) || t.halfBrick());

						int slope = Tools.GetSlopeID(rhs);
						if (slope == -1)
							throw new ArgumentException();
						return test = t => (t.active() && ((slope == 1) ? t.halfBrick() : (t.slope() == (byte)slope))) != negated;
                    }
                case "ns":
                case "nslope":
                    return test = t => ((t.slope() == 0) && !t.halfBrick());
                case "ac":
                case "actuator":
                    return test = t => t.actuator();
                case "nac":
                case "nactuator":
                    return test = t => !t.actuator();
                default:
					throw new ArgumentException("Invalid test.");
			}
		}
		public static bool TryParseTree(IEnumerable<string> parameters, out Expression expression)
		{
			expression = null;
			if (parameters.FirstOrDefault() != "=>")
				return false;

			try
			{
				expression = ParseExpression(ParsePostfix(ParseInfix(string.Join(" ", parameters.Skip(1)))));
				return true;
			}
			catch
			{
				return false;
			}
		}
	}
}

using OTAPI.Tile;

namespace WorldEdit.Expressions
{
	public delegate bool Test(ITile tile);

	public sealed class TestExpression : Expression
	{
		public Test Test;

		public TestExpression(Test test)
		{
			Test = test;
		}

		public override bool Evaluate(ITile tile)
		{
			return Test(tile);
		}
	}
}

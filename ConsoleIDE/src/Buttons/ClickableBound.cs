namespace ConsoleIDE.Buttons;

public readonly struct Coordinate
{
	public readonly int X;
	public readonly int Y;

	public Coordinate(int x, int y)
	{
		X = x;
		Y = y;
	}

	public readonly Coordinate AddTo(Coordinate other)
	{
		return new Coordinate(X+other.X, Y+other.Y);
	}
}

public readonly struct ClickableBound
{
	public readonly Coordinate TopLeft;
	public readonly Coordinate TopRight;
	public readonly Coordinate BottomLeft;
	public readonly Coordinate BottomRight;

	public ClickableBound(Coordinate topLeft, Coordinate topRight, Coordinate bottomLeft, Coordinate bottomRight)
	{
		TopLeft = topLeft;
		TopRight = topRight;
		BottomLeft = bottomLeft;
		BottomRight = bottomRight;
	}

	public ClickableBound(Coordinate topLeft, Coordinate bottomRight)
		: this(
			topLeft, 
			new(bottomRight.X, topLeft.Y), 
			new(topLeft.X, bottomRight.Y), 
			bottomRight
		) {}

	public readonly bool Contains(int x, int y)
	{
		return
			(x >= TopLeft.X) &&
			(y >= TopLeft.Y) &&
			(x <= BottomRight.X) &&
			(y < BottomRight.Y);
	}
}
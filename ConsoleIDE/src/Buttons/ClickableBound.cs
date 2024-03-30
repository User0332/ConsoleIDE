namespace ConsoleIDE.Buttons;

public readonly struct Coordinate(int x, int y)
{
	public readonly int X = x;
	public readonly int Y = y;

	public readonly Coordinate AddTo(Coordinate other)
	{
		return new Coordinate(X+other.X, Y+other.Y);
	}

	public readonly Coordinate AddY(int toAdd)
	{
		return AddTo(new(0, toAdd));
	}


	public readonly Coordinate AddX(int toAdd)
	{
		return AddTo(new(toAdd, 0));
	}

	public readonly Coordinate WithY(int newY)
	{
		return new(X, newY);
	}


	public readonly Coordinate WithX(int newX)
	{
		return new(newX, Y);
	}
}

public readonly struct ClickableBound(Coordinate topLeft, Coordinate topRight, Coordinate bottomLeft, Coordinate bottomRight)
{
	public readonly Coordinate TopLeft = topLeft;
	public readonly Coordinate TopRight = topRight;
	public readonly Coordinate BottomLeft = bottomLeft;
	public readonly Coordinate BottomRight = bottomRight;

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
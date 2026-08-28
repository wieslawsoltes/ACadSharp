using ACadSharp.Entities;
using ACadSharp.Tables;
using CSMath;
using Xunit;

namespace ACadSharp.Tests.Entities;

public class ShapeTests
{
	[Fact]
	public void ClonePreservesPublicShapeIdentity()
	{
		TextStyle style = new TextStyle("symbols")
		{
			Flags = StyleFlags.IsShape,
			Filename = "symbols.shx",
		};
		Shape shape = new Shape(style)
		{
			ShapeName = "PUMP",
			ShapeNumber = 321,
			InsertionPoint = new XYZ(2, 3, 4),
		};

		Shape clone = (Shape)shape.Clone();

		Assert.Equal("PUMP", clone.ShapeName);
		Assert.Equal((ushort)321, clone.ShapeNumber);
		Assert.Equal("symbols.shx", clone.ShapeStyle.Filename);
	}
}

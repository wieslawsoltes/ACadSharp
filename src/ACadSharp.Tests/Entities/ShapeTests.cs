using ACadSharp.Entities;
using ACadSharp.Tables;
using CSMath;
using System;
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

	[Fact]
	public void TranslationRotationAndUniformScalePreserveShapeFrame()
	{
		TextStyle style = new TextStyle("symbols")
		{
			Flags = StyleFlags.IsShape,
		};
		Shape shape = new Shape(style)
		{
			InsertionPoint = new XYZ(1, 2, 0),
			Size = 2,
			RelativeXScale = 3,
		};

		shape.ApplyTranslation(new XYZ(4, 5, 0));
		shape.ApplyRotation(XYZ.AxisZ, Math.PI / 2);
		shape.ApplyScaling(new XYZ(2, 2, 2));

		Assert.Equal(new XYZ(-14, 10, 0), shape.InsertionPoint);
		Assert.Equal(Math.PI / 2, shape.Rotation, 10);
		Assert.Equal(4, shape.Size, 10);
		Assert.Equal(3, shape.RelativeXScale, 10);
		Assert.Equal(0, shape.ObliqueAngle, 10);
	}
}

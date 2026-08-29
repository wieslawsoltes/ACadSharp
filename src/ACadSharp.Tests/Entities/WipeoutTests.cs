using ACadSharp.Entities;
using CSMath;
using Xunit;

namespace ACadSharp.Tests.Entities;

public class WipeoutTests
{
	[Fact]
	public void TranslationMovesInsertionWithoutTranslatingPixelVectors()
	{
		Wipeout wipeout = CreateWipeout();

		wipeout.ApplyTransform(Transform.CreateTranslation(new XYZ(10, 20, 30)));

		Assert.Equal(new XYZ(11, 22, 33), wipeout.InsertPoint);
		Assert.Equal(new XYZ(2, 0, 0), wipeout.UVector);
		Assert.Equal(new XYZ(0, 3, 0), wipeout.VVector);
	}

	[Fact]
	public void RotationAndScaleTransformPixelVectorsAsDirections()
	{
		Wipeout wipeout = CreateWipeout();
		Transform transform = new Transform(
			translation: new XYZ(7, 8, 9),
			scale: new XYZ(2, 3, 4),
			rotation: new XYZ(0, 0, System.Math.PI / 2));

		XYZ expectedInsert = transform.ApplyTransform(wipeout.InsertPoint);
		XYZ expectedU = transform.ApplyTransform(wipeout.InsertPoint + wipeout.UVector) - expectedInsert;
		XYZ expectedV = transform.ApplyTransform(wipeout.InsertPoint + wipeout.VVector) - expectedInsert;
		wipeout.ApplyTransform(transform);

		Assert.Equal(expectedInsert, wipeout.InsertPoint);
		Assert.Equal(expectedU, wipeout.UVector);
		Assert.Equal(expectedV, wipeout.VVector);
	}

	[Fact]
	public void CloneOwnsItsClipBoundaryVertices()
	{
		Wipeout source = CreateWipeout();
		Wipeout clone = (Wipeout)source.Clone();

		clone.ClipBoundaryVertices[0] = new XY(100, 200);
		clone.ClipBoundaryVertices.Add(new XY(300, 400));

		Assert.Equal(new XY(-0.5, -0.5), source.ClipBoundaryVertices[0]);
		Assert.Equal(2, source.ClipBoundaryVertices.Count);
	}

	[Fact]
	public void BoundingBoxMapsPixelBoundaryThroughImageFrame()
	{
		Wipeout wipeout = CreateWipeout();

		BoundingBox bounds = wipeout.GetBoundingBox();

		Assert.Equal(new XYZ(1, 2, 3), bounds.Min);
		Assert.Equal(new XYZ(21, 62, 3), bounds.Max);
	}

	[Fact]
	public void InsideClipBoundsUseHalfPixelBoundaryConvention()
	{
		Wipeout wipeout = CreateWipeout();
		wipeout.ClipBoundaryVertices.Clear();
		wipeout.ClipBoundaryVertices.Add(new XY(0.5, 0.5));
		wipeout.ClipBoundaryVertices.Add(new XY(2.5, 3.5));
		wipeout.ClipMode = ClipMode.Inside;

		BoundingBox bounds = wipeout.GetBoundingBox();

		Assert.Equal(new XYZ(3, 5, 3), bounds.Min);
		Assert.Equal(new XYZ(7, 14, 3), bounds.Max);
	}

	private static Wipeout CreateWipeout()
	{
		var wipeout = new Wipeout
		{
			InsertPoint = new XYZ(1, 2, 3),
			UVector = new XYZ(2, 0, 0),
			VVector = new XYZ(0, 3, 0),
			Size = new XY(10, 20),
			ClippingState = true,
			ClipMode = ClipMode.Inside,
		};
		wipeout.ClipBoundaryVertices.Add(new XY(-0.5, -0.5));
		wipeout.ClipBoundaryVertices.Add(new XY(9.5, 19.5));
		return wipeout;
	}
}

using ACadSharp.Entities;
using ACadSharp.Tests.Common;
using CSMath;
using Xunit;

namespace ACadSharp.Tests.Entities
{
	public class Polyline3DTests : CommonPolylineTests<Polyline3D, Vertex3D>
	{
		[Fact]
		public void ContainsTypeFlagTest()
		{
			Polyline3D polyline = new Polyline3D();

			Assert.True(polyline.Flags.HasFlag(PolylineFlags.Polyline3D));
			Assert.False(polyline.Flags.HasFlag(PolylineFlags.PolygonMesh));
			Assert.False(polyline.Flags.HasFlag(PolylineFlags.PolyfaceMesh));
		}

		[Fact]
		public void ApplyTransformKeepsWorldCoordinateVertices()
		{
			var polyline = new Polyline3D
			{
				Normal = XYZ.AxisY,
			};
			polyline.Vertices.Add(new Vertex3D(new XYZ(1, 2, 3)));

			polyline.ApplyTransform(Transform.CreateTranslation(new XYZ(5, 6, 7)));

			AssertUtils.AreEqual(new XYZ(6, 8, 10), polyline.Vertices[0].Location);
		}
	}
}

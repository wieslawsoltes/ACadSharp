using ACadSharp.Entities;
using ACadSharp.Tests.Common;
using CSMath;
using Xunit;

namespace ACadSharp.Tests.Entities
{
	public class PolygonMeshTests : CommonPolylineTests<PolygonMesh, PolygonMeshVertex>
	{
		[Fact]
		public void ContainsTypeFlagTest()
		{
			PolygonMesh polyline = new PolygonMesh();

			Assert.False(polyline.Flags.HasFlag(PolylineFlags.Polyline3D));
			Assert.True(polyline.Flags.HasFlag(PolylineFlags.PolygonMesh));
			Assert.False(polyline.Flags.HasFlag(PolylineFlags.PolyfaceMesh));
		}

		[Fact]
		public void ApplyTransformKeepsWorldCoordinateVertices()
		{
			var mesh = new PolygonMesh
			{
				Normal = XYZ.AxisY,
			};
			mesh.Vertices.Add(new PolygonMeshVertex(new XYZ(1, 2, 3)));

			mesh.ApplyTransform(Transform.CreateTranslation(new XYZ(5, 6, 7)));

			AssertUtils.AreEqual(new XYZ(6, 8, 10), mesh.Vertices[0].Location);
		}
	}
}

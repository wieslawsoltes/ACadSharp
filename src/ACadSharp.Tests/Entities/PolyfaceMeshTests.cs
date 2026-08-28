using ACadSharp.Entities;
using ACadSharp.Extensions;
using ACadSharp.Tables;
using ACadSharp.Tests.Common;
using CSMath;
using Xunit;

namespace ACadSharp.Tests.Entities;

public class PolyfaceMeshTests : CommonPolylineTests<PolyfaceMesh, VertexFaceMesh>
{
	[Fact]
	public void ContainsTypeFlagTest()
	{
		PolyfaceMesh polyline = new PolyfaceMesh();

		Assert.False(polyline.Flags.HasFlag(PolylineFlags.Polyline3D));
		Assert.False(polyline.Flags.HasFlag(PolylineFlags.PolygonMesh));
		Assert.True(polyline.Flags.HasFlag(PolylineFlags.PolyfaceMesh));
	}

	[Fact]
	public void ApplyTransformKeepsWorldCoordinateVertices()
	{
		var mesh = new PolyfaceMesh
		{
			Normal = XYZ.AxisY,
		};
		mesh.Vertices.Add(new VertexFaceMesh(new XYZ(1, 2, 3)));

		mesh.ApplyTransform(Transform.CreateTranslation(new XYZ(5, 6, 7)));

		AssertUtils.AreEqual(new XYZ(6, 8, 10), mesh.Vertices[0].Location);
	}

	[Fact]
	public void OnAddFaceShouldMatchProperties()
	{
		var polyline = new PolyfaceMesh();
		polyline.Layer = new Layer("test_layer");
		polyline.LineType = new LineType("test_ltype");

		var vertex = new VertexFaceRecord();

		polyline.Faces.Add(vertex);

		Assert.Equal(polyline.Layer.Name, vertex.Layer.Name);
		Assert.Equal(polyline.LineType.Name, vertex.LineType.Name);

		polyline = new PolyfaceMesh();
		polyline.Layer = new Layer("test_layer");
		polyline.LineType = new LineType("test_ltype");
		polyline.MatchVerticesEntityProperties = false;

		vertex = new VertexFaceRecord();

		polyline.Faces.Add(vertex);

		Assert.NotEqual(polyline.Layer.Name, vertex.Layer.Name);
		Assert.NotEqual(polyline.LineType.Name, vertex.LineType.Name);
	}

	[Fact]
	public void OnAddFaceShouldMatchPropertiesForClone()
	{
		var polyline = new PolyfaceMesh();
		polyline.Layer = new Layer("test_layer");
		polyline.LineType = new LineType("test_ltype");

		polyline = polyline.CloneTyped();

		var vertex = new VertexFaceRecord();
		polyline.Faces.Add(vertex);

		Assert.Equal(polyline.Layer.Name, vertex.Layer.Name);
		Assert.Equal(polyline.LineType.Name, vertex.LineType.Name);

		polyline = new PolyfaceMesh();
		polyline.Layer = new Layer("test_layer");
		polyline.LineType = new LineType("test_ltype");
		polyline.MatchVerticesEntityProperties = false;

		vertex = new VertexFaceRecord();

		polyline.Faces.Add(vertex);

		Assert.NotEqual(polyline.Layer.Name, vertex.Layer.Name);
		Assert.NotEqual(polyline.LineType.Name, vertex.LineType.Name);
	}
}

using ACadSharp.Entities;
using ACadSharp.IO;
using ACadSharp.Objects;
using CSMath;
using System.IO;
using System.Linq;
using Xunit;

namespace ACadSharp.Tests.Entities;

public class MultiLeaderTests
{
	[Fact]
	public void ApplyTransformUpdatesAllRetainedLeaderGeometryAndBreaks()
	{
		var source = new MultiLeader();
		source.ContextData.BasePoint = new XYZ(1, 2, 0);
		source.ContextData.ContentBasePoint = new XYZ(5, 2, 0);
		source.ContextData.BaseDirection = XYZ.AxisX;
		source.ContextData.BaseVertical = XYZ.AxisY;
		source.ContextData.Direction = XYZ.AxisX;
		source.ContextData.TextNormal = XYZ.AxisZ;
		source.ContextData.BlockContentNormal = XYZ.AxisZ;
		var root = new MultiLeaderObjectContextData.LeaderRoot
		{
			ConnectionPoint = new XYZ(4, 2, 0),
			Direction = XYZ.AxisX,
			LandingDistance = 2,
		};
		root.BreakStartEndPointsPairs.Add(
			new MultiLeaderObjectContextData.StartEndPointPair(
				new XYZ(4.5, 2, 0),
				new XYZ(5, 2, 0)));
		var line = new MultiLeaderObjectContextData.LeaderLine();
		line.Points.Add(new XYZ(1, 2, 0));
		line.Points.Add(new XYZ(3, 2, 0));
		line.StartEndPoints.Add(
			new MultiLeaderObjectContextData.StartEndPointPair(
				new XYZ(1.5, 2, 0),
				new XYZ(2, 2, 0)));
		root.Lines.Add(line);
		source.ContextData.LeaderRoots.Add(root);

		source.ApplyTransform(new Transform(
			new XYZ(10, 20, 3),
			new XYZ(2, 2, 2),
			XYZ.Zero));

		Assert.Equal(new XYZ(12, 24, 3), source.ContextData.BasePoint);
		Assert.Equal(new XYZ(18, 24, 3), root.ConnectionPoint);
		Assert.Equal(new XYZ(12, 24, 3), line.Points[0]);
		Assert.Equal(new XYZ(16, 24, 3), line.Points[1]);
		Assert.Equal(4, root.LandingDistance, 12);
		Assert.Equal(XYZ.AxisX, root.Direction);
		Assert.Equal(new XYZ(19, 24, 3), root.BreakStartEndPointsPairs[0].StartPoint);
		Assert.Equal(new XYZ(13, 24, 3), line.StartEndPoints[0].StartPoint);
	}

	[Fact]
	public void ApplyTransformComposesPersistedBlockContentMatrix()
	{
		var source = new MultiLeader();
		source.ContextData.HasContentsBlock = true;
		source.ContextData.BlockContentNormal = XYZ.AxisZ;
		source.ContextData.TextNormal = XYZ.AxisZ;
		source.ContextData.TransformationMatrix = Matrix4.Identity;

		source.ApplyTransform(Transform.CreateTranslation(new XYZ(7, 8, 9)));

		Assert.Equal(7, source.ContextData.TransformationMatrix.M30, 12);
		Assert.Equal(8, source.ContextData.TransformationMatrix.M31, 12);
		Assert.Equal(9, source.ContextData.TransformationMatrix.M32, 12);
		Assert.Equal(1, source.ContextData.TransformationMatrix.M33, 12);
	}

	[Fact]
	public void DwgRoundTripPreservesContextWithoutTextOrBlockContent()
	{
		var document = new CadDocument(ACadVersion.AC1032);
		var source = new MultiLeader
		{
			PropertyOverrideFlags = MultiLeaderPropertyOverrideFlags.ContentType,
			ContentType = LeaderContentType.None,
		};
		var root = new MultiLeaderObjectContextData.LeaderRoot
		{
			ConnectionPoint = new XYZ(4, 0, 0),
			Direction = XYZ.AxisX,
			LandingDistance = 2,
		};
		var line = new MultiLeaderObjectContextData.LeaderLine();
		line.Points.Add(XYZ.Zero);
		root.Lines.Add(line);
		source.ContextData.LeaderRoots.Add(root);
		document.Entities.Add(source);
		using var written = new MemoryStream();

		DwgWriter.Write(written, document);
		using var input = new MemoryStream(written.ToArray());
		CadDocument restored = DwgReader.Read(input);

		MultiLeader result = Assert.Single(restored.Entities.OfType<MultiLeader>());
		Assert.False(result.ContextData.HasTextContents);
		Assert.False(result.ContextData.HasContentsBlock);
		Assert.Single(result.ContextData.LeaderRoots);
		Assert.Single(result.ContextData.LeaderRoots[0].Lines);
		Assert.Equal(new XYZ(4, 0, 0), result.ContextData.LeaderRoots[0].ConnectionPoint);
	}
}

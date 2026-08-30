using ACadSharp.IO;
using ACadSharp.Tables;
using ACadSharp.Entities;
using CSMath;
using System.IO;
using System.Linq;
using Xunit;

namespace ACadSharp.Tests.Entities;

public class TableEntityTests
{
	[Fact]
	public void DefaultHorizontalDirectionMatchesWorldXAxis()
	{
		TableEntity source = new TableEntity();

		Assert.Equal(XYZ.AxisX, source.HorizontalDirection);
		Assert.Equal(XYZ.AxisZ, source.Normal);
	}

	[Fact]
	public void ApplyTransformUpdatesInsertStateAndNormalizesHorizontalDirection()
	{
		TableEntity source = new TableEntity
		{
			InsertPoint = new XYZ(1, 2, 0),
			HorizontalDirection = XYZ.AxisX,
			Normal = XYZ.AxisZ,
		};

		source.ApplyTransform(new Transform(
			new XYZ(10, 20, 3),
			new XYZ(2, 3, 4),
			new XYZ(0, 0, System.Math.PI / 2.0)));

		Assert.Equal(new XYZ(4, 22, 3), source.InsertPoint);
		Assert.Equal(0.0, source.HorizontalDirection.X, 12);
		Assert.Equal(1.0, source.HorizontalDirection.Y, 12);
		Assert.Equal(0.0, source.HorizontalDirection.Z, 12);
		Assert.Equal(1.0, source.HorizontalDirection.GetLength(), 12);
		Assert.Equal(1.0, source.Normal.GetLength(), 12);
	}

	[Fact]
	public void PersistedCacheAndOrientationRoundTripThroughDwg()
	{
		CadDocument document = new CadDocument(ACadVersion.AC1032);
		BlockRecord cache = new BlockRecord("*T1") { IsAnonymous = true };
		cache.Entities.Add(new Line(XYZ.Zero, new XYZ(4, 0, 0)));
		document.BlockRecords.Add(cache);
		TableEntity source = new TableEntity(cache)
		{
			InsertPoint = new XYZ(12, 8, 0),
			HorizontalDirection = XYZ.AxisY,
			Normal = XYZ.AxisZ,
		};
		document.Entities.Add(source);
		using MemoryStream written = new MemoryStream();

		DwgWriter.Write(
			written,
			document,
			new DwgWriterConfiguration { CloseStream = false });

		using MemoryStream input = new MemoryStream(written.ToArray());
		CadDocument restored = DwgReader.Read(input);
		TableEntity result = Assert.Single(restored.Entities.OfType<TableEntity>());

		Assert.Equal(source.InsertPoint, result.InsertPoint);
		Assert.Equal(source.HorizontalDirection, result.HorizontalDirection);
		Assert.Equal(source.Normal, result.Normal);
		Assert.NotNull(result.Block);
		Assert.StartsWith("*T", result.Block.Name);
		Assert.Single(result.Block.Entities.OfType<Line>());
	}
}

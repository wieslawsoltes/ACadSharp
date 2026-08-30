using ACadSharp.Entities;
using ACadSharp.IO;
using ACadSharp.Tables;
using CSMath;
using System.IO;
using System.Linq;
using Xunit;

namespace ACadSharp.Tests.Entities;

public class ToleranceTests
{
	[Fact]
	public void DefaultOrientationMatchesWorldPlane()
	{
		Tolerance source = new Tolerance();

		Assert.Equal(XYZ.AxisX, source.Direction);
		Assert.Equal(XYZ.AxisZ, source.Normal);
	}

	[Fact]
	public void ApplyTransformUpdatesInsertionAndKeepsDirectionNormalized()
	{
		Tolerance source = new Tolerance
		{
			InsertionPoint = new XYZ(1, 2, 0),
			Direction = XYZ.AxisX,
			Normal = XYZ.AxisZ,
		};

		source.ApplyTransform(new Transform(
			new XYZ(10, 20, 3),
			new XYZ(2, 3, 4),
			new XYZ(0, 0, System.Math.PI / 2.0)));

		Assert.Equal(new XYZ(4, 22, 3), source.InsertionPoint);
		Assert.Equal(0.0, source.Direction.X, 12);
		Assert.Equal(1.0, source.Direction.Y, 12);
		Assert.Equal(0.0, source.Direction.Z, 12);
		Assert.Equal(1.0, source.Direction.GetLength(), 12);
		Assert.Equal(1.0, source.Normal.GetLength(), 12);
	}

	[Theory]
	[InlineData(CadFileFormat.DXF)]
	[InlineData(CadFileFormat.DWG)]
	public void FeatureControlFrameRoundTrips(CadFileFormat format)
	{
		CadDocument document = new CadDocument(ACadVersion.AC1032);
		Tolerance source = new Tolerance
		{
			Text = "{\\Fgdt;b}%%v{\\Fgdt;n}0.10{\\Fgdt;m}%%vA",
			InsertionPoint = new XYZ(12, 8, 0),
			Direction = XYZ.AxisY,
			Normal = XYZ.AxisZ,
		};
		document.Entities.Add(source);
		using MemoryStream written = new MemoryStream();

		if (format == CadFileFormat.DXF)
		{
			DxfWriter.Write(written, document);
		}
		else
		{
			DwgWriter.Write(
				written,
				document,
				new DwgWriterConfiguration { CloseStream = false });
		}

		using MemoryStream input = new MemoryStream(written.ToArray());
		CadDocument restored = format == CadFileFormat.DXF
			? DxfReader.Read(input)
			: DwgReader.Read(input);
		Tolerance result = Assert.Single(restored.Entities.OfType<Tolerance>());

		Assert.Equal(source.Text, result.Text);
		Assert.Equal(source.InsertionPoint, result.InsertionPoint);
		Assert.Equal(source.Direction, result.Direction);
		Assert.Equal(source.Normal, result.Normal);
		Assert.Equal(DimensionStyle.DefaultName, result.Style.Name);
	}
}

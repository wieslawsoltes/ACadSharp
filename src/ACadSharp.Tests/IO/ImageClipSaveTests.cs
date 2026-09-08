using ACadSharp.Entities;
using ACadSharp.IO;
using ACadSharp.Objects;
using CSMath;
using System.IO;
using System.Linq;
using Xunit;

namespace ACadSharp.Tests.IO;

public class ImageClipSaveTests
{
	[Theory]
	[InlineData(false, false, false)]
	[InlineData(false, false, true)]
	[InlineData(false, true, false)]
	[InlineData(false, true, true)]
	[InlineData(true, false, false)]
	[InlineData(true, false, true)]
	[InlineData(true, true, false)]
	[InlineData(true, true, true)]
	public void PolygonClosureDoesNotAccumulateAcrossDxfReopens(bool binary, bool raster, bool closed)
	{
		XY[] corners = { new(-0.5, -0.5), new(35.5, -0.5), new(31.5, 11.5), new(3.5, 11.5) };
		CadWipeoutBase image = CreateImage(raster);
		image.ClipBoundaryVertices.AddRange(corners);
		if (closed) image.ClipBoundaryVertices.Add(corners[0]);
		var document = new CadDocument(ACadVersion.AC1032);
		document.Entities.Add(image);
		for (int cycle = 0; cycle < 4; cycle++)
		{
			XY[] before = image.ClipBoundaryVertices.ToArray();
			using var output = new MemoryStream();
			DxfWriter.Write(output, document, binary);
			Assert.Equal(before, image.ClipBoundaryVertices);
			using var input = new MemoryStream(output.ToArray());
			document = DxfReader.Read(input);
			image = Assert.Single(document.Entities.OfType<CadWipeoutBase>());
			Assert.Equal(5, image.ClipBoundaryVertices.Count);
			Assert.Equal(corners, image.ClipBoundaryVertices.Take(4));
			Assert.Equal(corners[0], image.ClipBoundaryVertices[4]);
			document.Entities.Add(new Line(new XYZ(cycle, 0, 0), new XYZ(cycle, 1, 0)));
		}
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public void RectangleKeepsItsTwoCornersAcrossDxfReopens(bool binary, bool raster)
	{
		XY[] corners = { new(-0.5, -0.5), new(35.5, 11.5) };
		CadWipeoutBase image = CreateImage(raster);
		image.ClipBoundaryVertices.AddRange(corners);
		var document = new CadDocument(ACadVersion.AC1032);
		document.Entities.Add(image);
		for (int cycle = 0; cycle < 3; cycle++)
		{
			using var output = new MemoryStream();
			DxfWriter.Write(output, document, binary);
			Assert.Equal(corners, image.ClipBoundaryVertices);
			using var input = new MemoryStream(output.ToArray());
			document = DxfReader.Read(input);
			image = Assert.Single(document.Entities.OfType<CadWipeoutBase>());
			Assert.Equal(ClipType.Rectangular, image.ClipType);
			Assert.Equal(corners, image.ClipBoundaryVertices);
		}
	}

	private static CadWipeoutBase CreateImage(bool raster)
	{
		CadWipeoutBase image = raster
			? new RasterImage(new ImageDefinition { Name = "clip", FileName = "clip.png", Size = new XY(36, 12) })
			: new Wipeout();
		image.Size = new XY(36, 12);
		image.UVector = XYZ.AxisX;
		image.VVector = XYZ.AxisY;
		image.ClippingState = true;
		image.Flags |= ImageDisplayFlags.UseClippingBoundary;
		return image;
	}
}

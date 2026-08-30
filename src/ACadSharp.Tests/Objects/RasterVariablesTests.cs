using ACadSharp.IO;
using ACadSharp.Entities;
using ACadSharp.Objects;
using ACadSharp.Types.Units;
using CSMath;
using System.IO;
using Xunit;

namespace ACadSharp.Tests.Objects;

public class RasterVariablesTests
{
	[Fact]
	public void DefaultFrameModeDisplaysAndPlots()
	{
		Assert.Equal(
			ImageFrameType.DisplayAndPlotted,
			new RasterVariables().FrameType);
	}

	[Fact]
	public void DxfRoundTripPreservesDisplayWithoutPlotAndUnits()
	{
		CadDocument source = CreateDocument();
		using var stream = new MemoryStream();

		DxfWriter.Write(stream, source);
		using var written = new MemoryStream(stream.ToArray());
		CadDocument restored = DxfReader.Read(written);

		RasterVariables variables = Assert.Single(
			restored.GetCadObjects<RasterVariables>());
		Assert.Equal(ImageFrameType.DisplayNoPlotted, variables.FrameType);
		Assert.Equal(ImageDisplayQuality.High, variables.DisplayQuality);
		Assert.Equal(ImageUnits.Centimeters, variables.Units);
	}

	[Fact]
	public void DwgRoundTripPreservesDisplayWithoutPlotAndUnits()
	{
		CadDocument source = CreateDocument();
		using var stream = new MemoryStream();

		DwgWriter.Write(stream, source);
		using var written = new MemoryStream(stream.ToArray());
		CadDocument restored = DwgReader.Read(written);

		RasterVariables variables = Assert.Single(
			restored.GetCadObjects<RasterVariables>());
		Assert.Equal(ImageFrameType.DisplayNoPlotted, variables.FrameType);
		Assert.Equal(ImageDisplayQuality.High, variables.DisplayQuality);
		Assert.Equal(ImageUnits.Centimeters, variables.Units);
	}

	[Fact]
	public void CompatibilityVisibilityMapsToLosslessStates()
	{
		var variables = new RasterVariables
		{
			FrameType = ImageFrameType.DisplayNoPlotted,
		};

#pragma warning disable CS0618
		Assert.True(variables.IsDisplayFrameShown);
		variables.IsDisplayFrameShown = false;
#pragma warning restore CS0618

		Assert.Equal(ImageFrameType.NoDisplayOrPlotted, variables.FrameType);
	}

	[Fact]
	public void DxfRasterImageRoundTripPreservesInsideClipMode()
	{
		var definition = new ImageDefinition
		{
			Name = "image",
			FileName = "image.png",
			Size = new XY(8, 6),
		};
		var image = new RasterImage(definition)
		{
			Size = new XY(8, 6),
			ClippingState = true,
			ClipMode = ClipMode.Inside,
			Flags = ImageDisplayFlags.ShowImage |
				ImageDisplayFlags.UseClippingBoundary,
		};
		image.ClipBoundaryVertices.AddRange([
			new XY(-0.5, -0.5),
			new XY(7.5, 5.5),
		]);
		var source = new CadDocument();
		source.Entities.Add(image);
		using var stream = new MemoryStream();

		DxfWriter.Write(stream, source);
		using var written = new MemoryStream(stream.ToArray());
		CadDocument restored = DxfReader.Read(written);

		RasterImage roundTripped = Assert.IsType<RasterImage>(
			Assert.Single(restored.Entities));
		Assert.Equal(ClipMode.Inside, roundTripped.ClipMode);
	}

	private static CadDocument CreateDocument()
	{
		var document = new CadDocument();
		var variables = new RasterVariables
		{
			Name = CadDictionary.AcadImageVars,
			FrameType = ImageFrameType.DisplayNoPlotted,
			DisplayQuality = ImageDisplayQuality.High,
			Units = ImageUnits.Centimeters,
		};
		document.RootDictionary.Add(variables);
		return document;
	}
}

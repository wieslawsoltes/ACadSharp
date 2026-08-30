using ACadSharp.IO;
using ACadSharp.Objects;
using CSMath;
using System.IO;
using Xunit;

namespace ACadSharp.Tests.IO.DXF;

public class DxfLayoutPlotSettingsTests
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void LayoutPlotSettingsRoundTripInTheirOwnSubclass(bool binary)
	{
		CadDocument document = new CadDocument(ACadVersion.AC1032);
		Layout model = document.Layouts[Layout.ModelLayoutName];
		model.PageName = "Model output";
		model.Flags = PlotFlags.ModelType |
			PlotFlags.PlotCentered |
			PlotFlags.PrintLineweights;
		model.PaperWidth = 210;
		model.PaperHeight = 297;
		model.PaperRotation = PlotRotation.Degrees90;
		model.NumeratorScale = 7;
		model.DenominatorScale = 13;
		model.ScaledFit = ScaledType._6;
		model.StandardScale = 1.0 / 96.0;
		model.PaperImageOriginX = 12.5;
		model.PaperImageOriginY = 34.5;
		using MemoryStream output = new MemoryStream();

		DxfWriter.Write(output, document, binary);
		using MemoryStream input = new MemoryStream(output.ToArray());
		CadDocument loaded = DxfReader.Read(input);
		Layout actual = loaded.Layouts[Layout.ModelLayoutName];

		Assert.Equal("Model", actual.Name);
		Assert.Equal("Model output", actual.PageName);
		Assert.Equal(model.Flags, actual.Flags);
		Assert.Equal(210, actual.PaperWidth);
		Assert.Equal(297, actual.PaperHeight);
		Assert.Equal(PlotRotation.Degrees90, actual.PaperRotation);
		Assert.Equal(7, actual.NumeratorScale);
		Assert.Equal(13, actual.DenominatorScale);
		Assert.Equal(ScaledType._6, actual.ScaledFit);
		Assert.Equal(1.0 / 96.0, actual.StandardScale, 15);
		Assert.Equal(new XY(12.5, 34.5), actual.PaperImageOrigin);
		Assert.Equal(12.5, actual.PaperImageOriginX);
		Assert.Equal(34.5, actual.PaperImageOriginY);
	}

	[Fact]
	public void PaperImageOriginRepresentationsStaySynchronized()
	{
		PlotSettings settings = new PlotSettings();

		settings.PaperImageOrigin = new XY(7.5, 8.5);

		Assert.Equal(7.5, settings.PaperImageOriginX);
		Assert.Equal(8.5, settings.PaperImageOriginY);

		settings.PaperImageOriginX = 17.5;
		settings.PaperImageOriginY = 18.5;

		Assert.Equal(new XY(17.5, 18.5), settings.PaperImageOrigin);
	}
}

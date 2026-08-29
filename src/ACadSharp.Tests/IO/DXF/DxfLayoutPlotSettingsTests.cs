using ACadSharp.IO;
using ACadSharp.Objects;
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
	}
}

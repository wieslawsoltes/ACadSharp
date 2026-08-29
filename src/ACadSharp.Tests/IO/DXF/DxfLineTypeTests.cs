using ACadSharp.IO;
using ACadSharp.Tables;
using System.IO;
using System.Linq;
using Xunit;

namespace ACadSharp.Tests.IO.DXF;

public class DxfLineTypeTests
{
	[Fact]
	public void ComplexSegmentRoundTripPreservesTextStyleReference()
	{
		CadDocument document = new CadDocument();
		TextStyle style = document.TextStyles[TextStyle.DefaultName];
		LineType lineType = new LineType("TEXT_LINE");
		lineType.AddSegment(new LineType.Segment { Length = 2.0 });
		lineType.AddSegment(new LineType.Segment
		{
			Length = -1.0,
			IsText = true,
			Text = "HW",
			Style = style,
			Scale = 0.25,
		});
		document.LineTypes.Add(lineType);

		using MemoryStream written = new MemoryStream();
		DxfWriter.Write(written, document);
		using MemoryStream source = new MemoryStream(written.ToArray());
		CadDocument loaded = DxfReader.Read(source);

		LineType.Segment segment = loaded.LineTypes[lineType.Name].Segments.Last();
		Assert.True(segment.IsText);
		Assert.Equal("HW", segment.Text);
		Assert.Same(loaded.TextStyles[TextStyle.DefaultName], segment.Style);
	}
}

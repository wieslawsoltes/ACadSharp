using ACadSharp.Entities;
using ACadSharp.IO;
using ACadSharp.Tables;
using System.IO;
using System.Linq;
using Xunit;

namespace ACadSharp.Tests.IO.DWG;

public class DwgShapeTests
{
	[Fact]
	public void RoundTripPreservesShapeNumberAndStyle()
	{
		CadDocument document = new CadDocument();
		document.Header.Version = ACadVersion.AC1027;
		TextStyle style = new TextStyle("symbols")
		{
			Flags = StyleFlags.IsShape,
			Filename = "symbols.shx",
		};
		document.TextStyles.Add(style);
		document.Entities.Add(new Shape(style)
		{
			ShapeName = "PUMP",
			ShapeNumber = 321,
			Size = 2.0,
		});

		using MemoryStream written = new MemoryStream();
		DwgWriter.Write(
			written,
			document,
			new DwgWriterConfiguration
			{
				CloseStream = false,
				WriteShapes = true,
			});
		using MemoryStream source = new MemoryStream(written.ToArray());
		CadDocument loaded = DwgReader.Read(source);
		Shape shape = loaded.Entities.OfType<Shape>().Single();

		Assert.Equal((ushort)321, shape.ShapeNumber);
		Assert.NotNull(shape.ShapeStyle);
		Assert.Equal("symbols.shx", shape.ShapeStyle.Filename);
	}
}

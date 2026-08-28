using ACadSharp.Entities;
using ACadSharp.IO;
using ACadSharp.Tables;
using CSMath;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace ACadSharp.Tests.IO.DXF;

public class DxfShapeTests
{
	[Fact]
	public void ReadPreservesShapeNameWithoutInventingAStyle()
	{
		string dxf = string.Join("\n",
			"0", "SECTION",
			"2", "ENTITIES",
			"0", "SHAPE",
			"5", "A1",
			"100", "AcDbEntity",
			"8", "0",
			"100", "AcDbShape",
			"10", "4",
			"20", "5",
			"30", "6",
			"40", "2.5",
			"2", "SWITCH",
			"0", "ENDSEC",
			"0", "EOF");

		using MemoryStream stream = new MemoryStream(Encoding.ASCII.GetBytes(dxf));
		CadDocument document = DxfReader.Read(stream);
		Shape shape = document.Entities.OfType<Shape>().Single();

		Assert.Equal("SWITCH", shape.ShapeName);
		Assert.Null(shape.ShapeStyle);
		Assert.Equal(new XYZ(4, 5, 6), shape.InsertionPoint);
		Assert.Equal(2.5, shape.Size);
	}

	[Fact]
	public void WriteRoundTripUsesShapeNameInsteadOfStyleName()
	{
		CadDocument document = new CadDocument();
		TextStyle style = new TextStyle("loaded-shapes")
		{
			Flags = StyleFlags.IsShape,
			Filename = "symbols.shx",
		};
		document.TextStyles.Add(style);
		document.Entities.Add(new Shape(style)
		{
			ShapeName = "VALVE",
			Size = 3.0,
			ShapeNumber = 42,
		});

		using MemoryStream written = new MemoryStream();
		DxfWriter.Write(
			written,
			document,
			configuration: new DxfWriterConfiguration { WriteShapes = true });
		string text = Encoding.UTF8.GetString(written.ToArray());
		Assert.Contains("VALVE", text);

		using MemoryStream source = new MemoryStream(written.ToArray());
		CadDocument loaded = DxfReader.Read(source);
		Shape shape = loaded.Entities.OfType<Shape>().Single();
		Assert.Equal("VALVE", shape.ShapeName);
		Assert.NotEqual(style.Name, shape.ShapeName);
	}
}

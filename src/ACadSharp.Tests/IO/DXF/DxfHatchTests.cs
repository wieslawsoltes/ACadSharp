using ACadSharp.Entities;
using ACadSharp.IO;
using CSMath;
using System.IO;
using System.Linq;
using Xunit;

namespace ACadSharp.Tests.IO.DXF;

public class DxfHatchTests
{
	[Fact]
	public void HatchStyleRoundTrips()
	{
		CadDocument document = new CadDocument();
		document.Header.Version = ACadVersion.AC1032;
		foreach (HatchStyleType style in new[] { HatchStyleType.Outer, HatchStyleType.Ignore })
		{
			Hatch hatch = new Hatch
			{
				IsSolid = true,
				Pattern = HatchPattern.Solid,
				PatternType = HatchPatternType.SolidFill,
				Style = style,
			};
			Hatch.BoundaryPath.Polyline polyline = new Hatch.BoundaryPath.Polyline
			{
				IsClosed = true,
			};
			polyline.Vertices.Add(new XYZ(0, 0, 0));
			polyline.Vertices.Add(new XYZ(10, 0, 0));
			polyline.Vertices.Add(new XYZ(10, 10, 0));
			polyline.Vertices.Add(new XYZ(0, 10, 0));
			Hatch.BoundaryPath path = new Hatch.BoundaryPath();
			path.Edges.Add(polyline);
			hatch.Paths.Add(path);
			document.Entities.Add(hatch);
		}

		using MemoryStream written = new MemoryStream();
		DxfWriter.Write(written, document);
		using MemoryStream source = new MemoryStream(written.ToArray());
		CadDocument loaded = DxfReader.Read(source);

		Assert.Equal(
			new[] { HatchStyleType.Outer, HatchStyleType.Ignore },
			loaded.Entities.OfType<Hatch>().Select(hatch => hatch.Style).ToArray());
	}
}

using ACadSharp.Entities;
using ACadSharp.IO;
using CSMath;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace ACadSharp.Tests.IO.DXF;

public class DxfHatchGradientTests
{
	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public void GradientMetadataAndColorsRoundTrip(bool binary, bool singleColor)
	{
		var document = new CadDocument();
		document.Header.Version = ACadVersion.AC1032;
		string[] names = { "LINEAR", "CYLINDER", "INVCYLINDER", "SPHERICAL", "INVSPHERICAL", "HEMISPHERICAL", "INVHEMISPHERICAL", "CURVED", "INVCURVED" };
		foreach (string name in names)
		{
			Hatch hatch = createHatch();
			hatch.GradientColor = new HatchGradientPattern(name)
			{
				Enabled = true,
				Angle = 0.4,
				Shift = 0.25,
				IsSingleColorGradient = singleColor,
				ColorTint = 0.75,
			};
			hatch.GradientColor.Colors.Add(new GradientColor { Value = 0, Color = new Color(23, 117, 241) });
			hatch.GradientColor.Colors.Add(new GradientColor { Value = 1, Color = new Color((short)3) });
			document.Entities.Add(hatch);
		}

		using var output = new MemoryStream();
		DxfWriter.Write(output, document, binary);
		using var input = new MemoryStream(output.ToArray());
		Hatch[] restored = DxfReader.Read(input).Entities.OfType<Hatch>().ToArray();
		Assert.Equal(names.Length, restored.Length);
		for (int i = 0; i < restored.Length; i++)
		{
			HatchGradientPattern gradient = restored[i].GradientColor;
			Assert.True(gradient.Enabled);
			Assert.Equal(names[i], gradient.Name);
			Assert.Equal(0.4, gradient.Angle, 10);
			Assert.Equal(0.25, gradient.Shift, 10);
			Assert.Equal(singleColor, gradient.IsSingleColorGradient);
			Assert.Equal(0.75, gradient.ColorTint, 10);
			Assert.Equal(2, gradient.Colors.Count);
			Assert.Equal(0, gradient.Colors[0].Value);
			Assert.Equal(1, gradient.Colors[1].Value);
			Assert.Equal(new Color(23, 117, 241), gradient.Colors[0].Color);
			Assert.Equal(new Color((short)3), gradient.Colors[1].Color);
		}
	}

	[Fact]
	public void OlderDxfVersionRejectsGradientInsteadOfDroppingIt()
	{
		var document = new CadDocument();
		document.Header.Version = ACadVersion.AC1015;
		Hatch hatch = createHatch();
		hatch.GradientColor.Enabled = true;
		hatch.GradientColor.Colors.Add(new GradientColor { Value = 0, Color = new Color((short)1) });
		hatch.GradientColor.Colors.Add(new GradientColor { Value = 1, Color = new Color((short)5) });
		document.Entities.Add(hatch);
		using var output = new MemoryStream();
		Assert.Throws<NotSupportedException>(() => DxfWriter.Write(output, document));
	}

	private static Hatch createHatch()
	{
		var hatch = new Hatch { IsSolid = true };
		var outline = new Hatch.BoundaryPath.Polyline { IsClosed = true };
		outline.Vertices.AddRange(new[] { new XYZ(0, 0, 0), new XYZ(10, 0, 0), new XYZ(10, 10, 0), new XYZ(0, 10, 0) });
		var boundary = new Hatch.BoundaryPath();
		boundary.Edges.Add(outline);
		hatch.Paths.Add(boundary);
		return hatch;
	}
}

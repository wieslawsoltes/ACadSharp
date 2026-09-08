using ACadSharp.Entities;
using ACadSharp.IO;
using CSMath;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ACadSharp.Tests.IO.DWG;

public class DwgSplineRepresentationTests
{
	[Theory]
	[InlineData(ACadVersion.AC1024)]
	[InlineData(ACadVersion.AC1027)]
	[InlineData(ACadVersion.AC1032)]
	public void ExplicitControlsWinOverInconsistentFitMetadataWithoutChangingSource(ACadVersion version)
	{
		var document = new CadDocument(version);
		var spline = new Spline
		{
			Degree = 3,
			KnotParametrization = KnotParametrization.Chord,
			Flags1 = SplineFlags1.MethodFitPoints | SplineFlags1.UseKnotParameter,
			StartTangent = XYZ.AxisX,
			EndTangent = XYZ.AxisY,
		};
		spline.ControlPoints.AddRange(new[] { XYZ.Zero, new XYZ(3, 6, 1), new XYZ(9, -3, 3), new XYZ(12, 3, 4) });
		spline.Knots.AddRange(new[] { 0d, 0, 0, 0, 1, 1, 1, 1 });
		spline.FitPoints.AddRange(new[] { new XYZ(100, 100, 0), new XYZ(200, 100, 0) });
		document.Entities.Add(spline);
		var messages = new List<string>();
		using var output = new MemoryStream();
		DwgWriter.Write(output, document, notification: (_, args) => messages.Add(args.Message));
		Assert.Equal(SplineFlags1.MethodFitPoints | SplineFlags1.UseKnotParameter, spline.Flags1);
		Assert.Equal(KnotParametrization.Chord, spline.KnotParametrization);
		Assert.Equal(2, spline.FitPoints.Count);
		Assert.Contains(messages, message => message.Contains("fit-point authoring data"));
		using var input = new MemoryStream(output.ToArray());
		Spline restored = Assert.IsType<Spline>(Assert.Single(DwgReader.Read(input).Entities));
		Assert.Equal(spline.ControlPoints, restored.ControlPoints);
		Assert.Equal(spline.Knots, restored.Knots);
		Assert.Empty(restored.FitPoints);
		Assert.False(restored.Flags1.HasFlag(SplineFlags1.MethodFitPoints));
	}

	[Theory]
	[InlineData(SplineFlags1.None)]
	[InlineData(SplineFlags1.UseKnotParameter)]
	[InlineData(SplineFlags1.MethodFitPoints)]
	public void FitScenarioEncodingDoesNotMutateSourceFlags(SplineFlags1 flags)
	{
		var document = new CadDocument(ACadVersion.AC1032);
		var spline = new Spline
		{
			Degree = 3, Flags1 = flags,
			KnotParametrization = KnotParametrization.Uniform,
			StartTangent = new XYZ(9, 18, 0), EndTangent = new XYZ(9, 18, 0),
		};
		spline.FitPoints.AddRange(new[] { XYZ.Zero, new XYZ(12, 3, 0) });
		document.Entities.Add(spline);
		using var output = new MemoryStream();
		DwgWriter.Write(output, document);
		Assert.Equal(flags, spline.Flags1);
		Assert.Empty(spline.ControlPoints);
		using var input = new MemoryStream(output.ToArray());
		Spline restored = Assert.IsType<Spline>(Assert.Single(DwgReader.Read(input).Entities));
		Assert.Equal(spline.FitPoints, restored.FitPoints);
		Assert.Equal(KnotParametrization.Uniform, restored.KnotParametrization);
		Assert.Equal(spline.StartTangent, restored.StartTangent);
		Assert.Equal(spline.EndTangent, restored.EndTangent);
	}
}

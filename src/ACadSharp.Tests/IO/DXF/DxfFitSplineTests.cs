using ACadSharp.Entities;
using ACadSharp.IO;
using CSMath;
using System;
using System.IO;
using Xunit;

namespace ACadSharp.Tests.IO.DXF;

public class DxfFitSplineTests
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void ExactUniformFitCubicWritesControlsAndRetainsAuthoredFitData(bool binary)
	{
		var document = new CadDocument(ACadVersion.AC1032);
		Spline spline = createSpline();
		document.Entities.Add(spline);
		using var output = new MemoryStream();
		DxfWriter.Write(output, document, binary, new DxfWriterConfiguration { CloseStream = false });
		Assert.Empty(spline.ControlPoints);
		Assert.Empty(spline.Knots);
		Assert.Equal(KnotParametrization.Uniform, spline.KnotParametrization);
		Assert.Equal(SplineFlags1.MethodFitPoints | SplineFlags1.UseKnotParameter, spline.Flags1);
		using var input = new MemoryStream(output.ToArray());
		Spline restored = Assert.IsType<Spline>(Assert.Single(DxfReader.Read(input).Entities));
		Assert.Equal(new[] { XYZ.Zero, new XYZ(3, 6, 1), new XYZ(9, -3, 3), new XYZ(12, 3, 4) }, restored.ControlPoints);
		Assert.Equal(new[] { 0d, 0, 0, 0, 1, 1, 1, 1 }, restored.Knots);
		Assert.Equal(spline.FitPoints, restored.FitPoints);
		Assert.Equal(spline.StartTangent, restored.StartTangent);
		Assert.Equal(spline.EndTangent, restored.EndTangent);
		Assert.Equal(spline.FitTolerance, restored.FitTolerance);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	[InlineData(4)]
	[InlineData(5)]
	[InlineData(6)]
	[InlineData(7)]
	[InlineData(8)]
	[InlineData(9)]
	[InlineData(10)]
	public void ExactFitQueryRejectsOtherSystemsAndNonfiniteResults(int invalid)
	{
		Spline spline = createSpline();
		switch (invalid)
		{
			case 0: spline.Degree = 2; break;
			case 1: spline.KnotParametrization = KnotParametrization.Chord; break;
			case 2: spline.IsClosed = true; break;
			case 3: spline.FitPoints.Add(XYZ.AxisX); break;
			case 4: spline.StartTangent = XYZ.Zero; break;
			case 5: spline.EndTangent = new XYZ(double.NaN, 1, 0); break;
			case 6: spline.FitPoints[0] = new XYZ(double.PositiveInfinity, 0, 0); break;
			case 7: spline.Weights.Add(1); break;
			case 8: spline.Knots.Add(0); break;
			case 9: spline.ControlPoints.Add(XYZ.Zero); break;
			case 10:
				spline.FitPoints[0] = new XYZ(double.MaxValue, 0, 0);
				spline.StartTangent = new XYZ(double.MaxValue, 0, 0);
				break;
		}
		Assert.False(spline.TryGetFitPointCubicBezier(out XYZ a, out XYZ b, out XYZ c, out XYZ d));
		Assert.Equal(XYZ.Zero, a);
		Assert.Equal(XYZ.Zero, b);
		Assert.Equal(XYZ.Zero, c);
		Assert.Equal(XYZ.Zero, d);
	}

	private static Spline createSpline()
	{
		var spline = new Spline
		{
			Degree = 3,
			KnotParametrization = KnotParametrization.Uniform,
			Flags1 = SplineFlags1.MethodFitPoints | SplineFlags1.UseKnotParameter,
			StartTangent = new XYZ(9, 18, 3),
			EndTangent = new XYZ(9, 18, 3),
		};
		spline.FitPoints.AddRange(new[] { XYZ.Zero, new XYZ(12, 3, 4) });
		return spline;
	}
}

using ACadSharp.Entities;
using ACadSharp.IO;
using ACadSharp.Tables;
using ACadSharp.Tests.Common;
using CSMath;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace ACadSharp.Tests.IO.DXF
{
	public class DxfWriterTests : IOTestsBase
	{
		public DxfWriterTests(ITestOutputHelper output) : base(output) { }

		[Theory]
		[MemberData(nameof(Versions))]
		public void WriteEmptyAsciiTest(ACadVersion version)
		{
			if (version < ACadVersion.AC1015)
				// Not implemented
				return;

			CadDocument doc = new CadDocument();
			doc.Header.Version = version;

			string path = Path.Combine(TestVariables.OutputSamplesFolder, $"out_empty_sample_{version}_ascii.dxf");

			using (var wr = new DxfWriter(path, doc, false))
			{
				wr.OnNotification += this.onNotification;
				wr.Write();
			}

			this._output.WriteLine("Writer successful");

			using (var re = new DxfReader(path, this.onNotification))
			{
				CadDocument readed = re.Read();
			}
		}

		[Theory]
		[MemberData(nameof(Versions))]
		public void WriteEmptyBinaryTest(ACadVersion version)
		{
			if (version < ACadVersion.AC1015)
				// Not implemented
				return;

			CadDocument doc = new CadDocument();
			doc.Header.Version = version;

			string path = Path.Combine(TestVariables.OutputSamplesFolder, $"out_empty_sample_{version}_binary.dxf");

			using (var wr = new DxfWriter(path, doc, true))
			{
				wr.OnNotification += this.onNotification;
				wr.Write();
			}

			this._output.WriteLine("Writer successful");

			using (var re = new DxfReader(path, this.onNotification))
			{
				CadDocument readed = re.Read();
			}
		}

		[Theory]
		[MemberData(nameof(Versions))]
		public void WriteDocumentWithEntitiesTest(ACadVersion version)
		{
			if (version < ACadVersion.AC1015)
				// Not implemented
				return;

			CadDocument doc = new CadDocument();
			doc.Header.Version = version;

			List<Entity> entities = new List<Entity>
			{
				EntityFactory.Create<Point>(),
				EntityFactory.Create<Line>(),
				EntityFactory.Create<Polyline2D>(),
				EntityFactory.Create<Polyline3D>(),
				EntityFactory.Create<Line>(),
				EntityFactory.Create<Arc>(),
				EntityFactory.Create<LwPolyline>(),
			};


			doc.Entities.AddRange(entities);

			string path = Path.Combine(TestVariables.OutputSamplesFolder, $"out_sample_{version}_ascii.dxf");

			using (var wr = new DxfWriter(path, doc, false))
			{
				wr.OnNotification += this.onNotification;
				wr.Write();
			}
		}

		[Theory]
		[InlineData(ACadVersion.AC1021)]
		[InlineData(ACadVersion.AC1032)]
		public void ActiveViewportGridDisplaySettingsRoundTrip(ACadVersion version)
		{
			CadDocument doc = new CadDocument(version);
			VPort active = doc.VPorts[VPort.DefaultName];
			active.ShowGrid = false;
			active.GridSpacing = new XY(2.5, 7.25);
			active.GridFlags = GridFlags._1 | GridFlags._2 | GridFlags._3;
			active.MinorGridLinesPerMajorGridLine = 17;
			using MemoryStream stream = new MemoryStream();

			DxfWriter.Write(stream, doc, false);
			using MemoryStream input = new MemoryStream(stream.ToArray());
			CadDocument read = DxfReader.Read(input);
			VPort restored = read.VPorts[VPort.DefaultName];

			Assert.False(restored.ShowGrid);
			Assert.Equal(new XY(2.5, 7.25), restored.GridSpacing);
			Assert.Equal(
				GridFlags._1 | GridFlags._2 | GridFlags._3,
				restored.GridFlags);
			Assert.Equal(17, restored.MinorGridLinesPerMajorGridLine);
		}

		[Theory]
		[InlineData(ACadVersion.AC1015)]
		[InlineData(ACadVersion.AC1021)]
		[InlineData(ACadVersion.AC1032)]
		public void ActiveViewportIsometricSnapSettingsRoundTrip(ACadVersion version)
		{
			CadDocument doc = new CadDocument(version);
			VPort active = doc.VPorts[VPort.DefaultName];
			active.IsometricSnap = true;
			active.SnapIsoPair = 2;
			using MemoryStream stream = new MemoryStream();

			DxfWriter.Write(stream, doc, false);
			using MemoryStream input = new MemoryStream(stream.ToArray());
			CadDocument read = DxfReader.Read(input);
			VPort restored = read.VPorts[VPort.DefaultName];

			Assert.True(restored.IsometricSnap);
			Assert.Equal(2, restored.SnapIsoPair);
		}

		[Theory]
		[InlineData(ACadVersion.AC1015)]
		[InlineData(ACadVersion.AC1021)]
		[InlineData(ACadVersion.AC1032)]
		public void Polyline2DDefaultsAndThicknessRoundTrip(ACadVersion version)
		{
			CadDocument doc = new CadDocument(version);
			var polyline = new Polyline2D
			{
				StartWidth = 2.5,
				EndWidth = 3.5,
				Thickness = 4.5,
			};
			polyline.Vertices.Add(new Vertex2D(XYZ.Zero));
			polyline.Vertices.Add(new Vertex2D(new XYZ(10, 0, 0)));
			doc.Entities.Add(polyline);
			using MemoryStream stream = new MemoryStream();

			DxfWriter.Write(stream, doc, false);
			using MemoryStream input = new MemoryStream(stream.ToArray());
			CadDocument read = DxfReader.Read(input);
			Polyline2D restored = Assert.IsType<Polyline2D>(Assert.Single(read.Entities));

			Assert.Equal(2.5, restored.StartWidth);
			Assert.Equal(3.5, restored.EndWidth);
			Assert.Equal(4.5, restored.Thickness);
		}

		[Fact]
		public void LightweightWidthPresenceDistinguishesConstantAndExplicitZero()
		{
			CadDocument doc = new CadDocument(ACadVersion.AC1032);
			var constant = new LwPolyline { ConstantWidth = 3.0 };
			var inherited = new LwPolyline.Vertex(0, 0)
			{
				StartWidth = 7.0,
				EndWidth = 8.0,
			};
			inherited.ClearStartWidth();
			inherited.ClearEndWidth();
			constant.Vertices.Add(inherited);
			constant.Vertices.Add(new LwPolyline.Vertex(10, 0));
			var explicitZero = new LwPolyline { ConstantWidth = 4.0 };
			explicitZero.Vertices.Add(new LwPolyline.Vertex(0, 2)
			{
				StartWidth = 0.0,
				EndWidth = 2.0,
			});
			explicitZero.Vertices.Add(new LwPolyline.Vertex(10, 2));
			doc.Entities.Add(constant);
			doc.Entities.Add(explicitZero);
			using MemoryStream stream = new MemoryStream();

			DxfWriter.Write(stream, doc, false);
			using MemoryStream input = new MemoryStream(stream.ToArray());
			CadDocument read = DxfReader.Read(input);
			LwPolyline[] restored = read.Entities.OfType<LwPolyline>().ToArray();

			Assert.Equal(2, restored.Length);
			Assert.False(restored[0].Vertices[0].HasStartWidth);
			Assert.False(restored[0].Vertices[0].HasEndWidth);
			Assert.True(restored[1].Vertices[0].HasStartWidth);
			Assert.True(restored[1].Vertices[0].HasEndWidth);
			Assert.Equal(0.0, restored[1].Vertices[0].StartWidth);
			Assert.Equal(2.0, restored[1].Vertices[0].EndWidth);
		}

		[Fact]
		public void LegacyWidthPresencePreservesOmittedDefaultAndTaperToZero()
		{
			CadDocument doc = new CadDocument(ACadVersion.AC1032);
			var polyline = new Polyline2D
			{
				StartWidth = 3.0,
				EndWidth = 3.0,
			};
			polyline.Vertices.Add(new Vertex2D(XYZ.Zero)
			{
				StartWidth = 0.0,
				EndWidth = 2.0,
			});
			var inherited = new Vertex2D(new XYZ(10, 0, 0))
			{
				StartWidth = 7.0,
				EndWidth = 8.0,
			};
			inherited.ClearStartWidth();
			inherited.ClearEndWidth();
			polyline.Vertices.Add(inherited);
			doc.Entities.Add(polyline);
			using MemoryStream stream = new MemoryStream();

			DxfWriter.Write(stream, doc, false);
			using MemoryStream input = new MemoryStream(stream.ToArray());
			CadDocument read = DxfReader.Read(input);
			Polyline2D restored = Assert.IsType<Polyline2D>(Assert.Single(read.Entities));

			Assert.True(restored.Vertices[0].HasStartWidth);
			Assert.True(restored.Vertices[0].HasEndWidth);
			Assert.Equal(0.0, restored.Vertices[0].StartWidth);
			Assert.Equal(2.0, restored.Vertices[0].EndWidth);
			Assert.False(restored.Vertices[1].HasStartWidth);
			Assert.False(restored.Vertices[1].HasEndWidth);
		}

		[Theory]
		[InlineData(ACadVersion.AC1015)]
		[InlineData(ACadVersion.AC1021)]
		[InlineData(ACadVersion.AC1032)]
		public void OrthoModeRoundTrips(ACadVersion version)
		{
			CadDocument doc = new CadDocument(version);
			doc.Header.OrthoMode = true;
			using MemoryStream stream = new MemoryStream();

			DxfWriter.Write(stream, doc, false);
			using MemoryStream input = new MemoryStream(stream.ToArray());
			CadDocument read = DxfReader.Read(input);

			Assert.True(read.Header.OrthoMode);
		}

		[Theory]
		[InlineData(ACadVersion.AC1015)]
		[InlineData(ACadVersion.AC1021)]
		[InlineData(ACadVersion.AC1032)]
		public void PolylineWidthDefaultRoundTrips(ACadVersion version)
		{
			CadDocument doc = new CadDocument(version);
			doc.Header.PolylineWidthDefault = 2.75;
			using MemoryStream stream = new MemoryStream();

			DxfWriter.Write(stream, doc, false);
			using MemoryStream input = new MemoryStream(stream.ToArray());
			CadDocument read = DxfReader.Read(input);

			Assert.Equal(2.75, read.Header.PolylineWidthDefault);
		}
	}
}

using ACadSharp.Entities;
using ACadSharp.IO;
using CSMath;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace ACadSharp.Tests.Entities;

public class LeaderTests
{
	[Theory]
	[InlineData(CadFileFormat.DXF)]
	[InlineData(CadFileFormat.DWG)]
	public void AssociatedMTextRoundTrips(CadFileFormat format)
	{
		CadDocument document = new CadDocument();
		document.Header.Version = ACadVersion.AC1032;
		MText annotation = new MText
		{
			InsertPoint = new XYZ(12, 8, 0),
			Value = "Pump A",
		};
		Leader leader = new Leader
		{
			AnnotationOffset = new XYZ(1, 2, 0),
			ArrowHeadEnabled = true,
		};
		leader.Vertices.Add(XYZ.Zero);
		leader.Vertices.Add(new XYZ(10, 0, 0));
		document.Entities.Add(annotation);
		document.Entities.Add(leader);
		leader.AttachAnnotation(annotation);

		using MemoryStream written = new MemoryStream();
		if (format == CadFileFormat.DXF)
		{
			DxfWriter.Write(written, document);
		}
		else
		{
			DwgWriter.Write(
				written,
				document,
				new DwgWriterConfiguration { CloseStream = false });
		}

		using MemoryStream source = new MemoryStream(written.ToArray());
		CadDocument loaded = format == CadFileFormat.DXF
			? DxfReader.Read(source)
			: DwgReader.Read(source);
		Leader loadedLeader = loaded.Entities.OfType<Leader>().Single();
		MText loadedAnnotation = loaded.Entities.OfType<MText>().Single();

		Assert.Same(loadedAnnotation, loadedLeader.AssociatedAnnotation);
		Assert.Equal(LeaderCreationType.CreatedWithTextAnnotation, loadedLeader.CreationType);
		Assert.Equal(new XYZ(1, 2, 0), loadedLeader.AnnotationOffset);
	}

	[Fact]
	public void AttachAndDetachKeepCreationTypeSynchronized()
	{
		Leader leader = new Leader();

		leader.AttachAnnotation(new MText());
		Assert.Equal(LeaderCreationType.CreatedWithTextAnnotation, leader.CreationType);
		leader.AttachAnnotation(new Tolerance());
		Assert.Equal(LeaderCreationType.CreatedWithToleranceAnnotation, leader.CreationType);
		leader.AttachAnnotation(new Insert());
		Assert.Equal(LeaderCreationType.CreatedWithBlockReferenceAnnotation, leader.CreationType);

		leader.DetachAnnotation();

		Assert.Null(leader.AssociatedAnnotation);
		Assert.Equal(LeaderCreationType.CreatedWithoutAnnotation, leader.CreationType);
	}

	[Fact]
	public void AttachRejectsUnsupportedAnnotationAndDifferentDocument()
	{
		Leader leader = new Leader();

		Assert.Throws<ArgumentNullException>(() => leader.AttachAnnotation(null));
		Assert.Throws<ArgumentException>(() => leader.AttachAnnotation(new Line()));

		CadDocument first = new CadDocument();
		CadDocument second = new CadDocument();
		first.Entities.Add(leader);
		MText annotation = new MText();
		second.Entities.Add(annotation);

		Assert.Throws<InvalidOperationException>(() => leader.AttachAnnotation(annotation));
	}
}

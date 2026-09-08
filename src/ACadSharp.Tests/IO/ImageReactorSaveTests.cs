using ACadSharp.Entities;
using ACadSharp.IO;
using ACadSharp.Objects;
using CSMath;
using System.IO;
using System.Linq;
using Xunit;

namespace ACadSharp.Tests.IO;

public class ImageReactorSaveTests
{
	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	public void RepeatedSavesKeepImageReactorIdentityAndNoDanglingDefinitionReferences(int format)
	{
		var document = new CadDocument(ACadVersion.AC1032);
		var image = new RasterImage
		{
			Definition = new ImageDefinition { Name = "image", FileName = "image.png" },
			Size = new XY(1, 1),
		};
		image.ClipBoundaryVertices.Add(new XY(-0.5, -0.5));
		image.ClipBoundaryVertices.Add(new XY(0.5, 0.5));
		document.Entities.Add(image);
		document.UpdateImageReactors();
		ImageDefinitionReactor reactor = Assert.Single(image.Definition.Reactors.OfType<ImageDefinitionReactor>());
		ulong handle = reactor.Handle;
		for (int i = 0; i < 3; i++)
		{
			using var output = new MemoryStream();
			if (format == 2) DwgWriter.Write(output, document);
			else DxfWriter.Write(output, document, format == 1);
			Assert.Same(reactor, Assert.Single(image.Definition.Reactors.OfType<ImageDefinitionReactor>()));
			Assert.Equal(handle, reactor.Handle);
			Assert.Same(image, reactor.Image);
			Assert.Same(image, reactor.Owner);
			Assert.Same(document, reactor.Document);
			using var input = new MemoryStream(output.ToArray());
			CadDocument reopened = format == 2 ? DwgReader.Read(input) : DxfReader.Read(input);
			RasterImage restored = Assert.Single(reopened.Entities.OfType<RasterImage>());
			ImageDefinitionReactor stored = Assert.Single(restored.Definition.Reactors.OfType<ImageDefinitionReactor>());
				Assert.Equal(handle, stored.Handle);
				Assert.Same(restored, stored.Owner);
				reopened.UpdateImageReactors();
				Assert.Same(stored, Assert.Single(restored.Definition.Reactors.OfType<ImageDefinitionReactor>()));
				Assert.Equal(handle, stored.Handle);
				Assert.Same(reopened, stored.Document);
			document.Entities.Add(new Line(XYZ.Zero, XYZ.AxisX));
		}
	}

	[Fact]
	public void UpdateRepairsDeletedCopiedAndRetargetedImagesWithoutRemovingOtherReactors()
	{
		var document = new CadDocument();
		var first = new ImageDefinition { Name = "first" };
		var second = new ImageDefinition { Name = "second" };
		var image = new RasterImage { Definition = first };
		document.Entities.Add(image);
		var unrelated = new Line(XYZ.Zero, XYZ.AxisX);
		document.Entities.Add(unrelated);
		first.AddReactor(unrelated);
		document.UpdateImageReactors();
		ImageDefinitionReactor original = Assert.Single(first.Reactors.OfType<ImageDefinitionReactor>());
		var copy = (RasterImage)image.Clone();
		document.Entities.Add(copy);
		document.UpdateImageReactors();
		Assert.Equal(2, first.Reactors.OfType<ImageDefinitionReactor>().Count());
		Assert.Same(image, original.Image);
		Assert.Same(image, original.Owner);
		copy.Definition = second;
		document.UpdateImageReactors();
		Assert.Same(original, Assert.Single(first.Reactors.OfType<ImageDefinitionReactor>()));
		Assert.Same(copy, Assert.Single(second.Reactors.OfType<ImageDefinitionReactor>()).Image);
		document.Entities.Remove(image);
		document.UpdateImageReactors();
		Assert.Empty(first.Reactors.OfType<ImageDefinitionReactor>());
		Assert.Contains(unrelated, first.Reactors);
		Assert.Null(original.Document);
		Assert.Single(document.GetCadObjects<ImageDefinitionReactor>());
	}
}

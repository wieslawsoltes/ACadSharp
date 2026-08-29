using ACadSharp.Entities;
using ACadSharp.IO;
using ACadSharp.Tables;
using System.IO;
using System.Linq;
using Xunit;

namespace ACadSharp.Tests.IO.DXF;

public class DxfAttributePositionLockTests
{
	[Theory]
	[InlineData(ACadVersion.AC1021)]
	[InlineData(ACadVersion.AC1024)]
	[InlineData(ACadVersion.AC1032)]
	public void PositionLockRoundTripsForDefinitionAndReference(
		ACadVersion version)
	{
		var document = new CadDocument();
		document.Header.Version = version;
		var block = new BlockRecord("LOCKED_ATTRIBUTE_BLOCK");
		var definition = new AttributeDefinition
		{
			Tag = "LOCKED",
			Value = "DEFAULT",
			IsLocked = true,
		};
		block.Entities.Add(definition);
		var insert = new Insert(block);
		AttributeEntity reference = insert.Attributes.Single();
		reference.IsLocked = true;
		document.Entities.Add(insert);
		using var stream = new MemoryStream();

		DxfWriter.Write(stream, document);
		using var input = new MemoryStream(stream.ToArray());
		CadDocument loaded = DxfReader.Read(input);

		AttributeDefinition loadedDefinition = loaded.BlockRecords[
				"LOCKED_ATTRIBUTE_BLOCK"]
			.AttributeDefinitions
			.Single();
		AttributeEntity loadedReference = loaded.Entities
			.OfType<Insert>()
			.Single()
			.Attributes
			.Single();
		Assert.True(loadedDefinition.IsLocked);
		Assert.True(loadedReference.IsLocked);
	}
}

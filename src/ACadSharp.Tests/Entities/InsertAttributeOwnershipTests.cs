using ACadSharp.Entities;
using ACadSharp.Tables;
using Xunit;

namespace ACadSharp.Tests.Entities;

public class InsertAttributeOwnershipTests
{
	[Fact]
	public void ConstructorCreatesReferencesOnlyForVariableDefinitions()
	{
		var block = new BlockRecord("ATTRIBUTE_OWNERSHIP");
		block.Entities.Add(new AttributeDefinition
		{
			Tag = "FIXED",
			Value = "CONSTANT",
			Flags = AttributeFlags.Constant,
		});
		block.Entities.Add(new AttributeDefinition
		{
			Tag = "FIXED_MULTILINE",
			Value = "CONSTANT MULTILINE",
			AttributeType = AttributeType.ConstantMultiLine,
			MText = new MText("CONSTANT MULTILINE"),
		});
		block.Entities.Add(new AttributeDefinition
		{
			Tag = "VARIABLE",
			Value = "ASSIGNED",
		});

		var insert = new Insert(block);

		AttributeEntity attribute = Assert.Single(insert.Attributes);
		Assert.Equal("VARIABLE", attribute.Tag);
		Assert.Equal("ASSIGNED", attribute.Value);
	}

	[Fact]
	public void UpdateAttributesTracksConstantTransitionsAndDuplicateTags()
	{
		var block = new BlockRecord("ATTRIBUTE_TRANSITIONS");
		var first = new AttributeDefinition
		{
			Tag = "DUPLICATE",
			Value = "FIRST",
		};
		var second = new AttributeDefinition
		{
			Tag = "DUPLICATE",
			Value = "SECOND",
		};
		block.Entities.Add(first);
		block.Entities.Add(second);
		var insert = new Insert(block);
		Assert.Equal(2, insert.Attributes.Count);

		first.Flags = AttributeFlags.Constant;
		insert.UpdateAttributes();

		Assert.Single(insert.Attributes);
		Assert.All(
			insert.Attributes,
			attribute => Assert.Equal("DUPLICATE", attribute.Tag));

		first.Flags = AttributeFlags.None;
		insert.UpdateAttributes();

		Assert.Equal(2, insert.Attributes.Count);
		Assert.All(
			insert.Attributes,
			attribute => Assert.Equal("DUPLICATE", attribute.Tag));
	}
}

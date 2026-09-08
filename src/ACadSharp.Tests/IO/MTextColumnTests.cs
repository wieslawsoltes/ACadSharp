using ACadSharp.Entities;
using ACadSharp.IO;
using System.IO;
using System.Linq;
using Xunit;

namespace ACadSharp.Tests.IO;

public class MTextColumnTests
{
	[Theory]
	[InlineData(ColumnType.StaticColumns, false)]
	[InlineData(ColumnType.DynamicColumns, false)]
	[InlineData(ColumnType.DynamicColumns, true)]
	public void DwgReaderPreservesColumnCount(ColumnType type, bool autoHeight)
	{
		var document = new CadDocument();
		document.Header.Version = ACadVersion.AC1032;
		var text = new MText { Value = "First\\NSecond", Height = 4, RectangleHeight = 50 };
		text.ColumnData.ColumnType = type;
		text.ColumnData.ColumnCount = 2;
		text.ColumnData.Width = 40;
		text.ColumnData.Gutter = 5;
		text.ColumnData.FlowReversed = true;
		text.ColumnData.AutoHeight = autoHeight;
		if (type == ColumnType.DynamicColumns && !autoHeight)
			text.ColumnData.Heights.AddRange(new double[] { 50, 60 });
		document.Entities.Add(text);
		using var output = new MemoryStream();
		DwgWriter.Write(output, document);
		using var input = new MemoryStream(output.ToArray());
		MText restored = Assert.Single(DwgReader.Read(input).Entities.OfType<MText>());
		Assert.Equal(type, restored.ColumnData.ColumnType);
		Assert.Equal(2, restored.ColumnData.ColumnCount);
		Assert.Equal(40, restored.ColumnData.Width);
		Assert.Equal(5, restored.ColumnData.Gutter);
		Assert.True(restored.ColumnData.FlowReversed);
		Assert.Equal(autoHeight, restored.ColumnData.AutoHeight);
		Assert.Equal(text.ColumnData.Heights, restored.ColumnData.Heights);
	}

	[Fact]
	public void CloneOwnsColumnHeights()
	{
		var columns = new MText.TextColumnData
		{
			ColumnType = ColumnType.DynamicColumns,
			ColumnCount = 2,
			Width = 40,
			Gutter = 5,
			FlowReversed = true,
			AutoHeight = false,
		};
		columns.Heights.AddRange(new double[] { 50, 60 });
		MText.TextColumnData clone = columns.Clone();
		Assert.NotSame(columns.Heights, clone.Heights);
		Assert.Equal(columns.ColumnType, clone.ColumnType);
		Assert.Equal(columns.ColumnCount, clone.ColumnCount);
		Assert.Equal(columns.Width, clone.Width);
		Assert.Equal(columns.Gutter, clone.Gutter);
		Assert.Equal(columns.FlowReversed, clone.FlowReversed);
		Assert.Equal(columns.AutoHeight, clone.AutoHeight);
		clone.Heights[0] = 100;
		clone.Heights.Add(70);
		Assert.Equal(new double[] { 50, 60 }, columns.Heights);
	}
}

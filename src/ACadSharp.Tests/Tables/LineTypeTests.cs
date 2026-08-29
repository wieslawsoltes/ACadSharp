using ACadSharp.Extensions;
using ACadSharp.Tables;
using ACadSharp.Tables.Collections;
using ACadSharp.Tests.Common;
using System;
using System.Linq;
using Xunit;

namespace ACadSharp.Tests.Tables
{
	public class LineTypeTests : TableEntryCommonTests<LineType>
	{
		[Fact]
		public void CloneTest()
		{
			var textStyle = new TextStyle("my_style");

			LineType lt = new LineType("segmented");
			lt.Description = "line type description";

			LineType.Segment s1 = new LineType.Segment
			{
				Length = 12,
				//Style = this.Document.TextStyles[TextStyle.DefaultName]
			};

			LineType.Segment s2 = new LineType.Segment
			{
				Length = -3,
				//Style = this.Document.TextStyles[TextStyle.DefaultName]
			};

			LineType.Segment s3 = new LineType.Segment
			{
				Length = 1,
				Style = textStyle
			};

			lt.AddSegment(s1);
			lt.AddSegment(s2);
			lt.AddSegment(s3);

			LineType clone = lt.CloneTyped();

			CadObjectTestUtils.AssertTableEntryClone(lt, clone);

			for (int i = 0; i < lt.Segments.Count(); i++)
			{
				Assert.Equal(lt.Segments.ElementAt(i).Length, clone.Segments.ElementAt(i).Length);
			}

			var last = clone.Segments.Last();

			Assert.NotNull(last.Style);
			Assert.NotEqual(textStyle, last.Style);
			Assert.Equal(textStyle.Name, last.Style.Name);
		}

		[Fact]
		public void ReplaceSegmentsPreservesAttachedEntryAndDetachesPreviousDefinition()
		{
			var document = new CadDocument();
			var style = new TextStyle("LIN_TEXT");
			document.TextStyles.Add(style);
			var lineType = new LineType("RELOAD");
			var previousDash = new LineType.Segment { Length = 2.0 };
			var previousGap = new LineType.Segment { Length = -1.0 };
			lineType.AddSegment(previousDash);
			lineType.AddSegment(previousGap);
			document.LineTypes.Add(lineType);
			ulong handle = lineType.Handle;
			var replacement = new LineType.Segment
			{
				IsText = true,
				Text = "HW",
				Style = style,
			};

			LineType.Segment[] detached = lineType.ReplaceSegments(new[] { replacement });

			Assert.Same(lineType, document.LineTypes[lineType.Name]);
			Assert.Equal(handle, lineType.Handle);
			Assert.Same(replacement, Assert.Single(lineType.Segments));
			Assert.Same(lineType, replacement.Owner);
			Assert.Same(style, replacement.Style);
			Assert.Equal(new[] { previousDash, previousGap }, detached);
			Assert.All(detached, segment => Assert.Null(segment.Owner));

			LineType.Segment[] secondDetached = lineType.ReplaceSegments(detached);

			Assert.Equal(new[] { previousDash, previousGap }, lineType.Segments);
			Assert.Same(replacement, Assert.Single(secondDetached));
			Assert.Null(replacement.Owner);

			lineType.ReplaceSegments(secondDetached);

			Assert.Same(replacement, Assert.Single(lineType.Segments));
			Assert.Same(style, replacement.Style);
		}

		[Fact]
		public void ReplaceSegmentsRejectsWholeInvalidDefinitionBeforeMutation()
		{
			var document = new CadDocument();
			var lineType = new LineType("ATOMIC");
			var original = new LineType.Segment { Length = 1.0 };
			lineType.AddSegment(original);
			document.LineTypes.Add(lineType);
			var foreignStyle = new TextStyle("FOREIGN");
			var valid = new LineType.Segment { Length = 2.0 };
			var invalid = new LineType.Segment { IsText = true, Style = foreignStyle };

			Assert.Throws<ArgumentException>(() =>
				lineType.ReplaceSegments(new[] { valid, invalid }));

			Assert.Same(original, Assert.Single(lineType.Segments));
			Assert.Same(lineType, original.Owner);
			Assert.Null(valid.Owner);
			Assert.Null(invalid.Owner);
			Assert.False(document.TextStyles.Contains(foreignStyle.Name));
		}

		protected override Table<LineType> getTable(CadDocument document)
		{
			return document.LineTypes;
		}
	}
}

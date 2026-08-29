using ACadSharp.Entities;
using CSMath;
using System;
using Xunit;

namespace ACadSharp.Tests
{
	public class CadObjectCollectionTests
	{
		[Fact]
		public void AddRangePublishesOnlyTheCompleteStructuralBatch()
		{
			CadDocument document = new CadDocument();
			Line retained = new Line(XYZ.Zero, XYZ.AxisX);
			Line first = new Line(XYZ.AxisY, XYZ.AxisY + XYZ.AxisX);
			Line second = new Line(XYZ.AxisZ, XYZ.AxisZ + XYZ.AxisX);
			document.Entities.Add(retained);
			int observedCount = -1;
			document.Entities.OnAdd += (_, _) => observedCount = document.Entities.Count;

			document.Entities.AddRange(new[] { first, second });

			Assert.Equal(3, observedCount);
			Assert.Equal(3, document.Entities.Count);
			Assert.Same(document.ModelSpace, first.Owner);
			Assert.Same(document.ModelSpace, second.Owner);
			Assert.Same(document, first.Document);
			Assert.Same(document, second.Document);
			Assert.NotEqual(0UL, first.Handle);
			Assert.NotEqual(0UL, second.Handle);
		}

		[Fact]
		public void AddRangeRejectsDuplicateOrOwnedItemsBeforeMutation()
		{
			CadDocument document = new CadDocument();
			Line attached = new Line(XYZ.Zero, XYZ.AxisX);
			Line detached = new Line(XYZ.AxisY, XYZ.AxisY + XYZ.AxisX);
			document.Entities.Add(attached);
			int notificationCount = 0;
			document.Entities.OnAdd += (_, _) => notificationCount++;

			Assert.Throws<ArgumentException>(() =>
				document.Entities.AddRange(new[] { detached, detached }));
			Assert.Throws<ArgumentException>(() =>
				document.Entities.AddRange(new[] { detached, attached }));

			Assert.Equal(0, notificationCount);
			Assert.Single(document.Entities);
			Assert.Contains(attached, document.Entities);
			Assert.Null(detached.Owner);
			Assert.Null(detached.Document);
			Assert.Equal(0UL, detached.Handle);
		}

		[Fact]
		public void TryRemoveRangePublishesOnlyTheCompleteStructuralBatch()
		{
			CadDocument document = new CadDocument();
			Line first = new Line(XYZ.Zero, XYZ.AxisX);
			Line retained = new Line(XYZ.AxisY, XYZ.AxisY + XYZ.AxisX);
			Line second = new Line(XYZ.AxisZ, XYZ.AxisZ + XYZ.AxisX);
			document.Entities.Add(first);
			document.Entities.Add(retained);
			document.Entities.Add(second);
			int observedCount = -1;
			document.Entities.OnRemove += (_, _) => observedCount = document.Entities.Count;

			bool removed = document.Entities.TryRemoveRange(new[] { first, second });

			Assert.True(removed);
			Assert.Equal(1, observedCount);
			Assert.Single(document.Entities);
			Assert.Contains(retained, document.Entities);
			Assert.Null(first.Owner);
			Assert.Null(second.Owner);
			Assert.Equal(0UL, first.Handle);
			Assert.Equal(0UL, second.Handle);
		}

		[Fact]
		public void TryRemoveRangeCancellationLeavesEveryItemAttached()
		{
			CadDocument document = new CadDocument();
			Line first = new Line(XYZ.Zero, XYZ.AxisX);
			Line second = new Line(XYZ.AxisY, XYZ.AxisY + XYZ.AxisX);
			document.Entities.Add(first);
			document.Entities.Add(second);
			ulong firstHandle = first.Handle;
			ulong secondHandle = second.Handle;
			document.Entities.OnBeforeRemove += (_, args) =>
			{
				if (ReferenceEquals(args.Item, second))
					args.Cancel = true;
			};

			bool removed = document.Entities.TryRemoveRange(new[] { first, second });

			Assert.False(removed);
			Assert.Equal(2, document.Entities.Count);
			Assert.Same(document.ModelSpace, first.Owner);
			Assert.Same(document.ModelSpace, second.Owner);
			Assert.Equal(firstHandle, first.Handle);
			Assert.Equal(secondHandle, second.Handle);
		}

		[Fact]
		public void TryRemoveRangeRejectsDuplicateOrForeignItemsBeforePreflight()
		{
			CadDocument document = new CadDocument();
			Line attached = new Line(XYZ.Zero, XYZ.AxisX);
			Line detached = new Line(XYZ.AxisY, XYZ.AxisY + XYZ.AxisX);
			document.Entities.Add(attached);
			int preflightCount = 0;
			document.Entities.OnBeforeRemove += (_, _) => preflightCount++;

			Assert.Throws<ArgumentException>(() =>
				document.Entities.TryRemoveRange(new[] { attached, attached }));
			Assert.Throws<ArgumentException>(() =>
				document.Entities.TryRemoveRange(new[] { attached, detached }));
			Assert.Equal(0, preflightCount);
			Assert.Single(document.Entities);
		}
	}
}

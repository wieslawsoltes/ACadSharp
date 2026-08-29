using ACadSharp.Entities;
using CSMath;
using System;
using System.Linq;
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

		[Fact]
		public void CollectionsRetainExplicitInsertionOrder()
		{
			CadDocument document = new CadDocument();
			Line first = new Line(XYZ.Zero, XYZ.AxisX);
			Line second = new Line(XYZ.AxisY, XYZ.AxisY + XYZ.AxisX);
			Line third = new Line(XYZ.AxisZ, XYZ.AxisZ + XYZ.AxisX);
			Line fourth = new Line(XYZ.AxisX, XYZ.AxisY);

			document.Entities.AddRange(new[] { first, second, third });
			Assert.True(document.Entities.Remove(second));
			document.Entities.Add(fourth);

			Assert.Equal(new[] { first, third, fourth }, document.Entities.ToArray());
			Assert.Same(first, document.Entities[0]);
			Assert.Same(third, document.Entities[1]);
			Assert.Same(fourth, document.Entities[2]);
		}

		[Fact]
		public void SeqendTracksOnlyEmptyNonEmptyTransitions()
		{
			CadDocument document = new CadDocument();
			Insert insert = new Insert();
			document.Entities.Add(insert);
			AttributeEntity first = new AttributeEntity { Tag = "FIRST" };
			AttributeEntity second = new AttributeEntity { Tag = "SECOND" };

			insert.Attributes.AddRange(new[] { first, second });

			Seqend seqend = insert.Attributes.Seqend;
			ulong seqendHandle = seqend.Handle;
			Assert.NotEqual(0UL, seqendHandle);
			Assert.Same(seqend, document.GetCadObject<Seqend>(seqendHandle));

			Assert.True(insert.Attributes.Remove(first));
			Assert.Same(seqend, insert.Attributes.Seqend);
			Assert.Same(seqend, document.GetCadObject<Seqend>(seqendHandle));

			Assert.True(insert.Attributes.Remove(second));
			Assert.Null(insert.Attributes.Seqend);
			Assert.Null(document.GetCadObject<Seqend>(seqendHandle));
			Assert.Null(seqend.Document);
			Assert.Equal(0UL, seqend.Handle);
		}

		[Fact]
		public void ReversibleReplacementPreservesOrderIdentityAndHandles()
		{
			CadDocument document = new CadDocument();
			Insert insert = new Insert();
			AttributeEntity first = new AttributeEntity { Tag = "FIRST", Value = "1" };
			AttributeEntity retained = new AttributeEntity { Tag = "RETAINED", Value = "2" };
			AttributeEntity third = new AttributeEntity { Tag = "THIRD", Value = "3" };
			insert.Attributes.AddRange(new[] { first, retained, third });
			document.Entities.Add(insert);
			ulong firstHandle = first.Handle;
			ulong retainedHandle = retained.Handle;
			ulong thirdHandle = third.Handle;
			AttributeEntity added = new AttributeEntity { Tag = "ADDED", Value = "4" };
			CadObjectCollection<AttributeEntity>.ReversibleReplacement replacement =
				insert.Attributes.CreateReversibleReplacement(new[] { added, retained });

			Assert.True(replacement.TryApply());
			ulong addedHandle = added.Handle;
			Assert.NotEqual(0UL, addedHandle);
			Assert.Equal(new[] { added, retained }, insert.Attributes.ToArray());
			Assert.Null(document.GetCadObject<AttributeEntity>(firstHandle));
			Assert.Null(document.GetCadObject<AttributeEntity>(thirdHandle));
			Assert.Same(document, first.Document);
			Assert.Same(document, third.Document);
			Assert.Equal(firstHandle, first.Handle);
			Assert.Equal(thirdHandle, third.Handle);
			Assert.Throws<InvalidOperationException>(() => document.RestoreHandles());

			Assert.True(replacement.TryRevert());
			Assert.Equal(new[] { first, retained, third }, insert.Attributes.ToArray());
			Assert.Same(first, document.GetCadObject<AttributeEntity>(firstHandle));
			Assert.Same(retained, document.GetCadObject<AttributeEntity>(retainedHandle));
			Assert.Same(third, document.GetCadObject<AttributeEntity>(thirdHandle));
			Assert.Null(document.GetCadObject<AttributeEntity>(addedHandle));
			Assert.Same(document, added.Document);
			Assert.Equal(addedHandle, added.Handle);

			Assert.True(replacement.TryApply());
			Assert.Equal(addedHandle, added.Handle);
			Assert.Same(added, document.GetCadObject<AttributeEntity>(addedHandle));
			replacement.Release();

			Assert.Null(first.Document);
			Assert.Null(third.Document);
			Assert.Equal(0UL, first.Handle);
			Assert.Equal(0UL, third.Handle);
			insert.Attributes.Add(new AttributeEntity { Tag = "AFTER_RELEASE" });
			Assert.Equal(3, insert.Attributes.Count);
		}

		[Fact]
		public void ReversibleReplacementsComposeInHistoryOrder()
		{
			CadDocument document = new CadDocument();
			Insert insert = new Insert();
			AttributeEntity first = new AttributeEntity { Tag = "FIRST" };
			insert.Attributes.Add(first);
			document.Entities.Add(insert);
			AttributeEntity second = new AttributeEntity { Tag = "SECOND" };
			CadObjectCollection<AttributeEntity>.ReversibleReplacement earlier =
				insert.Attributes.CreateReversibleReplacement(new[] { first, second });

			Assert.True(earlier.TryApply());
			AttributeEntity third = new AttributeEntity { Tag = "THIRD" };
			CadObjectCollection<AttributeEntity>.ReversibleReplacement later =
				insert.Attributes.CreateReversibleReplacement(new[] { second, third });

			Assert.True(later.TryApply());
			Assert.Equal(new[] { second, third }, insert.Attributes.ToArray());
			Assert.True(later.TryRevert());
			Assert.Equal(new[] { first, second }, insert.Attributes.ToArray());
			Assert.True(earlier.TryRevert());
			Assert.Same(first, Assert.Single(insert.Attributes));
			Assert.True(earlier.TryApply());
			Assert.True(later.TryApply());
			Assert.Equal(new[] { second, third }, insert.Attributes.ToArray());

			earlier.Release();
			later.Release();
			Assert.Null(first.Document);
			Assert.Equal(0UL, first.Handle);
		}

		[Fact]
		public void EmptyReplacementLeasesAndRestoresSeqend()
		{
			CadDocument document = new CadDocument();
			Insert insert = new Insert();
			AttributeEntity attribute = new AttributeEntity { Tag = "ONLY" };
			insert.Attributes.Add(attribute);
			document.Entities.Add(insert);
			Seqend seqend = insert.Attributes.Seqend;
			ulong attributeHandle = attribute.Handle;
			ulong seqendHandle = seqend.Handle;
			CadObjectCollection<AttributeEntity>.ReversibleReplacement replacement =
				insert.Attributes.CreateReversibleReplacement(Array.Empty<AttributeEntity>());

			Assert.True(replacement.TryApply());
			Assert.Empty(insert.Attributes);
			Assert.Null(document.GetCadObject<AttributeEntity>(attributeHandle));
			Assert.Null(document.GetCadObject<Seqend>(seqendHandle));
			Assert.Equal(attributeHandle, attribute.Handle);
			Assert.Equal(seqendHandle, seqend.Handle);

			Assert.True(replacement.TryRevert());
			Assert.Same(attribute, Assert.Single(insert.Attributes));
			Assert.Same(seqend, insert.Attributes.Seqend);
			Assert.Same(attribute, document.GetCadObject<AttributeEntity>(attributeHandle));
			Assert.Same(seqend, document.GetCadObject<Seqend>(seqendHandle));

			Assert.True(replacement.TryApply());
			replacement.Release();
			Assert.Null(attribute.Document);
			Assert.Null(seqend.Document);
			Assert.Equal(0UL, attribute.Handle);
			Assert.Equal(0UL, seqend.Handle);

			AttributeEntity later = new AttributeEntity { Tag = "LATER" };
			insert.Attributes.Add(later);
			Assert.NotEqual(0UL, insert.Attributes.Seqend.Handle);
			Assert.Same(insert.Attributes.Seqend, document.GetCadObject<Seqend>(
				insert.Attributes.Seqend.Handle));
		}

		[Fact]
		public void ReplacementCancellationLeavesCollectionAndLeaseStateUntouched()
		{
			CadDocument document = new CadDocument();
			Line first = new Line(XYZ.Zero, XYZ.AxisX);
			Line second = new Line(XYZ.AxisY, XYZ.AxisY + XYZ.AxisX);
			document.Entities.AddRange(new[] { first, second });
			Line added = new Line(XYZ.AxisZ, XYZ.AxisZ + XYZ.AxisX);
			CadObjectCollection<Entity>.ReversibleReplacement replacement =
				document.Entities.CreateReversibleReplacement(new Entity[] { first, added });
			document.Entities.OnBeforeRemove += (_, args) =>
			{
				if (ReferenceEquals(args.Item, second))
					args.Cancel = true;
			};

			Assert.False(replacement.TryApply());
			Assert.Equal(new Entity[] { first, second }, document.Entities.ToArray());
			Assert.Null(added.Owner);
			Assert.Null(added.Document);
			Assert.Equal(0UL, added.Handle);
			replacement.Release();
			document.Entities.Add(added);
			Assert.Equal(3, document.Entities.Count);
		}
	}
}

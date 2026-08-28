using ACadSharp.Entities;
using ACadSharp.Tables;
using CSMath;
using System;
using Xunit;

namespace ACadSharp.Tests.Entities;

public abstract class CommonDimensionTests<T> : CommonEntityTests<T>
	where T : Dimension, new()
{
	public abstract DimensionType Type { get; }

	[Fact]
	public void DimensionStyleOverride()
	{
		T dim = new T();
		DimensionStyle style = new DimensionStyle();
		style.ScaleFactor = 2.0;
		style.ToleranceZeroHandling = ZeroHandling.ShowZeroFeetSuppressZeroInches;

		dim.SetDimensionOverride(style);

		var current = dim.GetActiveDimensionStyle();

		Assert.NotNull(current);
		Assert.Equal("override", current.Name);
		Assert.Equal(style.ScaleFactor, current.ScaleFactor);
	}

	[Fact]
	public void DimensionTypeTest()
	{
		T dim = new T();

		Assert.True(dim.Flags.HasFlag(this.Type));
		Assert.True(dim.Flags.HasFlag(DimensionType.BlockReference));
	}

	[Fact]
	public void DimStyleNotNull()
	{
		T dim = new T();

		Assert.NotNull(dim.Style);
		Assert.Throws<ArgumentNullException>(() => dim.Style = null);
	}

	[Fact]
	public void IsAngularTest()
	{
		T dim = this.createDim();

		Assert.Equal(dim.Flags.HasFlag(DimensionType.Angular) || dim.Flags.HasFlag(DimensionType.Angular3Point), dim.IsAngular);
	}

	[Fact]
	public void IsTextUserDefinedLocationTest()
	{
		T dim = new T();

		Assert.False(dim.Flags.HasFlag(DimensionType.TextUserDefinedLocation));

		dim.IsTextUserDefinedLocation = true;

		Assert.True(dim.Flags.HasFlag(DimensionType.TextUserDefinedLocation));
	}

	[Fact]
	public virtual void UpdateBlockTests()
	{
		T dim = this.createDim();

		Assert.Null(dim.Block);

		dim.UpdateBlock();

		Assert.NotNull(dim.Block);
		Assert.True(dim.Block.IsAnonymous);
	}

	[Fact]
	public void TranslationKeepsWorldAndObjectCoordinateFieldsDistinct()
	{
		T dim = new T
		{
			Normal = XYZ.AxisY,
			DefinitionPoint = new XYZ(10, 20, 30),
			IsTextUserDefinedLocation = true,
			TextMiddlePoint = new XYZ(1, 2, 3),
		};

		dim.ApplyTranslation(new XYZ(5, 6, 7));

		Assert.Equal(new XYZ(15, 26, 37), dim.DefinitionPoint);
		// OCS X/Y/Z for normal +Y are WCS -X/+Z/+Y.
		Assert.Equal(new XYZ(-4, 9, 9), dim.TextMiddlePoint);
	}

	[Fact]
	public void AddingCloneKeepsItsPersistedPictureIndependent()
	{
		var document = new CadDocument();
		T source = new T
		{
			Block = new BlockRecord("SOURCE_DIMENSION_PICTURE"),
		};
		source.Block.Entities.Add(new Line(XYZ.Zero, XYZ.AxisX));
		document.Entities.Add(source);

		T clone = (T)source.Clone();
		document.Entities.Add(clone);

		Assert.NotSame(source.Block, clone.Block);
		Assert.NotEqual(source.Block.Name, clone.Block.Name);
		Assert.Single(source.Block.Entities);
		Assert.Single(clone.Block.Entities);
	}

	protected virtual T createDim()
	{
		return new T();
	}
}

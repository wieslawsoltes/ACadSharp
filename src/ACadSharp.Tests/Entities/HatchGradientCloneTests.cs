using ACadSharp.Entities;
using Xunit;

namespace ACadSharp.Tests.Entities;

public class HatchGradientCloneTests
{
	[Fact]
	public void CloneKeepsSourceColorsAndOwnsIndependentEntries()
	{
		var source = new HatchGradientPattern("LINEAR")
		{
			Enabled = true,
			Angle = 0.4,
			Shift = 0.25,
			IsSingleColorGradient = true,
			ColorTint = 0.75,
		};
		source.Colors.Add(new GradientColor { Value = 0, Color = new Color(255, 0, 0) });
		source.Colors.Add(new GradientColor { Value = 1, Color = new Color(0, 0, 255) });
		HatchGradientPattern clone = source.Clone();

		Assert.Equal(2, source.Colors.Count);
		Assert.Equal(2, clone.Colors.Count);
		Assert.NotSame(source.Colors, clone.Colors);
		Assert.NotSame(source.Colors[0], clone.Colors[0]);
		Assert.Equal(source.Name, clone.Name);
		Assert.Equal(source.Angle, clone.Angle);
		Assert.Equal(source.Shift, clone.Shift);
		Assert.Equal(source.Enabled, clone.Enabled);
		Assert.Equal(source.IsSingleColorGradient, clone.IsSingleColorGradient);
		Assert.Equal(source.ColorTint, clone.ColorTint);
		clone.Colors[0].Value = 0.5;
		clone.Colors[0].Color = new Color(0, 255, 0);
		Assert.Equal(0, source.Colors[0].Value);
		Assert.Equal(new Color(255, 0, 0), source.Colors[0].Color);
		clone.Colors.Clear();
		Assert.Equal(2, source.Colors.Count);
	}
}

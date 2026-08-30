using ACadSharp.IO;
using ACadSharp.Types.Units;
using System;
using System.IO;
using Xunit;

namespace ACadSharp.Tests.IO.DXF;

public class DxfHeaderAngleTests
{
	[Fact]
	public void AngleBaseRoundTripsInRadians()
	{
		var document = new CadDocument();
		document.Header.AngleBase = Math.PI / 6.0;
		document.Header.AngularDirection = AngularDirection.ClockWise;
		using var stream = new MemoryStream();

		DxfWriter.Write(stream, document);
		using var input = new MemoryStream(stream.ToArray());
		CadDocument loaded = DxfReader.Read(input);

		Assert.Equal(Math.PI / 6.0, loaded.Header.AngleBase, 12);
		Assert.Equal(AngularDirection.ClockWise, loaded.Header.AngularDirection);
	}
}

namespace ACadSharp.Objects;

/// <summary>
/// Controls whether raster-image frames are displayed and plotted.
/// </summary>
/// <remarks>
/// The persisted RASTERVARIABLES group-code 70 value uses 3 for the
/// display-without-plot state, while AutoCAD exposes that state as IMAGEFRAME
/// value 2.
/// </remarks>
public enum ImageFrameType : short
{
	/// <summary>Frames are not displayed or plotted.</summary>
	NoDisplayOrPlotted = 0,

	/// <summary>Frames are displayed and plotted.</summary>
	DisplayAndPlotted = 1,

	/// <summary>Frames are displayed, but not plotted.</summary>
	DisplayNoPlotted = 3,
}

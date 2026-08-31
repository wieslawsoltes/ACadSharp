using ACadSharp.Attributes;
using CSMath;

namespace ACadSharp.Entities;

/// <summary>
/// Represents a base type for Vertex entities.
/// </summary>
[DxfSubClass(DxfSubclassMarker.Vertex, true)]
public abstract class Vertex : Entity, IVertex
{
	/// <inheritdoc/>
	[DxfCodeValue(DxfReferenceType.Optional, 42)]
	public double Bulge { get; set; } = 0.0;

	/// <summary>
	/// Curve fit tangent direction.
	/// </summary>
	[DxfCodeValue(DxfReferenceType.IsAngle, 50)]
	public double CurveTangent { get; set; }

	/// <summary>
	/// Ending width.
	/// </summary>
	[DxfCodeValue(DxfReferenceType.Optional, 41)]
	public double EndWidth
	{
		get => this._endWidth;
		set
		{
			this._endWidth = value;
			this.HasEndWidth = true;
		}
	}

	/// <summary>
	/// Indicates whether an explicit ending width was assigned or read.
	/// </summary>
	public bool HasEndWidth { get; private set; }

	/// <summary>
	/// Vertex flags.
	/// </summary>
	[DxfCodeValue(70)]
	public virtual VertexFlags Flags { get => _flags; set => _flags = value; }

	/// <summary>
	/// Vertex identifier.
	/// </summary>
	[DxfCodeValue(DxfReferenceType.Ignored, 91)]    //TODO: for some versions this code is invalid
	public int Id { get; set; }

	/// <summary>
	/// Location point (in OCS when 2D, and WCS when 3D).
	/// </summary>
	[DxfCodeValue(10, 20, 30)]
	public XYZ Location { get; set; } = XYZ.Zero;

	IVector IVertex.Location { get { return this.Location; } set { this.Location = value.Convert<XYZ>(); } }

	/// <inheritdoc/>
	public override string ObjectName => DxfFileToken.EntityVertex;

	/// <summary>
	/// Starting width
	/// </summary>
	[DxfCodeValue(DxfReferenceType.Optional, 40)]
	public double StartWidth
	{
		get => this._startWidth;
		set
		{
			this._startWidth = value;
			this.HasStartWidth = true;
		}
	}

	/// <summary>
	/// Indicates whether an explicit starting width was assigned or read.
	/// </summary>
	public bool HasStartWidth { get; private set; }

	protected VertexFlags _flags;
	private double _endWidth;
	private double _startWidth;

	/// <summary>
	/// Clears the optional ending-width value so a legacy polyline vertex can
	/// inherit its owning entity's default.
	/// </summary>
	public void ClearEndWidth()
	{
		this._endWidth = 0.0;
		this.HasEndWidth = false;
	}

	/// <summary>
	/// Clears the optional starting-width value so a legacy polyline vertex can
	/// inherit its owning entity's default.
	/// </summary>
	public void ClearStartWidth()
	{
		this._startWidth = 0.0;
		this.HasStartWidth = false;
	}

	internal void CopyWidthStateFrom(Vertex source)
	{
		this._startWidth = source._startWidth;
		this._endWidth = source._endWidth;
		this.HasStartWidth = source.HasStartWidth;
		this.HasEndWidth = source.HasEndWidth;
	}

	internal void SetDwgWidthState(double startWidth, double endWidth)
	{
		this._startWidth = startWidth;
		this._endWidth = endWidth;
		// Legacy DWG stores a zero pair for inherited entity defaults. A
		// nonzero member makes the encoded pair an explicit segment profile.
		this.HasStartWidth = startWidth != 0.0 || endWidth != 0.0;
		this.HasEndWidth = this.HasStartWidth;
	}

	/// <summary>
	/// Default constructor.
	/// </summary>
	protected Vertex()
	{ }

	/// <summary>
	/// Initializes a new instance of the Vertex class with the specified location.
	/// </summary>
	/// <param name="location">The location of the vertex. Must implement the IVector interface and be convertible to an XYZ vector.</param>
	protected Vertex(IVector location)
	{
		this.Location = location.Convert<XYZ>();
	}

	/// <inheritdoc/>
	public override void ApplyTransform(Transform transform)
	{
		this.Location = transform.ApplyTransform(this.Location);
	}

	/// <inheritdoc/>
	public override BoundingBox GetBoundingBox()
	{
		return new BoundingBox(this.Location);
	}

	/// <inheritdoc/>
	public override string ToString()
	{
		return $"{this.SubclassMarker}|{this.Location.ToString()}";
	}
}

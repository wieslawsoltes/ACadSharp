using ACadSharp.Attributes;
using CSMath;

namespace ACadSharp.Entities;

public partial class LwPolyline
{
	public class Vertex : IVertex
	{
		/// <summary>
		/// The bulge is the tangent of one fourth the included angle for an arc segment, made negative if the arc goes clockwise from the start point to the endpoint.A bulge of 0 indicates a straight segment, and a bulge of 1 is a semicircle
		/// </summary>
		[DxfCodeValue(DxfReferenceType.Optional, 42)]
		public double Bulge { get; set; } = 0.0;

		/// <summary>
		/// Curve fit tangent direction
		/// </summary>
		[DxfCodeValue(50)]
		public double CurveTangent { get; set; } = 0;

		/// <summary>
		/// Ending width
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
		/// Vertex flags
		/// </summary>
		[DxfCodeValue(70)]
		public VertexFlags Flags { get; set; } = VertexFlags.Default;

		/// <summary>
		/// Vertex identifier
		/// </summary>
		[DxfCodeValue(91)]
		public int Id { get; set; } = 0;

		/// <summary>
		/// Vertex coordinates (in OCS)
		/// </summary>
		[DxfCodeValue(10, 20)]
		public XY Location { get; set; } = XY.Zero;

		IVector IVertex.Location { get { return this.Location; } set { this.Location = value.Convert<XY>(); } }

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

		private double _endWidth;
		private double _startWidth;

		/// <summary>
		/// Clears the optional ending-width value.
		/// </summary>
		public void ClearEndWidth()
		{
			this._endWidth = 0.0;
			this.HasEndWidth = false;
		}

		/// <summary>
		/// Clears the optional starting-width value.
		/// </summary>
		public void ClearStartWidth()
		{
			this._startWidth = 0.0;
			this.HasStartWidth = false;
		}

		public Vertex()
		{ }

		public Vertex(XY location)
		{
			Location = location;
		}

		public Vertex(double x, double y)
			: this(new XY(x, y))
		{
		}

		/// <inheritdoc/>
		public Vertex Clone()
		{
			return (Vertex)this.MemberwiseClone();
		}

		/// <inheritdoc/>
		public override string ToString()
		{
			return this.Location.ToString();
		}
	}
}

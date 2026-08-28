using ACadSharp.Attributes;
using ACadSharp.Tables;
using CSMath;
using System;

namespace ACadSharp.Entities;

/// <summary>
/// Represents a <see cref="Shape"/> entity.
/// </summary>
/// <remarks>
/// Object name <see cref="DxfFileToken.EntityShape"/> <br/>
/// Dxf class name <see cref="DxfSubclassMarker.Shape"/>
/// </remarks>
[DxfName(DxfFileToken.EntityShape)]
[DxfSubClass(DxfSubclassMarker.Shape)]
public class Shape : Entity, IOrientable
{
	/// <summary>
	/// Insertion point (in WCS).
	/// </summary>
	[DxfCodeValue(10, 20, 30)]
	public XYZ InsertionPoint { get; set; }

	/// <inheritdoc/>
	[DxfCodeValue(210, 220, 230)]
	public XYZ Normal { get; set; } = XYZ.AxisZ;

	/// <inheritdoc/>
	public override string ObjectName => DxfFileToken.EntityShape;

	/// <inheritdoc/>
	public override ObjectType ObjectType => ObjectType.SHAPE;

	/// <summary>
	/// Oblique angle.
	/// </summary>
	[DxfCodeValue(DxfReferenceType.IsAngle, 51)]
	public double ObliqueAngle { get; set; } = 0;

	/// <summary>
	/// Relative X scale factor.
	/// </summary>
	[DxfCodeValue(41)]
	public double RelativeXScale { get; set; } = 1;

	/// <summary>
	/// Rotation angle.
	/// </summary>
	[DxfCodeValue(DxfReferenceType.IsAngle, 50)]
	public double Rotation { get; set; } = 0;

	/// <summary>
	/// Name of the shape stored in DXF group code 2.
	/// </summary>
	[DxfCodeValue(DxfReferenceType.Name, 2)]
	public string ShapeName { get; set; } = string.Empty;

	/// <summary>
	/// Shape-file text style that owns the shape definition.
	/// </summary>
	/// <remarks>
	/// DWG stores this relationship explicitly. DXF stores only
	/// <see cref="ShapeName"/>, so this property can be <see langword="null"/>
	/// until a host resolves the loaded SHX shape files.
	/// </remarks>
	public TextStyle ShapeStyle
	{
		get { return this._style; }
		set
		{
			if (value != null && !value.IsShapeFile)
			{
				throw new ArgumentException("The text style must reference a shape file.", nameof(value));
			}

			if (value != null && this.Document != null)
			{
				this._style = CadObject.updateCollection(value, this.Document.TextStyles);
			}
			else
			{
				this._style = value;
			}
		}
	}

	/// <summary>
	/// Size.
	/// </summary>
	[DxfCodeValue(40)]
	public double Size { get; set; } = 1.0;

	/// <inheritdoc/>
	public override string SubclassMarker => DxfSubclassMarker.Shape;

	/// <summary>
	/// Thickness.
	/// </summary>
	[DxfCodeValue(39)]
	public double Thickness { get; set; } = 0.0;

	/// <summary>
	/// Number of the shape within <see cref="ShapeStyle"/>'s SHX file.
	/// </summary>
	/// <remarks>
	/// DWG stores the number directly. DXF stores the corresponding
	/// <see cref="ShapeName"/> instead.
	/// </remarks>
	public ushort ShapeNumber { get; set; }

	private TextStyle _style;

	/// <summary>
	/// Initializes a shape by the <see cref="TextStyle"/>
	/// </summary>
	/// <param name="textStyle">Text style with the flag <see cref="TextStyle.IsShapeFile"/></param>
	public Shape(TextStyle textStyle)
	{
		this.ShapeStyle = textStyle;
	}

	internal Shape() : base()
	{
	}

	/// <inheritdoc/>
	public override void ApplyTransform(Transform transform)
	{
		XYZ oldOrigin = this.InsertionPoint;
		XYZ newOrigin = transform.ApplyTransform(oldOrigin);
		XYZ newNormal = this.transformNormal(transform, this.Normal);
		Matrix3 objectToWorld = Matrix3.ArbitraryAxis(this.Normal);

		double cosine = Math.Cos(this.Rotation);
		double sine = Math.Sin(this.Rotation);
		double shear = this.Size * Math.Tan(this.ObliqueAngle);
		XYZ localX = new XYZ(
			cosine * this.Size * this.RelativeXScale,
			sine * this.Size * this.RelativeXScale,
			0.0);
		XYZ localY = new XYZ(
			(cosine * shear) - (sine * this.Size),
			(sine * shear) + (cosine * this.Size),
			0.0);
		XYZ worldX = objectToWorld * localX;
		XYZ worldY = objectToWorld * localY;
		worldX = transform.ApplyTransform(oldOrigin + worldX) - newOrigin;
		worldY = transform.ApplyTransform(oldOrigin + worldY) - newOrigin;

		Matrix3 worldToObject = Matrix3.ArbitraryAxis(newNormal).Transpose();
		XYZ transformedX = worldToObject * worldX;
		XYZ transformedY = worldToObject * worldY;
		double xLength = Math.Sqrt(
			(transformedX.X * transformedX.X) +
			(transformedX.Y * transformedX.Y));
		if (!(xLength > 0.0) || !double.IsFinite(xLength))
		{
			throw new ArgumentException("The transform collapses the SHAPE X axis.", nameof(transform));
		}

		double rotation = Math.Atan2(transformedX.Y, transformedX.X);
		double rotatedCosine = transformedX.X / xLength;
		double rotatedSine = transformedX.Y / xLength;
		double along =
			(transformedY.X * rotatedCosine) +
			(transformedY.Y * rotatedSine);
		double height =
			(-transformedY.X * rotatedSine) +
			(transformedY.Y * rotatedCosine);
		if (!(height > 0.0) || !double.IsFinite(height))
		{
			throw new ArgumentException("The transform reflects or collapses the SHAPE Y axis.", nameof(transform));
		}

		double width = xLength / height;
		double oblique = Math.Atan2(along, height);
		if (!double.IsFinite(width) || !double.IsFinite(oblique))
		{
			throw new ArgumentException("The transformed SHAPE frame is not finite.", nameof(transform));
		}

		this.InsertionPoint = newOrigin;
		this.Normal = newNormal;
		this.Rotation = rotation;
		this.Size = height;
		this.RelativeXScale = width;
		this.ObliqueAngle = oblique;
	}

	/// <inheritdoc/>
	public override CadObject Clone()
	{
		Shape clone = (Shape)base.Clone();

		clone.ShapeStyle = (TextStyle)(this.ShapeStyle?.Clone());

		return clone;
	}

	/// <inheritdoc/>
	public override BoundingBox GetBoundingBox()
	{
		return new BoundingBox(this.InsertionPoint);
	}
}

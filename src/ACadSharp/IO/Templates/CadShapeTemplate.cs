using ACadSharp.Entities;
using ACadSharp.Tables;

namespace ACadSharp.IO.Templates
{
	internal class CadShapeTemplate : CadEntityTemplate
	{
		public ulong? ShapeFileHandle { get; set; }

		public string ShapeName { get; set; }

		public CadShapeTemplate(Shape shape) : base(shape) { }

		protected override void build(CadDocumentBuilder builder)
		{
			base.build(builder);

			Shape shape = this.CadObject as Shape;
			shape.ShapeName = this.ShapeName ?? shape.ShapeName;

			if (this.ShapeFileHandle.HasValue &&
				this.getTableReference(builder, this.ShapeFileHandle, null, out TextStyle text))
			{
				if (text.IsShapeFile)
				{
					shape.ShapeStyle = text;
				}
				else
				{
					builder.Notify($"Shape style {this.ShapeFileHandle} is not a shape-file style", NotificationType.Warning);
				}
			}
		}
	}
}

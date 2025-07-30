using ACadSharp;
using ACadSharp.Entities;
using CSMath;
using System.Drawing;

namespace ACadSharp.Viewer.Interfaces;

/// <summary>
/// Interface for CAD rendering backends
/// </summary>
public interface ICadRenderer
{
    /// <summary>
    /// Begins a new rendering session
    /// </summary>
    /// <param name="boundingBox">The bounding box of the content to render</param>
    /// <param name="backgroundColor">Background color</param>
    void BeginRender(BoundingBox boundingBox, Color backgroundColor = default);

    /// <summary>
    /// Ends the current rendering session
    /// </summary>
    void EndRender();

    /// <summary>
    /// Renders a complete CAD document
    /// </summary>
    /// <param name="document">The CAD document to render</param>
    void RenderDocument(CadDocument document);

    /// <summary>
    /// Renders a single entity
    /// </summary>
    /// <param name="entity">The entity to render</param>
    void RenderEntity(Entity entity);

    /// <summary>
    /// Renders a line entity
    /// </summary>
    /// <param name="line">The line to render</param>
    void RenderLine(Line line);

    /// <summary>
    /// Renders a circle entity
    /// </summary>
    /// <param name="circle">The circle to render</param>
    void RenderCircle(Circle circle);

    /// <summary>
    /// Renders an arc entity
    /// </summary>
    /// <param name="arc">The arc to render</param>
    void RenderArc(Arc arc);

    /// <summary>
    /// Renders an ellipse entity
    /// </summary>
    /// <param name="ellipse">The ellipse to render</param>
    void RenderEllipse(Ellipse ellipse);

    /// <summary>
    /// Renders a polyline entity
    /// </summary>
    /// <param name="polyline">The polyline to render</param>
    void RenderPolyline(IPolyline polyline);

    /// <summary>
    /// Renders a text entity
    /// </summary>
    /// <param name="text">The text to render</param>
    void RenderText(IText text);

    /// <summary>
    /// Renders a point entity
    /// </summary>
    /// <param name="point">The point to render</param>
    void RenderPoint(ACadSharp.Entities.Point point);

    /// <summary>
    /// Renders an insert (block reference) entity
    /// </summary>
    /// <param name="insert">The insert to render</param>
    void RenderInsert(Insert insert);

    /// <summary>
    /// Sets the current transformation matrix
    /// </summary>
    /// <param name="transform">The transformation matrix</param>
    void SetTransform(Matrix4 transform);

    /// <summary>
    /// Pushes the current transformation matrix onto the stack
    /// </summary>
    void PushTransform();

    /// <summary>
    /// Pops the transformation matrix from the stack
    /// </summary>
    void PopTransform();

    /// <summary>
    /// Sets the current pen for drawing operations
    /// </summary>
    /// <param name="color">Line color</param>
    /// <param name="width">Line width</param>
    /// <param name="lineType">Line type pattern</param>
    void SetPen(Color color, float width, string lineType = "Continuous");

    /// <summary>
    /// Sets the current brush for fill operations
    /// </summary>
    /// <param name="color">Fill color</param>
    void SetBrush(Color color);
}
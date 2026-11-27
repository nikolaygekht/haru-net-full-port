using System;
using System.Collections.Generic;
using Haru.Objects;
using Haru.Types;
using Haru.Xref;

namespace Haru.ThreeD
{
    /// <summary>
    /// A view of a 3D model (/Type /3DView): the camera, the projection, the background, the lighting, a cross
    /// section and the state of the model's nodes. Create it with <see cref="Hpdf3DModel.CreateView"/>.
    /// </summary>
    public class Hpdf3DView
    {
        private readonly HpdfDict _dict;
        private readonly HpdfXref _xref;
        private readonly List<Hpdf3DNode> _nodes = new List<Hpdf3DNode>();
        private HpdfArray? _nodeArray;
        private HpdfArray? _measureArray;

        private static readonly string[] LightingNames =
        {
            "Artwork", "None", "White", "Day", "Night", "Hard", "Primary", "Blue", "Red", "Cube", "CAD", "Headlamp"
        };

        internal Hpdf3DView(HpdfXref xref, Hpdf3DModel model, string name)
        {
            _xref = xref;
            Model = model;
            Name = name;

            // The C library writes /TYPE; the key is /Type
            _dict = new HpdfDict();
            _dict.Add("Type", new HpdfName("3DView"));
            _dict.Add("XN", new HpdfString(name));
            _dict.Add("IN", new HpdfString(name));
            xref.Add(_dict);
        }

        /// <summary>
        /// Gets the view dictionary.
        /// </summary>
        public HpdfDict Dict => _dict;

        /// <summary>
        /// Gets the model the view belongs to.
        /// </summary>
        public Hpdf3DModel Model { get; }

        /// <summary>
        /// Gets the name of the view (/XN and /IN).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the nodes whose state the view sets.
        /// </summary>
        public IReadOnlyList<Hpdf3DNode> Nodes => _nodes;

        /// <summary>
        /// Places the camera on an orbit around a point, looking at it (HPDF_3DView_SetCamera): /C2W, /CO, /MS /M.
        /// </summary>
        /// <param name="centerOfOrbit">The point the camera looks at, in world coordinates.</param>
        /// <param name="centerToCamera">The direction from that point to the camera (0, 0, 0 is (0, -1, 0)).</param>
        /// <param name="radius">The distance from the point to the camera.</param>
        /// <param name="roll">The roll of the camera around its viewing direction, in degrees.</param>
        public void SetCamera(HpdfPoint3D centerOfOrbit, HpdfPoint3D centerToCamera, float radius, float roll)
        {
            if (!IsFinite(centerOfOrbit) || !IsFinite(centerToCamera) || !float.IsFinite(radius) || !float.IsFinite(roll))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Camera values must be finite numbers");

            // The viewing direction, from the camera to the center of the orbit
            double viewX = -centerToCamera.X, viewY = -centerToCamera.Y, viewZ = -centerToCamera.Z;
            if (viewX == 0 && viewY == 0 && viewZ == 0)
                viewY = 1;
            Normalize(ref viewX, ref viewY, ref viewZ);

            // Top and bottom views
            double leftX = -1, leftY = 0, leftZ = 0;
            double upX = 0, upY = viewZ < 0 ? 1 : -1, upZ = 0;

            if (Math.Abs(viewX) + Math.Abs(viewY) != 0)
            {
                // Up: the world's up less its part along the view; left: up x view
                upX = -viewZ * viewX;
                upY = -viewZ * viewY;
                upZ = -viewZ * viewZ + 1;
                Normalize(ref upX, ref upY, ref upZ);

                leftX = viewZ * upY - viewY * upZ;
                leftY = viewX * upZ - viewZ * upX;
                leftZ = viewY * upX - viewX * upY;
                Normalize(ref leftX, ref leftY, ref leftZ);
            }

            // The roll turns left and up around the view; the C library adds the sine to both, which makes
            // the vectors non-orthogonal
            double sin = Math.Sin(roll * Math.PI / 180), cos = Math.Cos(roll * Math.PI / 180);
            (leftX, leftY, leftZ, upX, upY, upZ) = (
                leftX * cos + upX * sin, leftY * cos + upY * sin, leftZ * cos + upZ * sin,
                upX * cos - leftX * sin, upY * cos - leftY * sin, upZ * cos - leftZ * sin);

            double distance = Math.Abs(radius);
            if (distance == 0)
                distance = 1e-18;

            var matrix = new Hpdf3DMatrix(
                (float)leftX, (float)leftY, (float)leftZ,
                (float)upX, (float)upY, (float)upZ,
                (float)viewX, (float)viewY, (float)viewZ,
                (float)(centerOfOrbit.X - distance * viewX), (float)(centerOfOrbit.Y - distance * viewY),
                (float)(centerOfOrbit.Z - distance * viewZ));
            SetCameraMatrix(matrix, (float)distance);
        }

        /// <summary>
        /// Sets the camera by its camera-to-world matrix (/C2W, /MS /M) and the distance to the center of the orbit
        /// (/CO), around which viewers rotate the camera.
        /// </summary>
        /// <param name="cameraToWorld">The camera-to-world matrix.</param>
        /// <param name="centerOfOrbitDistance">The distance from the camera to the center of the orbit.</param>
        public void SetCameraMatrix(Hpdf3DMatrix cameraToWorld, float centerOfOrbitDistance)
        {
            if (!float.IsFinite(centerOfOrbitDistance) || centerOfOrbitDistance < 0)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The distance must be a non-negative number");

            var matrix = ToArray(cameraToWorld);
            _dict["MS"] = new HpdfName("M");
            _dict["C2W"] = matrix;
            _dict["CO"] = new HpdfReal(centerOfOrbitDistance);
        }

        /// <summary>
        /// Uses a perspective projection (/P /Subtype /P) with a field of view, scaled to the smaller dimension of
        /// the annotation (/PS /Min).
        /// </summary>
        /// <param name="fieldOfView">The field of view in degrees, 0 to 180.</param>
        public void SetPerspectiveProjection(float fieldOfView)
        {
            if (!(fieldOfView >= 0 && fieldOfView <= 180))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The field of view must be between 0 and 180 degrees");

            var projection = new HpdfDict();
            projection.Add("Subtype", new HpdfName("P"));
            projection.Add("PS", new HpdfName("Min"));
            projection.Add("FOV", new HpdfReal(fieldOfView));
            _dict["P"] = projection;
        }

        /// <summary>
        /// Uses an orthographic projection (/P /Subtype /O) with a scale factor (/OS).
        /// </summary>
        /// <param name="scale">The scale factor, positive.</param>
        public void SetOrthographicProjection(float scale)
        {
            if (!(scale > 0) || !float.IsFinite(scale))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The scale must be a positive number");

            var projection = new HpdfDict();
            projection.Add("Subtype", new HpdfName("O"));
            projection.Add("OS", new HpdfReal(scale));
            _dict["P"] = projection;
        }

        /// <summary>
        /// Sets the background color (/BG).
        /// </summary>
        public void SetBackgroundColor(float r, float g, float b)
        {
            if (!IsUnit(r) || !IsUnit(g) || !IsUnit(b))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Color values must be between 0 and 1");

            // The components in the color space /CS of the background, DeviceRGB by default
            var color = new HpdfArray();
            color.Add(new HpdfReal(r));
            color.Add(new HpdfReal(g));
            color.Add(new HpdfReal(b));
            var background = new HpdfDict();
            background.Add("Type", new HpdfName("3DBG"));
            background.Add("C", color);
            _dict["BG"] = background;
        }

        /// <summary>
        /// Sets the lighting scheme (/LS, PDF 1.7).
        /// </summary>
        /// <param name="lighting">The lighting scheme.</param>
        public void SetLighting(Hpdf3DLighting lighting)
        {
            if ((int)lighting < 0 || (int)lighting >= LightingNames.Length)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, $"Invalid lighting scheme: {lighting}");

            var scheme = new HpdfDict();
            scheme.Add("Type", new HpdfName("3DLightingScheme"));
            scheme.Add("Subtype", new HpdfName(LightingNames[(int)lighting]));
            _dict["LS"] = scheme;
            _xref.RequireVersion(Doc.HpdfVersion.Version17);
        }

        /// <summary>
        /// Cuts the model with a plane (/SA, PDF 1.7): the plane goes through a point; it starts perpendicular to the
        /// x axis and is turned around the y axis (roll) and the z axis (pitch).
        /// </summary>
        /// <param name="center">A point of the cutting plane.</param>
        /// <param name="roll">The rotation around the y axis, in degrees.</param>
        /// <param name="pitch">The rotation around the z axis, in degrees.</param>
        /// <param name="opacity">The opacity of the plane, 0 to 1.</param>
        /// <param name="showIntersection">Whether the intersection of the plane with the model is shown (in red).</param>
        public void SetCrossSection(HpdfPoint3D center, float roll, float pitch, float opacity, bool showIntersection)
        {
            if (!IsFinite(center) || !float.IsFinite(roll) || !float.IsFinite(pitch))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Cross section values must be finite numbers");
            if (!(opacity >= 0 && opacity <= 1))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The opacity must be between 0 and 1");

            var section = new HpdfDict();
            section.Add("Type", new HpdfName("3DCrossSection"));
            section.Add("C", ToArray(center));

            // One angle is null: the plane starts perpendicular to that axis
            var orientation = new HpdfArray();
            orientation.Add(HpdfNull.Instance);
            orientation.Add(new HpdfReal(roll));
            orientation.Add(new HpdfReal(pitch));
            section.Add("O", orientation);
            section.Add("PO", new HpdfReal(opacity));
            section.Add("IV", new HpdfBoolean(showIntersection));

            var intersectionColor = new HpdfArray();
            intersectionColor.Add(new HpdfName("DeviceRGB"));
            intersectionColor.Add(new HpdfReal(1));
            intersectionColor.Add(new HpdfReal(0));
            intersectionColor.Add(new HpdfReal(0));
            section.Add("IC", intersectionColor);

            var sections = new HpdfArray();
            sections.Add(section);
            _dict["SA"] = sections;
            _xref.RequireVersion(Doc.HpdfVersion.Version17);
        }

        /// <summary>
        /// Shows the model without a cross section (an empty /SA, which turns off the sections of the model file).
        /// </summary>
        public void RemoveCrossSection()
        {
            _dict["SA"] = new HpdfArray();
            _xref.RequireVersion(Doc.HpdfVersion.Version17);
        }

        /// <summary>
        /// Creates the state of a node of the model in this view (/NA, PDF 1.7).
        /// </summary>
        /// <param name="name">The name of the node in the model file.</param>
        /// <returns>The node state.</returns>
        public Hpdf3DNode CreateNode(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The node name cannot be null or empty");

            var node = new Hpdf3DNode(name);
            if (_nodeArray is null)
            {
                _nodeArray = new HpdfArray();
                _dict.Add("NA", _nodeArray);
            }
            _nodeArray.Add(node.Dict);
            _nodes.Add(node);
            _xref.RequireVersion(Doc.HpdfVersion.Version17);
            return node;
        }

        /// <summary>
        /// Adds a measurement shown in this view (/MA, Adobe ExtensionLevel 3).
        /// </summary>
        /// <param name="measure">The measurement.</param>
        public void AddMeasure(Hpdf3DMeasure measure)
        {
            if (measure is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Measure cannot be null");
            if (!ReferenceEquals(measure.Model, Model))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The measurement belongs to another model");

            if (_measureArray is null)
            {
                _measureArray = new HpdfArray();
                _dict.Add("MA", _measureArray);
            }
            foreach (var item in _measureArray)
            {
                if (ReferenceEquals(item, measure.Dict))
                    return;
            }
            _measureArray.Add(measure.Dict);
            _xref.RequireAdobeExtensionLevel(3);
        }

        internal static HpdfArray ToArray(Hpdf3DMatrix m)
        {
            var array = new HpdfArray();
            foreach (float value in new[] { m.A, m.B, m.C, m.D, m.E, m.F, m.G, m.H, m.I, m.Tx, m.Ty, m.Tz })
            {
                if (!float.IsFinite(value))
                    throw new HpdfException(HpdfErrorCode.InvalidParameter, "Matrix values must be finite numbers");
                array.Add(new HpdfReal(value));
            }
            return array;
        }

        internal static HpdfArray ToArray(HpdfPoint3D point)
        {
            var array = new HpdfArray();
            array.Add(new HpdfReal(point.X));
            array.Add(new HpdfReal(point.Y));
            array.Add(new HpdfReal(point.Z));
            return array;
        }

        internal static bool IsFinite(HpdfPoint3D point) =>
            float.IsFinite(point.X) && float.IsFinite(point.Y) && float.IsFinite(point.Z);

        /// <summary>
        /// A color of 3D dictionaries: [/DeviceRGB r g b].
        /// </summary>
        internal static HpdfArray ToColor(float r, float g, float b)
        {
            if (!IsUnit(r) || !IsUnit(g) || !IsUnit(b))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Color values must be between 0 and 1");

            var color = new HpdfArray();
            color.Add(new HpdfName("DeviceRGB"));
            color.Add(new HpdfReal(r));
            color.Add(new HpdfReal(g));
            color.Add(new HpdfReal(b));
            return color;
        }

        private static bool IsUnit(float value) => value >= 0 && value <= 1;

        private static void Normalize(ref double x, ref double y, ref double z)
        {
            double length = Math.Sqrt(x * x + y * y + z * z);
            if (length == 0)
                return;
            x /= length;
            y /= length;
            z /= length;
        }
    }
}

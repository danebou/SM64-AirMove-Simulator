using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeeSawSimulator
{
    class Program
    {
        struct Point
        {
            public short X, Y, Z;

            public Point(short x, short y, short z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            public static Point operator -(Point p1, Point p2)
            {
                return new Point((short)(p1.X - p2.X), (short)(p1.Y - p2.Y), (short)(p1.Z - p2.Z));
            }

            public override string ToString()
            {
                return String.Format("({0}, {1}, {2})", X, Y, Z);
            }
        }

        struct Triangle
        {
            public Point[] Vertices;
            public float Offset;
            public float NormalX;
            public float NormalY;
            public float NormalZ;

            public Triangle(Point p1, Point p2, Point p3)
            {
                Vertices = new Point[3] { p1, p2, p3 };
                Point line1 = p1 - p2;
                Point line2 = p1 - p3;

                float normX = line1.Y * line2.Z - line1.Z * line2.Y;
                float normY = line1.Z * line2.X - line1.X * line2.Z;
                float normZ = line1.X * line2.Y - line1.Y * line2.X;

                float magn = (float)Math.Sqrt(normX * normX + normY * normY + normZ * normZ);
                NormalX = normX / magn;
                NormalY = normY / magn;
                NormalZ = normZ / magn;

                Offset = -(p1.X * normX + p1.Y * normY + p1.Z * normZ);
            }
        }

        static readonly List<Point> SeeSawPoints = new List<Point>()
        {
            new Point(-511, 179, 307),
            new Point(512, 179, 307),
            new Point(512, 179, -306),
            new Point(512, 26, -306),
            new Point(512, 26, 307),
            new Point(-511, 26, 307),
            new Point(-511, 26, -306),
            new Point(-511, 179, -306)
        };

        static readonly List<int[]> SeeSawTriangleVertexIndices = new List<int[]>() {
           new int[3] { 00, 01, 02 },
           new int[3] { 05, 01, 00 },

           new int[3] { 05, 04, 01 },
           new int[3] { 04, 05, 06 },

           new int[3] { 04, 06, 03 },
           new int[3] { 07, 03, 06 },

           new int[3] { 07, 02, 03 },
           new int[3] { 00, 02, 07 }

           // Walls
           //new int[3] { 00, 06, 05 },
           //new int[3] { 00, 07, 06 },
           //new int[3] { 01, 03, 02 },
           //new int[3] { 01, 04, 03 },
        };

        static readonly Point SeeSawPosition = new Point(4454, -2226, 266);

        static void Main(string[] args)
        {
            for (int i = ushort.MinValue; i <= ushort.MaxValue; i += 16)
            {
                List<Point> gapPoints = SeeSawGapPoints((ushort)i);
                if (gapPoints.Count > 0)
                {
                    Console.WriteLine(String.Format("Angle {0} has potential:", i));
                    String.Join("\n", gapPoints);
                }
            }
            Console.WriteLine("Finished. Press any key to end...");
            Console.ReadLine();
        }

        static List<Point> SeeSawGapPoints(ushort angle)
        {
            List<Point> gapPoints = new List<Point>();
            List<Triangle> rotatedHitBox = CreateTriangles(SeeSawPoints, angle, 0, 0);
            for (int i = 0; i + 1 < rotatedHitBox.Count; i += 2)
            {
                gapPoints.AddRange(EdgeHasGap(rotatedHitBox[i], rotatedHitBox[i + 1]));
            }
            return gapPoints;
        }

        static bool inCCWTriangle(float x, float z, Triangle tri)
        {
            // (z1 - a3) * (x2 - x1) < (x1 - a1) * (z2 - z1) 
            return ((tri.Vertices[0].Z - z) * (tri.Vertices[1].X - tri.Vertices[0].X) <= (tri.Vertices[0].X - x) * (tri.Vertices[1].Z - tri.Vertices[0].Z)
                // (z2 - a3) * (x3 - x2) < (x2 - a1) * (z3 - z2)
                && (tri.Vertices[1].Z - z) * (tri.Vertices[2].X - tri.Vertices[1].X) <= (tri.Vertices[1].X - x) * (tri.Vertices[2].Z - tri.Vertices[1].Z)
                // (z3 - a3) * (x1 - x3) < (x3 - a1) * (z1 - z3)
                && (tri.Vertices[2].Z - z) * (tri.Vertices[0].X - tri.Vertices[2].X) <= (tri.Vertices[2].X - x) * (tri.Vertices[0].Z - tri.Vertices[2].Z));
        }

        static List<Triangle> CreateTriangles(List<Point> points, ushort angleX, ushort angleY, ushort angleZ)
        {
            float sx = sin(angleX);
            float cx = cos(angleX);
            float sy = sin(angleY);
            float cy = cos(angleY);
            float sz = sin(angleZ);
            float cz = cos(angleZ);

            float[,] transMatrix =
            {
                { sx*sy*sz + cy*cz,   -cy*sz + sx*sy*cz,  cx*sy,     },
                { cx*sz,              cx*cz,              -sx,       },
                { -sy*cz+sx*cy*sz,    sx*cy*cz+sy*sz,     cx*cy,     },
            };

            List<Point> transformedPoints = new List<Point>();
            foreach (var point in points)
            {
                transformedPoints.Add(new Point(
                    (short)Math.Truncate(SeeSawPosition.X + transMatrix[0, 0] * point.X + transMatrix[0, 1] * point.Y + transMatrix[0, 2] * point.Z),
                    (short)Math.Truncate(SeeSawPosition.Y + transMatrix[1, 0] * point.X + transMatrix[1, 1] * point.Y + transMatrix[1, 2] * point.Z),
                    (short)Math.Truncate(SeeSawPosition.Z + transMatrix[2, 0] * point.X + transMatrix[2, 1] * point.Y + transMatrix[2, 2] * point.Z)
                    ));
            }

            List<Triangle> triangles = new List<Triangle>();
            foreach (var vertextTriGroup in SeeSawTriangleVertexIndices)
            {
                Point p1 = transformedPoints[vertextTriGroup[0]];
                Point p2 = transformedPoints[vertextTriGroup[1]];
                Point p3 = transformedPoints[vertextTriGroup[2]];
                triangles.Add(new Triangle(p1, p2, p3));
            }

            return triangles;
        }

        static List<Point> EdgeHasGap(Triangle t1, Triangle t2)
        {
            // Find z and x coordinates
            List<Point> gapPoints = new List<Point>();
            short? t1x = common(t1.Vertices[0].X, t1.Vertices[1].X, t1.Vertices[2].X);
            short? t2x = common(t2.Vertices[0].X, t2.Vertices[1].X, t2.Vertices[2].X);
            if (!t1x.HasValue || !t2x.HasValue || Math.Abs(t1x.Value - t2x.Value) != 1023)
            {
                Trace.WriteLine("Weirdness Occured");
                Debugger.Break();
            }
            if (t1x > t2x)
            {
                short? temp = t1x;
                t1x = t2x;
                t2x = temp;
            }
            short? t1z = common(t1.Vertices[0].Z, t1.Vertices[1].Z, t1.Vertices[2].Z);
            short? t2z = common(t2.Vertices[0].Z, t2.Vertices[1].Z, t2.Vertices[2].Z);
            if (!t1z.HasValue || !t2z.HasValue || t1z != t2z)
            {
                Trace.WriteLine("Weirdness Occured");
                Debugger.Break();
            }
            short baseZ = t1z.Value;

            // Iterate over z and x values
            for (short x = (short)(t1x.Value - 2); x <= t2x.Value + 2; x++)
            {
                for (short z = (short)(baseZ - 2); z <= baseZ + 2; z++)
                {
                    if (PointHasGap(t1, t2, x, z))
                    {
                        gapPoints.Add(new Point(x, z, 0));
                    }
                }
            }

            return gapPoints;
        }

        static bool PointHasGap(Triangle t1, Triangle t2, short x, short z)
        {
            // Find floor and ceiling triangles (and verify a floor and ceiling triangle exist)
            Triangle floorTri, ceilTri;
            if (0.01 < t1.NormalY)
            {
                floorTri = t1;
                ceilTri = t2;
            }
            else
            {
                floorTri = t1;
                ceilTri = t2;
            }
            if (0.1 >= floorTri.NormalY || ceilTri.NormalY >= -0.01)
                return false;

            // Verify point in triangle
            if (!inCCWTriangle(x, z, floorTri))
                return false;
            if (!inCCWTriangle(x, z, ceilTri))
                return false;

            float floorY = triangleHeight(x, z, floorTri);
            float ceilY = triangleHeight(x, z, ceilTri);

            //if (ceilY - floorY <= 2)
                //return false;

            return true;
        }

        static short? common(short v1, short v2, short v3)
        {
            if (v1 == v2)
                return v1;

            if (v2 == v3)
                return v2;

            if (v1 == v3)
                return v3;

            return null;
        }

        static float triangleHeight(float x, float z, Triangle tri)
        {
            return -((x * tri.NormalX) + (z * tri.NormalZ) + tri.Offset) / tri.NormalY;
        }

        static float sin(ushort angle)
        {
            ushort truncAngle = (ushort)((angle / 16) * 16);
            return (float)Math.Sin(truncAngle / 65536d * Math.PI * 2);
        }

        static float cos(ushort angle)
        {
            ushort truncAngle = (ushort)((angle / 16) * 16);
            return (float)Math.Cos((truncAngle) / 65536d * Math.PI * 2);
        }
    }
}

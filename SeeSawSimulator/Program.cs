using System;
using System.Collections.Generic;
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
            List<Tuple<int, int>> gapEdgesAngles = new List<Tuple<int, int>>();
            List<Triangle> rotatedHitBox = CreateTriangles(SeeSawPoints, angle, 0, 0);
            for (int i = 0; i + 1 < rotatedHitBox.Count; i += 2)
            {
                if (EdgeHasGap(rotatedHitBox[i], rotatedHitBox[i + 1]))
                {
                    gapEdgesAngles.Add(i / 2);
                } 
            }
            return gapEdgesAngles;
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
            foreach(var point in points)
            {
                transformedPoints.Add(new Point(
                    (short)Math.Truncate(SeeSawPosition.X + transMatrix[0, 0] * point.X + transMatrix[0, 1] * point.Y + transMatrix[0, 2] * point.Z),
                    (short)Math.Truncate(SeeSawPosition.Y + transMatrix[1, 0] * point.X + transMatrix[1, 1] * point.Y + transMatrix[1, 2] * point.Z),
                    (short)Math.Truncate(SeeSawPosition.Z + transMatrix[2, 0] * point.X + transMatrix[2, 1] * point.Y + transMatrix[2, 2] * point.Z)
                    ));
            }

            List<Triangle> triangles = new List<Triangle>();
            foreach(var vertextTriGroup in SeeSawTriangleVertexIndices)
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
            //float yPos = t1.Vertices[0].Y + t1.Vertices

            return false;
        }

        static float triangleHeight(float x, float z, Triangle tri)
        {
            return -((tri.NormalX * x) + (tri.NormalZ * z) + tri.Offset) / tri.NormalY;
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

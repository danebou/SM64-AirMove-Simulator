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

            // Create triangle from three points
            public Triangle(Point p1, Point p2, Point p3)
            {
                Vertices = new Point[3] { p1, p2, p3 };

                // Calculate two lines to create triangle from
                Point line1 = p1 - p2;
                Point line2 = p1 - p3;

                // Calculate triangle normals
                float normX = line1.Y * line2.Z - line1.Z * line2.Y;
                float normY = line1.Z * line2.X - line1.X * line2.Z;
                float normZ = line1.X * line2.Y - line1.Y * line2.X;

                // Normalize normals (range from -1.0 to 1.0)
                float magn = (float)Math.Sqrt(normX * normX + normY * normY + normZ * normZ);
                NormalX = normX / magn;
                NormalY = normY / magn; 
                NormalZ = normZ / magn;

                // Calculate offset
                Offset = -(p1.X * NormalX + p1.Y * NormalY + p1.Z * NormalZ);
            }

            // Calculate if an x-z point is in a triangle (from above) (x-z only)
            public bool inCCWTriangle(float x, float z, bool reverse)
            {
                if (reverse)
                {
                    Triangle revTri = this;
                    revTri.Vertices[1] = Vertices[2];
                    revTri.Vertices[2] = Vertices[1];
                    return revTri.inCCWTriangle(x, z, false);
                }

                // (z1 - z) * (x2 - x1) <= (x1 - x) * (z2 - z1) 
                return ((Vertices[0].Z - z) * (Vertices[1].X - Vertices[0].X) <= (Vertices[0].X - x) * (Vertices[1].Z - Vertices[0].Z)
                    // (z2 - z) * (x3 - x2) <= (x2 - x) * (z3 - z2)
                    && (Vertices[1].Z - z) * (Vertices[2].X - Vertices[1].X) <= (Vertices[1].X - x) * (Vertices[2].Z - Vertices[1].Z)
                    // (z3 - z) * (x1 - x3) <= (x3 - x) * (z1 - z3)
                    && (Vertices[2].Z - z) * (Vertices[0].X - Vertices[2].X) <= (Vertices[2].X - x) * (Vertices[0].Z - Vertices[2].Z));
            }
        }

        // List of points of the see-saw that make up the cube
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

        // List of triangles that make up. Each pair of triangles intersects on an edge
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

        // Position of the seesaws
        static readonly Point SeeSawPosition = new Point(4454, -2226, 266); // First seesaw
        //static readonly Point SeeSawPosition = new Point(5786, -2380, 266); // Second seesaw                                                            

        static void Main(string[] args)
        {
            bool foundRlbPoint = false;

            // Check every angle for RLB points
            for (int i = ushort.MinValue; i <= ushort.MaxValue; i += 16) // Only angles divisible by 16 matter beacause of truncated trig table
            {
                // Look for ponts that could trigger an RLB
                List<Point> gapPoints = SeeSawGapPoints((ushort)i);

                // There is potential if there points where found
                if (gapPoints.Count > 0)
                {
                    foundRlbPoint = true;
                    Console.WriteLine(String.Format("Angle {0} has potential:", i));
                    Console.WriteLine(String.Join("\n", gapPoints));
                }
            }
            Console.WriteLine(!foundRlbPoint ? "Finished :(" : "Finsihed :)");
            Console.WriteLine("Press enter to end...");
            Console.ReadLine();
        }

        static List<Point> SeeSawGapPoints(ushort angle)
        {
            List<Point> gapPoints = new List<Point>();

            // Generate hitbox (triangles)
            List<Triangle> rotatedHitBox = CreateTriangles(SeeSawPoints, angle, 0, 0);

            // Group triangles in pairs that contain an edge
            for (int i = 0; i + 1 < rotatedHitBox.Count; i += 2)
            {
                // Look for RLB points near that edge. (add to list if any exist)
                gapPoints.AddRange(EdgeHasGap(rotatedHitBox[i], rotatedHitBox[i + 1]));
            }

            return gapPoints;
        }

        // Create triangles from a set of points and point index list
        static List<Triangle> CreateTriangles(List<Point> points, ushort angleX, ushort angleY, ushort angleZ)
        {
            // Calculate sines and cosines
            float sx = sin(angleX);
            float cx = cos(angleX);
            float sy = sin(angleY);
            float cy = cos(angleY);
            float sz = sin(angleZ);
            float cz = cos(angleZ);

            // Generate transformation matrix
            float[,] transMatrix =
            {
                { sx*sy*sz + cy*cz,   -cy*sz + sx*sy*cz,  cx*sy,     },
                { cx*sz,              cx*cz,              -sx,       },
                { -sy*cz+sx*cy*sz,    sx*cy*cz+sy*sz,     cx*cy,     },
            };

            // Transform each point (rotate and add offset)
            List<Point> transformedPoints = new List<Point>();
            foreach (var point in points)
            {
                transformedPoints.Add(new Point(
                    (short)Math.Truncate(SeeSawPosition.X + transMatrix[0, 0] * point.X + transMatrix[0, 1] * point.Y + transMatrix[0, 2] * point.Z),
                    (short)Math.Truncate(SeeSawPosition.Y + transMatrix[1, 0] * point.X + transMatrix[1, 1] * point.Y + transMatrix[1, 2] * point.Z),
                    (short)Math.Truncate(SeeSawPosition.Z + transMatrix[2, 0] * point.X + transMatrix[2, 1] * point.Y + transMatrix[2, 2] * point.Z)
                    ));
            }

            // Create triangles using list
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

        // Determine if and edge (floor and ceiling triangle) have an RLB gap
        static List<Point> EdgeHasGap(Triangle t1, Triangle t2)
        {
            List<Point> gapPoints = new List<Point>();

            // Find x coordinates of edge (start and stop)
            // All edges run parallel to x axis
            short? t1x = common(t1.Vertices[0].X, t1.Vertices[1].X, t1.Vertices[2].X);
            short? t2x = common(t2.Vertices[0].X, t2.Vertices[1].X, t2.Vertices[2].X);

            // Expect that each triangle has a pair of vertices which share the same x value
            // Also test that the edge length is expected to be 1023.
            // This is not needed, its just a test to make sure everything is going as expected
            if (!t1x.HasValue || !t2x.HasValue || Math.Abs(t1x.Value - t2x.Value) != 1023)
            {
                Trace.WriteLine("Weirdness Occured");
                Debugger.Break();
            }

            // Make t1x be the smallest x coordinate (swap if needed)
            if (t1x > t2x)
            {
                // Swap t1x and t2x
                short? temp = t1x;
                t1x = t2x;
                t2x = temp;
            }

            // Find the z coordinate of the edge
            short? t1z = common(t1.Vertices[0].Z, t1.Vertices[1].Z, t1.Vertices[2].Z);
            short? t2z = common(t2.Vertices[0].Z, t2.Vertices[1].Z, t2.Vertices[2].Z);

            // Expect that each triangle has a pair of vertices which share the same z value
            // Also test that both triangles' individual edge has the same z coordinate (the edges should be the same edge)
            // This is not needed, its just a test to make sure everything is going as expected
            if (!t1z.HasValue || !t2z.HasValue || t1z != t2z)
            {
                Trace.WriteLine("Weirdness Occured");
                Debugger.Break();
            }
            short edgeZ = t1z.Value; // Get the final edge z coordinate (since t1z and t2z are the same)

            // Iterate over z and x values
            // Check +- 2 positions over edge z and +- 2 positions edge x
            for (short x = (short)(t1x.Value - 2); x <= t2x.Value + 2; x++) // Check all positions on edge line 
            {
                for (short z = (short)(edgeZ - 2); z <= edgeZ + 2; z++) // Check positions above and below edge line
                {
                    // Test point for RLB
                    if (PointHasGap(t1, t2, x, z))
                    {
                        // RLB point found. Add to list
                        gapPoints.Add(new Point(x, z, 0));
                    }
                }
            }

            return gapPoints;
        }

        // Determine if a point has an RLB gap
        static bool PointHasGap(Triangle t1, Triangle t2, short x, short z)
        {
            // Find floor and ceiling triangles
            Triangle floorTri, ceilTri;
            if (0.01 < t1.NormalY) // First triangle is a floor
            {
                floorTri = t1;
                ceilTri = t2;
            }
            else // First triangle is not a floor
            {
                floorTri = t1;
                ceilTri = t2;
            }

            // Verify one triangle is a floor and the other triangle is a ceiling
            // (from this point we only ASSUMED one was a floor and the other was a ceiling)
            if (0.01 >= floorTri.NormalY || ceilTri.NormalY >= -0.01)
                return false;

            // Verify point in triangle triangles (x-z)
            if (!floorTri.inCCWTriangle(x, z, true))
                return false;
            if (!ceilTri.inCCWTriangle(x, z, false))
                return false;

            // Get the height of point projected onto each triangle
            float floorY = triangleHeight(x, z, floorTri);
            float ceilY = triangleHeight(x, z, ceilTri);

            // Check that the gap size is less than (or equal to 2)
            if (ceilY - floorY <= 2)
                return false;

            // All conditions for RLB met
            return true;
        }

        // Get two common shorts from a set of three shorts. 
        // If all shorts are different null is returned.
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

        // Calculate the height of a point (y coordinate) projected onto a triangle from
        // the x and z coordinates
        static float triangleHeight(float x, float z, Triangle tri)
        {
            return -((x * tri.NormalX) + (z * tri.NormalZ) + tri.Offset) / tri.NormalY;
        }

        // Truncated sine
        static float sin(ushort angle)
        {
            ushort truncAngle = (ushort)((angle / 16) * 16);
            return (float)Math.Sin(truncAngle / 65536d * Math.PI * 2);
        }

        // Truncated cosine
        static float cos(ushort angle)
        {
            ushort truncAngle = (ushort)((angle / 16) * 16);
            return (float)Math.Cos((truncAngle) / 65536d * Math.PI * 2);
        }
    }
}

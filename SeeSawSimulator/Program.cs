using MathNet.Numerics.LinearAlgebra;
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
        }

        struct Triangle
        {
            public Point[] Vertices;
            public float Offset;
            public float NormalY;
        }

        static readonly Triangle[] SeeSawTriangles =
        {

        };

        static readonly Point SeeSawPosition = new Point(4454, -2226, 266);

        static void Main(string[] args)
        {
            for (int i = short.MinValue; i <= short.MaxValue; i++)
            {
                List<int> edgeNumbers = SeeSawHasGapAtEdge((short)i);
                if (edgeNumbers.Count > 0)
                {
                    Console.WriteLine(String.Format("Angle {0} has potential at edge(s) {1}", i, String.Join(", ", edgeNumbers.ToString())));
                }
            }
            Console.WriteLine("Finished. Press any key to end...");
            Console.ReadLine();
        }

        static List<int> SeeSawHasGapAtEdge(short angle)
        {
            List<int> gapEdges = new List<int>();
            Triangle[] rotatedHitBox = RotateTriangles(SeeSawTriangles, new Point(angle, 0, 0);
            for (int i = 0; i + 1 < rotatedHitBox.Length; i += 2)
            {
                if (EdgeHasGap(rotatedHitBox[i], rotatedHitBox[i + 1]))
                {
                    gapEdges.Add(i / 2);
                } 
            }
            return gapEdges;
        }

        static Triangle[] RotateTriangles(Triangle[] triangles, Point angles)
        {
            float sx = sin(angles.X);
            float cx = cos(angles.X);
            float sy = sin(angles.Y);
            float cy = cos(angles.Y);
            float sz = sin(angles.Z);
            float cz = cos(angles.Z);

            float[,] transMatrix =
            {
                { sx*sy*sz + cy*cz,   -cy*sz + sx*sy*cz,  cx*sy,    SeeSawPosition.X },
                { cx*sz,              cx*cz,              -sx,      SeeSawPosition.Y },
                { -sy*cz+sx*cy*sz,    sx*cy*cz+sy*sz,     cx*cy,    SeeSawPosition.Z },
                { 0,                  0,                  0,        1 }
            };

            return null;
        }

        static float sin(short angle)
        {
            return (float)Math.Sin((ushort)(angle) / 65536d * Math.PI * 2);
        }

        static float cos(short angle)
        {
            return (float)Math.Cos((ushort)(angle) / 65536d * Math.PI * 2);
        }

        static bool EdgeHasGap(Triangle t1, Triangle t2)
        {
            return false;
        }
}

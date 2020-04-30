using System.Numerics;

namespace YALCT
{
    public struct VertexPosition
    {
        public Vector3 Position;

        public const uint SizeInBytes = 12;

        public VertexPosition(Vector3 position)
        {
            Position = position;
        }
    }
}
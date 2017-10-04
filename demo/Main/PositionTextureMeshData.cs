using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Veldrid.Graphics;

namespace Veldrid.NeoDemo
{
    public static partial class PrimitiveShapes
    {
        internal class PositionTextureMeshData : MeshData
        {
            public readonly VertexPositionTexture[] Vertices;
            public readonly ushort[] Indices;

            public PositionTextureMeshData(VertexPositionTexture[] vertices, ushort[] indices)
            {
                Vertices = vertices;
                Indices = indices;
            }

            public IndexBuffer CreateIndexBuffer(ResourceFactory factory, out int indexCount)
            {
                IndexBuffer ret = factory.CreateIndexBuffer(Indices, false);
                indexCount = Indices.Length;
                return ret;
            }

            public VertexBuffer CreateVertexBuffer(ResourceFactory factory)
            {
                return factory.CreateVertexBuffer(
                    Vertices,
                    new VertexDescriptor(VertexPositionTexture.SizeInBytes, VertexPositionTexture.ElementCount),
                    false);
            }

            public unsafe BoundingBox GetBoundingBox()
            {
                fixed (VertexPositionTexture* vertexPtr = &Vertices[0])
                {
                    Vector3* positionPtr = (Vector3*)vertexPtr;
                    return BoundingBox.CreateFromVertices(
                        positionPtr,
                        Vertices.Length,
                        VertexPositionTexture.SizeInBytes,
                        Quaternion.Identity,
                        Vector3.Zero,
                        Vector3.One);
                }
            }

            public unsafe BoundingSphere GetBoundingSphere()
            {
                fixed (VertexPositionTexture* vertexPtr = &Vertices[0])
                {
                    Vector3* positionPtr = (Vector3*)vertexPtr;
                    return BoundingSphere.CreateFromPoints(positionPtr, Vertices.Length, VertexPositionTexture.SizeInBytes);
                }
            }

            public ushort[] GetIndices()
            {
                return Indices;
            }

            public Vector3[] GetVertexPositions()
            {
                return Vertices.Select(vpt => vpt.Position).ToArray();
            }

            public bool RayCast(Ray ray, out float distance)
            {
                distance = float.MaxValue;
                bool result = false;
                for (int i = 0; i < Indices.Length - 2; i += 3)
                {
                    Vector3 v0 = Vertices[Indices[i + 0]].Position;
                    Vector3 v1 = Vertices[Indices[i + 1]].Position;
                    Vector3 v2 = Vertices[Indices[i + 2]].Position;

                    float newDistance;
                    if (ray.Intersects(ref v0, ref v1, ref v2, out newDistance))
                    {
                        if (newDistance < distance)
                        {
                            distance = newDistance;
                        }

                        result = true;
                    }
                }

                return result;
            }

            public int RayCast(Ray ray, List<float> distances)
            {
                int hits = 0;
                for (int i = 0; i < Indices.Length - 2; i += 3)
                {
                    Vector3 v0 = Vertices[Indices[i + 0]].Position;
                    Vector3 v1 = Vertices[Indices[i + 1]].Position;
                    Vector3 v2 = Vertices[Indices[i + 2]].Position;

                    float newDistance;
                    if (ray.Intersects(ref v0, ref v1, ref v2, out newDistance))
                    {
                        hits++;
                        distances.Add(newDistance);
                    }
                }

                return hits;
            }
        }
    }
}

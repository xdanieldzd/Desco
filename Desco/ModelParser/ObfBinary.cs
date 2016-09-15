using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using OpenTK;
using OpenTK.Graphics;

using Cobalt.IO;
using Cobalt.Mesh;

using Desco.Conversion;

namespace Desco.ModelParser
{
    public class ObfBinary : ParsableData
    {
        public uint FileSize { get; private set; }
        public uint Unknown0x04 { get; private set; }
        public uint UnknownPointer { get; private set; }
        public uint ModelListPointer { get; private set; }
        public uint ObjectListPointer { get; private set; }
        public uint NodeListPointer { get; private set; }
        public uint TransformIndicesPointer { get; private set; }
        public uint TransformDataPointer { get; private set; }
        public uint GroupListPointer { get; private set; }
        public uint PrimitiveListPointer { get; private set; }
        public uint VertexDataPointer { get; private set; }
        public uint TextureListPointer { get; private set; }

        public Node[] Nodes { get; private set; }
        public Group[] Groups { get; private set; }
        public Primitive[] Primitives { get; private set; }
        public Vertex[] Vertices { get; private set; }
        public Texture[] Textures { get; private set; }

        Cobalt.Texture.Texture[] convTextures;
        NodeTransformData[][] nodeTransforms;

        public ObfBinary(Stream stream) : base(stream) { }

        public override void ReadFromStream(Stream stream)
        {
            EndianBinaryReader reader = new EndianBinaryReader(stream, Program.Endianness);

            FileSize = reader.ReadUInt32();
            Unknown0x04 = reader.ReadUInt32();
            UnknownPointer = reader.ReadUInt32();
            ModelListPointer = reader.ReadUInt32();
            ObjectListPointer = reader.ReadUInt32();
            NodeListPointer = reader.ReadUInt32();
            TransformIndicesPointer = reader.ReadUInt32();
            TransformDataPointer = reader.ReadUInt32();
            GroupListPointer = reader.ReadUInt32();
            PrimitiveListPointer = reader.ReadUInt32();
            VertexDataPointer = reader.ReadUInt32();
            TextureListPointer = reader.ReadUInt32();

            Nodes = GetArray<Node>(stream, NodeListPointer);
            Groups = GetArray<Group>(stream, GroupListPointer);
            Primitives = GetArray<Primitive>(stream, PrimitiveListPointer);
            Vertices = GetVertices(stream, VertexDataPointer, (int)Primitives.Max(x => x.VertexIndices.Max()) + 1);
            Textures = GetArray<Texture>(stream, TextureListPointer);

            convTextures = new Cobalt.Texture.Texture[Textures.Length];
            for (int i = 0; i < Textures.Length; i++)
                convTextures[i] = new Cobalt.Texture.Texture(Textures[i].Image, OpenTK.Graphics.OpenGL.TextureWrapMode.Repeat, OpenTK.Graphics.OpenGL.TextureWrapMode.Repeat, OpenTK.Graphics.OpenGL.TextureMinFilter.Linear, OpenTK.Graphics.OpenGL.TextureMagFilter.Linear);

            nodeTransforms = new NodeTransformData[Nodes.Length][];
            for (int i = 0; i < Nodes.Length; i++)
            {
                nodeTransforms[i] = new NodeTransformData[Nodes[i].NumTransformIndices];
                for (int j = 0; j < nodeTransforms[i].Length; j++)
                {
                    TransformIndices indices = GetTransformIndices(stream, TransformIndicesPointer, Nodes[i].TransformIndices[j]);

                    NodeTransformData nodeTransform = new NodeTransformData(
                        GetTransformData(stream, TransformDataPointer, indices.TranslationXIndex),
                        GetTransformData(stream, TransformDataPointer, indices.TranslationYIndex),
                        GetTransformData(stream, TransformDataPointer, indices.TranslationZIndex),
                        GetTransformData(stream, TransformDataPointer, indices.RotationXIndex),
                        GetTransformData(stream, TransformDataPointer, indices.RotationYIndex),
                        GetTransformData(stream, TransformDataPointer, indices.RotationZIndex),
                        GetTransformData(stream, TransformDataPointer, indices.ScaleXIndex),
                        GetTransformData(stream, TransformDataPointer, indices.ScaleYIndex),
                        GetTransformData(stream, TransformDataPointer, indices.ScaleZIndex));

                    nodeTransforms[i][j] = nodeTransform;
                }
            }
        }

        private T[] GetArray<T>(Stream stream, uint pointer) where T : ParsableData, IComponent
        {
            long lastPosition = stream.Position;
            EndianBinaryReader reader = new EndianBinaryReader(stream, Program.Endianness);
            stream.Seek(pointer, SeekOrigin.Begin);

            T[] elements = new T[reader.ReadUInt32()];
            for (int i = 0; i < elements.Length; i++)
            {
                stream.Seek((pointer + 0x04) + (i * 0x04), SeekOrigin.Begin);
                stream.Seek(pointer + reader.ReadUInt32(), SeekOrigin.Begin);
                elements[i] = (T)Activator.CreateInstance(typeof(T), new object[] { stream });
            }

            stream.Position = lastPosition;
            return elements;
        }

        private Vertex[] GetVertices(Stream stream, uint pointer, int numVertices)
        {
            long lastPosition = stream.Position;
            EndianBinaryReader reader = new EndianBinaryReader(stream, Program.Endianness);
            stream.Seek(pointer, SeekOrigin.Begin);

            /* OpenGL is right-handed, Obf is left-handed; to convert, invert Z axis */

            Vertex[] vertices = new Vertex[numVertices];
            for (int i = 0; i < numVertices; i++)
            {
                Vector3 position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), -reader.ReadSingle());
                Vector3 normal = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                Vector3 unknown = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                float a = reader.ReadSingle(), r = reader.ReadSingle(), g = reader.ReadSingle(), b = reader.ReadSingle();
                Color4 color = new Color4(r, g, b, a);
                Vector2 texCoord = new Vector2(reader.ReadSingle(), reader.ReadSingle());

                Vertex vertex = new Vertex() { Position = position, Normal = normal, Unknown = unknown, Color = color, TexCoord = texCoord };
                vertices[i] = vertex;
            }
            stream.Position = lastPosition;
            return vertices;
        }

        private TransformIndices GetTransformIndices(Stream stream, uint pointer, uint indicesIndex)
        {
            long lastPosition = stream.Position;
            stream.Seek(pointer + (indicesIndex * 0x28), SeekOrigin.Begin);

            TransformIndices indices = new TransformIndices(stream);

            stream.Position = lastPosition;
            return indices;
        }

        private TransformData GetTransformData(Stream stream, uint pointer, int dataIndex)
        {
            long lastPosition = stream.Position;
            stream.Seek(pointer + (dataIndex * 0x10), SeekOrigin.Begin);

            TransformData data = new TransformData(stream);

            stream.Position = lastPosition;
            return data;
        }

        public Dictionary<Tuple<Node, Group, Primitive>, Mesh> GetMeshes()
        {
            Dictionary<Tuple<Node, Group, Primitive>, Mesh> meshes = new Dictionary<Tuple<Node, Group, Primitive>, Mesh>();

            for (int n = 0; n < Nodes.Length; n++)
            {
                Node node = Nodes[n];
                if (node.GroupIndex == -1) continue;

                Group group = Groups[node.GroupIndex];

                for (int p = 0; p < group.PrimitiveIndices.Length; p++)
                {
                    if (group.PrimitiveIndices[p] >= 0 && group.PrimitiveIndices[p] < Primitives.Length)
                    {
                        Mesh mesh = new Mesh();

                        Primitive primitive = Primitives[group.PrimitiveIndices[p]];

                        Vertex[] primVertices = new Vertex[primitive.VertexIndices.Length];
                        for (int v = 0; v < primVertices.Length; v++) primVertices[v] = Vertices[primitive.VertexIndices[v]];
                        mesh.SetVertexData<Vertex>(primVertices);

                        if (primitive.TextureIndex >= 0 && primitive.TextureIndex < Textures.Length)
                            mesh.SetMaterial(new Material(convTextures[primitive.TextureIndex]));

                        meshes.Add(new Tuple<Node, Group, Primitive>(node, group, primitive), mesh);
                    }
                }
            }

            return meshes;
        }

        public NodeTransformData GetNodeTransform(Node node, int idx)
        {
            int nodeIdx = Array.IndexOf(Nodes, node);
            return nodeTransforms[nodeIdx][idx];
        }
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
    public struct Vertex : IVertexStruct
    {
        [VertexElement(AttributeIndex = 0)]
        public Vector3 Position;
        [VertexElement(AttributeIndex = 1)]
        public Vector3 Normal;
        [VertexElement(AttributeIndex = 2)]
        public Vector3 Unknown;
        [VertexElement(AttributeIndex = 3)]
        public Color4 Color;
        [VertexElement(AttributeIndex = 4)]
        public Vector2 TexCoord;
    }
}

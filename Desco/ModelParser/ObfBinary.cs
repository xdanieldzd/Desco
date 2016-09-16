using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using Cobalt;
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
        public uint AssetListPointer { get; private set; }
        public uint ModelListPointer { get; private set; }
        public uint NodeListPointer { get; private set; }
        public uint TransformIndicesPointer { get; private set; }
        public uint TransformDataPointer { get; private set; }
        public uint GroupListPointer { get; private set; }
        public uint PrimitiveListPointer { get; private set; }
        public uint VertexDataPointer { get; private set; }
        public uint TextureListPointer { get; private set; }

        public Dictionary<short, ICollection<short>> Assets { get; private set; }
        public Model[] Models { get; private set; }
        public Node[] Nodes { get; private set; }
        public Group[] Groups { get; private set; }
        public Primitive[] Primitives { get; private set; }
        public Vertex[] Vertices { get; private set; }
        public Texture[] Textures { get; private set; }

        Cobalt.Texture.Texture[] convTextures;
        Dictionary<Primitive, Mesh> primitiveMeshes;

        NodeTransformData[][] nodeTransforms;

        public ObfBinary(Stream stream) : base(stream) { }

        public override void ReadFromStream(Stream stream)
        {
            EndianBinaryReader reader = new EndianBinaryReader(stream, Program.Endianness);

            /* Read header */
            FileSize = reader.ReadUInt32();
            Unknown0x04 = reader.ReadUInt32();
            UnknownPointer = reader.ReadUInt32();
            AssetListPointer = reader.ReadUInt32();
            ModelListPointer = reader.ReadUInt32();
            NodeListPointer = reader.ReadUInt32();
            TransformIndicesPointer = reader.ReadUInt32();
            TransformDataPointer = reader.ReadUInt32();
            GroupListPointer = reader.ReadUInt32();
            PrimitiveListPointer = reader.ReadUInt32();
            VertexDataPointer = reader.ReadUInt32();
            TextureListPointer = reader.ReadUInt32();

            /* Read data */
            Assets = BinaryHelpers.GetMultiDictionary<short, short>(stream, AssetListPointer);
            Models = BinaryHelpers.GetArray<Model>(stream, ModelListPointer);
            Nodes = BinaryHelpers.GetArray<Node>(stream, NodeListPointer);
            Groups = BinaryHelpers.GetArray<Group>(stream, GroupListPointer);
            Primitives = BinaryHelpers.GetArray<Primitive>(stream, PrimitiveListPointer);
            Vertices = GetVertices(stream, VertexDataPointer, (int)Primitives.Max(x => x.VertexIndices.Max()) + 1);
            Textures = BinaryHelpers.GetArray<Texture>(stream, TextureListPointer);

            /* Generate textures */
            convTextures = new Cobalt.Texture.Texture[Textures.Length];
            for (int i = 0; i < Textures.Length; i++)
                convTextures[i] = new Cobalt.Texture.Texture(Textures[i].Image, OpenTK.Graphics.OpenGL.TextureWrapMode.Repeat, OpenTK.Graphics.OpenGL.TextureWrapMode.Repeat, OpenTK.Graphics.OpenGL.TextureMinFilter.Linear, OpenTK.Graphics.OpenGL.TextureMagFilter.Linear);

            /* Generate transforms */
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

            primitiveMeshes = new Dictionary<Primitive, Mesh>();
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

        public void RenderAssets(Shader shader)
        {
            foreach (KeyValuePair<short, ICollection<short>> asset in Assets)
            {
                RenderAsset(asset.Key, shader);
            }
        }

        public void RenderAsset(short assetId, Shader shader)
        {
            GL.FrontFace(FrontFaceDirection.Cw);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

            // TODO: bounds checking

            foreach (short modelId in Assets[assetId])
            {
                Model model = Models[modelId];
                foreach (uint nodeIdx in model.NodeIndices)
                    RenderNode((int)model.NodeIndices[0], (int)nodeIdx, shader);
            }

            GL.FrontFace(FrontFaceDirection.Ccw);
        }

        private void RenderNode(int baseNodeIdx, int nodeIdx, Shader shader)
        {
            Node node = Nodes[nodeIdx];

            shader.SetUniformMatrix("node_matrix", false, GetTransformationMatrix(baseNodeIdx, nodeIdx));

            if (node.GroupIndex != -1)
            {
                Group group = Groups[node.GroupIndex];

                Vector2 texAnim = new Vector2(group.TextureAnimationOffsetX, group.TextureAnimationOffsetY);
                shader.SetUniform("texCoord_offset", texAnim);

                IEnumerable<Mesh> meshes = GetMeshes(group);
                foreach (Mesh mesh in meshes) mesh.Render();
            }
        }

        float blend = 0.0f;

        // garbage test
        OpenTK.Input.KeyboardState tmpLastKbd;
        int tmpTransformIdx = 0;
        // garbage test

        private Matrix4 GetTransformationMatrix(int baseNodeIdx, int nodeIdx)
        {
            Node node = Nodes[nodeIdx];

            Vector3[] scales = new Vector3[2];
            Vector3[] rotations = new Vector3[2];
            Vector3[] translations = new Vector3[2];

            for (int i = 0; i < 2; i++)
            {
                int transformIdx = (int)((Core.DeltaTime / 16.0f) + i);

                transformIdx = tmpTransformIdx + i;

                if (transformIdx >= nodeTransforms[nodeIdx].Length) transformIdx %= nodeTransforms[nodeIdx].Length;

                NodeTransformData nodeTransform = nodeTransforms[nodeIdx][transformIdx];

                scales[i] = new Vector3(nodeTransform.ScaleX.Value0x00, nodeTransform.ScaleY.Value0x00, nodeTransform.ScaleZ.Value0x00);
                rotations[i] = new Vector3(nodeTransform.RotationX.Value0x00, nodeTransform.RotationY.Value0x00, nodeTransform.RotationZ.Value0x00);
                translations[i] = new Vector3(nodeTransform.TranslationX.Value0x00, nodeTransform.TranslationY.Value0x00, -nodeTransform.TranslationZ.Value0x00) * 10.0f;
            }

            // garbage test
            OpenTK.Input.KeyboardState tmpKbd = OpenTK.Input.Keyboard.GetState();
            if (tmpKbd[OpenTK.Input.Key.KeypadPlus]) blend += 0.000005f;
            if (tmpKbd[OpenTK.Input.Key.KeypadMinus]) blend -= 0.000005f;
            if (blend < 0.0f) blend = 0.0f;
            if (blend > 1.0f) blend = 1.0f;
            if (tmpKbd[OpenTK.Input.Key.Keypad7] && !tmpLastKbd[OpenTK.Input.Key.Keypad7]) tmpTransformIdx--;
            if (tmpKbd[OpenTK.Input.Key.Keypad9] && !tmpLastKbd[OpenTK.Input.Key.Keypad9]) tmpTransformIdx++;
            tmpLastKbd = tmpKbd;
            // garbage test

            Matrix4 localMatrix = Matrix4.Identity;
            localMatrix *= Matrix4.CreateScale(Vector3.Lerp(scales[0], scales[1], blend));
            localMatrix *= Matrix4.CreateRotationX(Vector3.Lerp(rotations[0], rotations[1], blend).X);
            localMatrix *= Matrix4.CreateRotationY(Vector3.Lerp(rotations[0], rotations[1], blend).Y);
            localMatrix *= Matrix4.CreateRotationZ(Vector3.Lerp(rotations[0], rotations[1], blend).Z);
            localMatrix *= Matrix4.CreateTranslation(Vector3.Lerp(translations[0], translations[1], blend));

            if (node.RelatedNodeIndex != -1)
            {
                int relativeNodeIdx = baseNodeIdx + node.RelatedNodeIndex;
                if (relativeNodeIdx != nodeIdx) localMatrix *= GetTransformationMatrix(baseNodeIdx, relativeNodeIdx);
            }

            return localMatrix;
        }

        private IEnumerable<Mesh> GetMeshes(Group group)
        {
            List<Mesh> meshes = new List<Mesh>();

            for (int p = 0; p < group.PrimitiveIndices.Length; p++)
                if (group.PrimitiveIndices[p] >= 0 && group.PrimitiveIndices[p] < Primitives.Length)
                    meshes.Add(GetMesh(Primitives[group.PrimitiveIndices[p]]));

            return meshes;
        }

        private Mesh GetMesh(Primitive primitive)
        {
            if (!primitiveMeshes.ContainsKey(primitive))
            {
                Mesh mesh = new Mesh();

                Vertex[] primVertices = new Vertex[primitive.VertexIndices.Length];
                for (int v = 0; v < primVertices.Length; v++) primVertices[v] = Vertices[primitive.VertexIndices[v]];
                mesh.SetVertexData<Vertex>(primVertices);

                if (primitive.TextureIndex >= 0 && primitive.TextureIndex < Textures.Length)
                    mesh.SetMaterial(new Material(convTextures[primitive.TextureIndex]));

                primitiveMeshes.Add(primitive, mesh);
            }

            return primitiveMeshes[primitive];
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

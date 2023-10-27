using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Obi
{
    [Serializable]
    public class HalfEdgeMesh
    {

        public Mesh inputMesh;
        public Vector3 scale = Vector3.one;

        private float _area = 0;
        private float _volume = 0;


        [Serializable]
        public struct HalfEdge
        {
            public int index;
            public int indexInFace;
            public int face;
            public int nextHalfEdge;
            public int pair;
            public int endVertex;
        }

        [Serializable]
        public struct Vertex
        {
            public int index;
            public int halfEdge;
            public Vector3 position;
        }

        [Serializable]
        public struct Face
        {
            public int index;
            public int halfEdge;
        }

        [SerializeField] [HideInInspector] public bool containsData = false;
        [SerializeField] [HideInInspector] public List<Vertex> vertices = new List<Vertex>();
        [SerializeField] [HideInInspector] public List<HalfEdge> halfEdges = new List<HalfEdge>();
        [SerializeField] [HideInInspector] public List<HalfEdge> borderEdges = new List<HalfEdge>();
        [SerializeField] [HideInInspector] public List<Face> faces = new List<Face>();
        [SerializeField] [HideInInspector] public List<Vector3> restNormals = new List<Vector3>();
        [SerializeField] [HideInInspector] public List<Quaternion> restOrientations = new List<Quaternion>();
        [SerializeField] [HideInInspector] public List<int> rawToWelded = new List<int>();

        public bool ContainsData
        {
            get { return containsData; }
        }

        public bool closed
        {
            get { return borderEdges.Count == 0; }
        }

        public float area
        {
            get { return _area; }
        }

        public float volume
        {
            get { return _volume; }
        }

        public HalfEdgeMesh() { }

        public HalfEdgeMesh(HalfEdgeMesh halfEdge)
        {
            this.containsData = halfEdge.containsData;
            this.inputMesh = halfEdge.inputMesh;
            this.scale = halfEdge.scale;

            this.vertices = new List<Vertex>(halfEdge.vertices);
            this.halfEdges = new List<HalfEdge>(halfEdge.halfEdges);
            this.borderEdges = new List<HalfEdge>(halfEdge.borderEdges);
            this.faces = new List<Face>(halfEdge.faces);
            this.restNormals = new List<Vector3>(halfEdge.restNormals);
            this.restOrientations = new List<Quaternion>(halfEdge.restOrientations);
            this.rawToWelded = new List<int>(halfEdge.rawToWelded);
        }

        public void Generate()
        {
            containsData = false;
            vertices = new List<Vertex>();
            halfEdges = new List<HalfEdge>();
            borderEdges = new List<HalfEdge>();
            faces = new List<Face>();
            restNormals = new List<Vector3>();
            restOrientations = new List<Quaternion>();
            rawToWelded = new List<int>();

            _area = 0;
            _volume = 0;

            var vertexBuffer = new Dictionary<Vector3, Vertex>();
            var edgeBuffer = new Dictionary<Vector2Int, HalfEdge>();

            // Merge vertices together based on proximity:
            Vector3[] inputVertices = inputMesh.vertices;

            for (int i = 0; i < inputVertices.Length; ++i)
            {
                Vertex v;
                Vector3 position = Vector3.Scale(inputVertices[i], scale);

                if (!vertexBuffer.TryGetValue(position, out v))
                {
                    v = new Vertex();
                    v.position = position;
                    v.index = vertices.Count;
                    vertexBuffer.Add(position, v);
                    vertices.Add(v);
                }

                rawToWelded.Add(v.index);
            }

            // Build half-edges and faces:
            int[] inputTriangles = inputMesh.triangles;
            for (int i = 0; i < inputTriangles.Length; i += 3)
            {
                int i1 = inputTriangles[i];
                int i2 = inputTriangles[i + 1];
                int i3 = inputTriangles[i + 2];

                Vector3 p1 = inputVertices[i1];
                Vector3 p2 = inputVertices[i2];
                Vector3 p3 = inputVertices[i3];

                Vertex v1 = vertices[rawToWelded[i1]];
                Vertex v2 = vertices[rawToWelded[i2]];
                Vertex v3 = vertices[rawToWelded[i3]];

                HalfEdge e1 = new HalfEdge();
                e1.index = halfEdges.Count;
                e1.indexInFace = 0;
                e1.face = faces.Count;
                e1.endVertex = v1.index;

                HalfEdge e2 = new HalfEdge();
                e2.index = halfEdges.Count + 1;
                e2.indexInFace = 1;
                e2.face = faces.Count;
                e2.endVertex = v2.index;

                HalfEdge e3 = new HalfEdge();
                e3.index = halfEdges.Count + 2;
                e3.indexInFace = 2;
                e3.face = faces.Count;
                e3.endVertex = v3.index;

                e1.nextHalfEdge = e2.index;
                e2.nextHalfEdge = e3.index;
                e3.nextHalfEdge = e1.index;

                v1.halfEdge = e2.index;
                v2.halfEdge = e3.index;
                v3.halfEdge = e1.index;

                vertices[rawToWelded[i1]] = v1;
                vertices[rawToWelded[i2]] = v2;
                vertices[rawToWelded[i3]] = v3;

                Vector2Int pair1 = new Vector2Int(v3.index, v1.index);
                Vector2Int pair2 = new Vector2Int(v1.index, v2.index);
                Vector2Int pair3 = new Vector2Int(v2.index, v3.index);

                if (edgeBuffer.ContainsKey(pair1) ||
                    edgeBuffer.ContainsKey(pair2) ||
                    edgeBuffer.ContainsKey(pair3))
                {
                    continue;
                }
                else
                {
                    edgeBuffer.Add(pair1, e1);
                    edgeBuffer.Add(pair2, e2);
                    edgeBuffer.Add(pair3, e3);
                }

                halfEdges.Add(e1);
                halfEdges.Add(e2);
                halfEdges.Add(e3);

                Face face = new Face();
                face.index = faces.Count;
                face.halfEdge = e1.index;
                faces.Add(face);

                _area += Vector3.Cross(v2.position - v1.position, v3.position - v1.position).magnitude / 2.0f;
                _volume += Vector3.Dot(Vector3.Cross(v1.position, v2.position), v3.position) / 6.0f;
            }

            foreach (var elm in edgeBuffer)
            {
                HalfEdge lonelyEdge = elm.Value;

                // swap vertex indices, to find its pair:
                Vector2Int swapped = new Vector2Int(elm.Key.y, elm.Key.x);
                HalfEdge pair;

                // if we couldn´t find a pair for this edge, it means its in the border. Border edges are always the last ones in the edges array:
                if (!edgeBuffer.TryGetValue(swapped, out pair))
                {
                    //generate border:
                    pair = new HalfEdge();
                    pair.indexInFace = -1; //flag as border.
                    pair.face = -1;
                    pair.index = halfEdges.Count;
                    pair.endVertex = halfEdges[halfEdges[lonelyEdge.nextHalfEdge].nextHalfEdge].endVertex;
                    pair.pair = lonelyEdge.index;

                    // update vertex half edge, as it must point to the border:
                    Vertex v = vertices[lonelyEdge.endVertex];
                    v.halfEdge = pair.index;
                    vertices[lonelyEdge.endVertex] = v;

                    halfEdges.Add(pair);
                    borderEdges.Add(pair);
                }

                // give the lonely edge a pair:
                lonelyEdge.pair = pair.index;
                halfEdges[lonelyEdge.index] = lonelyEdge;

            }

            // link border edges:
            for (int i = 0; i < borderEdges.Count; ++i)
            {
                HalfEdge edge = halfEdges[borderEdges[i].index];
                edge.nextHalfEdge = vertices[edge.endVertex].halfEdge;
                halfEdges[borderEdges[i].index] = edge;
            }
            containsData = true;

            CalculateRestNormals();
            CalculateRestOrientations();
        }

        private void CalculateRestNormals()
        {
            restNormals.Capacity = vertices.Count;
            for (int i = 0; i < vertices.Count; ++i)
                restNormals.Add(Vector3.zero);

            for (int i = 0; i < faces.Count; ++i)
            {
                HalfEdge e1 = halfEdges[faces[i].halfEdge];
                HalfEdge e2 = halfEdges[e1.nextHalfEdge];
                HalfEdge e3 = halfEdges[e2.nextHalfEdge];

                Vector3 v1 = vertices[e1.endVertex].position;
                Vector3 v2 = vertices[e2.endVertex].position;
                Vector3 v3 = vertices[e3.endVertex].position;

                Vector3 n = Vector3.Cross(v2 - v1, v3 - v1);

                restNormals[e1.endVertex] += n;
                restNormals[e2.endVertex] += n;
                restNormals[e3.endVertex] += n;
            }

            for (int i = 0; i < restNormals.Count; ++i)
                restNormals[i].Normalize();
        }

        private void CalculateRestOrientations()
        {
            for (int i = 0; i < vertices.Count; ++i)
            {
                Vector3 surface = vertices[halfEdges[vertices[i].halfEdge].endVertex].position - vertices[i].position;
                restOrientations.Add(Quaternion.Inverse(Quaternion.LookRotation(restNormals[i], surface)));
            }
        }

        public void SwapVertices(int index1, int index2)
        {
            vertices.Swap(index1, index2);
            restNormals.Swap(index1, index2);
            restOrientations.Swap(index1, index2);

            for (int i = 0; i < halfEdges.Count; ++i)
            {
                HalfEdgeMesh.HalfEdge halfEdge = halfEdges[i];
                if (halfEdge.endVertex == index1)
                {
                    halfEdge.endVertex = index2;
                    halfEdges[i] = halfEdge;
                }
                else if (halfEdge.endVertex == index2)
                {
                    halfEdge.endVertex = index1;
                    halfEdges[i] = halfEdge;
                }
            }

            for (int i = 0; i < borderEdges.Count; ++i)
            {
                HalfEdgeMesh.HalfEdge halfEdge = borderEdges[i];
                if (halfEdge.endVertex == index1)
                {
                    halfEdge.endVertex = index2;
                    borderEdges[i] = halfEdge;
                }
                else if (halfEdge.endVertex == index2)
                {
                    halfEdge.endVertex = index1;
                    borderEdges[i] = halfEdge;
                }
            }

            for (int i = 0; i < rawToWelded.Count; ++i)
            {
                if (rawToWelded[i] == index1)
                    rawToWelded[i] = index2;
                else if (rawToWelded[i] == index2)
                    rawToWelded[i] = index1;
            }
        }

        public int GetHalfEdgeStartVertex(HalfEdge edge)
        {

            // In a border edge, get the ending vertex of the pair edge:
            if (edge.face == -1)
                return halfEdges[edge.pair].endVertex;

            // In case of an interior edge, find the vertex by going around the face:
            return halfEdges[halfEdges[edge.nextHalfEdge].nextHalfEdge].endVertex;
        }

        public float GetFaceArea(Face face)
        {

            HalfEdge e1 = halfEdges[face.halfEdge];
            HalfEdge e2 = halfEdges[e1.nextHalfEdge];
            HalfEdge e3 = halfEdges[e2.nextHalfEdge];

            return Vector3.Cross(vertices[e2.endVertex].position - vertices[e1.endVertex].position,
                                 vertices[e3.endVertex].position - vertices[e1.endVertex].position).magnitude / 2.0f;
        }

        public IEnumerable<Vertex> GetNeighbourVerticesEnumerator(Vertex vertex)
        {

            HalfEdge startEdge = halfEdges[vertex.halfEdge];
            HalfEdge edge = startEdge;

            do
            {
                yield return vertices[edge.endVertex];
                edge = halfEdges[edge.pair];
                edge = halfEdges[edge.nextHalfEdge];

            } while (edge.index != startEdge.index);

        }

        public IEnumerable<HalfEdge> GetNeighbourEdgesEnumerator(Vertex vertex)
        {

            HalfEdge startEdge = halfEdges[vertex.halfEdge];
            HalfEdge edge = startEdge;

            do
            {
                edge = halfEdges[edge.pair];
                yield return edge;
                edge = halfEdges[edge.nextHalfEdge];
                yield return edge;

            } while (edge.index != startEdge.index);

        }

        public IEnumerable<Face> GetNeighbourFacesEnumerator(Vertex vertex)
        {

            HalfEdge startEdge = halfEdges[vertex.halfEdge];
            HalfEdge edge = startEdge;

            do
            {

                edge = halfEdges[edge.pair];
                if (edge.face > -1)
                    yield return faces[edge.face];
                edge = halfEdges[edge.nextHalfEdge];

            } while (edge.index != startEdge.index);

        }

        /**
         * Calculates and returns a list of all edges (note: not half-edges, but regular edges) in the mesh. Each edge is represented as the index of
         * the first half-edge in the list that is part of the edge. 
         * This is O(2N) in both time and space, with N = number of edges.
         */
        public List<int> GetEdgeList()
        {

            List<int> edges = new List<int>();
            bool[] listed = new bool[halfEdges.Count];

            for (int i = 0; i < halfEdges.Count; i++)
            {
                if (!listed[halfEdges[i].pair])
                {
                    edges.Add(i);
                    listed[halfEdges[i].pair] = true;
                    listed[i] = true;
                }
            }

            return edges;
        }

        /**
         * Returns true if the edge has been split in a vertex split operation. (as a result of tearing)
         */
        public bool IsSplit(int halfEdgeIndex)
        {

            HalfEdge edge = halfEdges[halfEdgeIndex];

            if (edge.pair < 0 || edge.face < 0) return false;

            HalfEdge pair = halfEdges[edge.pair];

            return edge.endVertex != halfEdges[halfEdges[pair.nextHalfEdge].nextHalfEdge].endVertex ||
                   pair.endVertex != halfEdges[halfEdges[edge.nextHalfEdge].nextHalfEdge].endVertex;

        }

    }
}

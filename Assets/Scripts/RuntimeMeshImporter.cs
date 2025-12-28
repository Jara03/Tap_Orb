using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public static class RuntimeMeshImporter
{
    public static bool TryLoadMeshFromFile(string path, out Mesh mesh, out Bounds bounds)
    {
        mesh = null;
        bounds = default;

        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return false;

        string extension = Path.GetExtension(path).ToLowerInvariant();
        if (extension == ".obj")
        {
            mesh = LoadObjMesh(path);
        }
        else if (extension == ".assetbundle" || extension == ".unity3d")
        {
            mesh = LoadMeshFromBundle(path);
        }

        if (mesh == null)
            return false;

        bounds = mesh.bounds;
        return true;
    }

    private static Mesh LoadMeshFromBundle(string path)
    {
        var bundle = AssetBundle.LoadFromFile(path);
        if (bundle == null)
        {
            Debug.LogWarning($"Unable to load asset bundle at {path}");
            return null;
        }

        Mesh loadedMesh = null;

        var meshes = bundle.LoadAllAssets<Mesh>();
        if (meshes != null && meshes.Length > 0)
        {
            loadedMesh = Object.Instantiate(meshes[0]);
        }

        if (loadedMesh == null)
        {
            var prefabs = bundle.LoadAllAssets<GameObject>();
            if (prefabs != null && prefabs.Length > 0)
            {
                var meshFilter = prefabs[0].GetComponentInChildren<MeshFilter>();
                if (meshFilter != null)
                {
                    loadedMesh = Object.Instantiate(meshFilter.sharedMesh);
                }
            }
        }

        bundle.Unload(false);
        return loadedMesh;
    }

    private static Mesh LoadObjMesh(string path)
    {
        var rawVertices = new List<Vector3>();
        var rawUVs = new List<Vector2>();
        var rawNormals = new List<Vector3>();

        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var normals = new List<Vector3>();
        var triangles = new List<int>();

        var cache = new Dictionary<VertexKey, int>();

        var lines = File.ReadAllLines(path);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                continue;

            var trimmed = line.Trim();
            if (trimmed.StartsWith("v "))
            {
                var parts = trimmed.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 4)
                {
                    rawVertices.Add(new Vector3(ParseFloat(parts[1]), ParseFloat(parts[2]), ParseFloat(parts[3])));
                }
            }
            else if (trimmed.StartsWith("vt "))
            {
                var parts = trimmed.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    rawUVs.Add(new Vector2(ParseFloat(parts[1]), ParseFloat(parts[2])));
                }
            }
            else if (trimmed.StartsWith("vn "))
            {
                var parts = trimmed.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 4)
                {
                    rawNormals.Add(new Vector3(ParseFloat(parts[1]), ParseFloat(parts[2]), ParseFloat(parts[3])));
                }
            }
            else if (trimmed.StartsWith("f "))
            {
                var parts = trimmed.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 4)
                    continue;

                var faceIndices = new List<int>();
                for (int i = 1; i < parts.Length; i++)
                {
                    var indices = ParseFaceVertex(parts[i]);
                    var key = new VertexKey(indices.v, indices.vt, indices.vn);
                    if (!cache.TryGetValue(key, out int newIndex))
                    {
                        Vector3 position = ResolveIndex(rawVertices, indices.v, Vector3.zero);
                        Vector2 uv = ResolveIndex(rawUVs, indices.vt, Vector2.zero);
                        Vector3 normal = ResolveIndex(rawNormals, indices.vn, Vector3.zero);

                        vertices.Add(position);
                        uvs.Add(uv);
                        normals.Add(normal);

                        newIndex = vertices.Count - 1;
                        cache.Add(key, newIndex);
                    }

                    faceIndices.Add(newIndex);
                }

                for (int i = 1; i + 1 < faceIndices.Count; i++)
                {
                    triangles.Add(faceIndices[0]);
                    triangles.Add(faceIndices[i]);
                    triangles.Add(faceIndices[i + 1]);
                }
            }
        }

        if (vertices.Count == 0 || triangles.Count == 0)
            return null;

        var mesh = new Mesh
        {
            name = Path.GetFileNameWithoutExtension(path)
        };

        mesh.SetVertices(vertices);
        if (uvs.Count == vertices.Count)
            mesh.SetUVs(0, uvs);
        if (normals.Exists(n => n != Vector3.zero))
            mesh.SetNormals(normals);

        mesh.SetTriangles(triangles, 0);

        if (!normals.Exists(n => n != Vector3.zero))
            mesh.RecalculateNormals();

        mesh.RecalculateBounds();
        return mesh;
    }

    private static (int v, int vt, int vn) ParseFaceVertex(string token)
    {
        int v = 0, vt = -1, vn = -1;
        var parts = token.Split('/');

        if (parts.Length > 0)
            v = ParseIndex(parts[0]);
        if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]))
            vt = ParseIndex(parts[1]);
        if (parts.Length > 2 && !string.IsNullOrEmpty(parts[2]))
            vn = ParseIndex(parts[2]);

        return (v, vt, vn);
    }

    private static int ParseIndex(string value)
    {
        int parsed;
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed);
        return parsed;
    }

    private static float ParseFloat(string value)
    {
        return float.Parse(value, CultureInfo.InvariantCulture.NumberFormat);
    }

    private static T ResolveIndex<T>(List<T> list, int index, T fallback)
    {
        if (list == null || list.Count == 0)
            return fallback;

        if (index > 0 && index <= list.Count)
            return list[index - 1];

        if (index < 0 && list.Count + index >= 0)
            return list[list.Count + index];

        return fallback;
    }

    private readonly struct VertexKey
    {
        public readonly int V;
        public readonly int Vt;
        public readonly int Vn;

        public VertexKey(int v, int vt, int vn)
        {
            V = v;
            Vt = vt;
            Vn = vn;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + V;
                hash = hash * 23 + Vt;
                hash = hash * 23 + Vn;
                return hash;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is VertexKey other)
            {
                return V == other.V && Vt == other.Vt && Vn == other.Vn;
            }

            return false;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(BoxCollider), typeof(Renderer))]
public class GridMapDimensions : MonoBehaviour
{
    [SerializeField, Min(1), Tooltip("Number of one-unit cells across the world X axis.")]
    private int cellsX = 20;

    [SerializeField, Min(1), Tooltip("Number of one-unit cells across world Z (map depth).")]
    private int cellsZ = 20;
    [SerializeField] private MeshFilter gridLines;

    private const float CellSize = 1f;
    private const float LineWidth = 0.018f;
    private const float GridSurfaceHeight = 0.502f;
    private const string GeneratedMeshName = "Generated Map Grid";

    private Mesh generatedMesh;
    private bool rebuildPending;

    private void OnEnable()
    {
        rebuildPending = true;
    }

    private void OnValidate()
    {
        cellsX = Mathf.Clamp(cellsX, 1, 1000);
        cellsZ = Mathf.Clamp(cellsZ, 1, 1000);
        rebuildPending = true;
    }

    private void Update()
    {
        if (!rebuildPending)
        {
            return;
        }

        rebuildPending = false;
        ApplyDimensionsAndGrid();
    }

    private void ApplyDimensionsAndGrid()
    {
        Vector3 scale = transform.localScale;
        scale.x = cellsX * CellSize;
        scale.z = cellsZ * CellSize;
        transform.localScale = scale;

        if (gridLines == null)
        {
            MeshFilter[] childFilters = GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < childFilters.Length; i++)
            {
                if (childFilters[i].gameObject != gameObject)
                {
                    gridLines = childFilters[i];
                    break;
                }
            }
        }

        if (gridLines == null)
        {
            Debug.LogError("GridMapDimensions needs a child MeshFilter for the grid lines.", this);
            return;
        }

        Vector3 gridPosition = gridLines.transform.localPosition;
        gridPosition.y = GridSurfaceHeight;
        gridLines.transform.localPosition = gridPosition;

        if (generatedMesh == null && gridLines.sharedMesh != null && gridLines.sharedMesh.name == GeneratedMeshName)
        {
            generatedMesh = gridLines.sharedMesh;
        }

        if (generatedMesh == null)
        {
            generatedMesh = new Mesh
            {
                name = GeneratedMeshName,
                hideFlags = HideFlags.DontSave
            };
            gridLines.sharedMesh = generatedMesh;
        }
        else
        {
            generatedMesh.Clear();
        }

        BuildGridMesh();
    }

    private void BuildGridMesh()
    {
        List<Vector3> vertices = new List<Vector3>((cellsX + cellsZ + 2) * 4);
        List<Vector3> normals = new List<Vector3>((cellsX + cellsZ + 2) * 4);
        List<int> triangles = new List<int>((cellsX + cellsZ + 2) * 6);

        float halfLineX = LineWidth / (2f * cellsX * CellSize);
        float halfLineZ = LineWidth / (2f * cellsZ * CellSize);

        for (int x = 0; x <= cellsX; x++)
        {
            float lineX = -0.5f + (float)x / cellsX;
            AddQuad(
                vertices, normals, triangles,
                new Vector3(lineX - halfLineX, 0f, -0.5f),
                new Vector3(lineX - halfLineX, 0f, 0.5f),
                new Vector3(lineX + halfLineX, 0f, 0.5f),
                new Vector3(lineX + halfLineX, 0f, -0.5f));
        }

        for (int z = 0; z <= cellsZ; z++)
        {
            float lineZ = -0.5f + (float)z / cellsZ;
            AddQuad(
                vertices, normals, triangles,
                new Vector3(-0.5f, 0f, lineZ - halfLineZ),
                new Vector3(-0.5f, 0f, lineZ + halfLineZ),
                new Vector3(0.5f, 0f, lineZ + halfLineZ),
                new Vector3(0.5f, 0f, lineZ - halfLineZ));
        }

        generatedMesh.SetVertices(vertices);
        generatedMesh.SetTriangles(triangles, 0);
        generatedMesh.SetNormals(normals);
        generatedMesh.RecalculateBounds();
    }

    private static void AddQuad(
        List<Vector3> vertices,
        List<Vector3> normals,
        List<int> triangles,
        Vector3 a,
        Vector3 b,
        Vector3 c,
        Vector3 d)
    {
        int start = vertices.Count;
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        vertices.Add(d);
        normals.Add(Vector3.up);
        normals.Add(Vector3.up);
        normals.Add(Vector3.up);
        normals.Add(Vector3.up);
        triangles.Add(start);
        triangles.Add(start + 1);
        triangles.Add(start + 2);
        triangles.Add(start);
        triangles.Add(start + 2);
        triangles.Add(start + 3);
    }

    private void OnDestroy()
    {
        if (generatedMesh == null)
        {
            return;
        }

        if (gridLines != null && gridLines.sharedMesh == generatedMesh)
        {
            gridLines.sharedMesh = null;
        }

        if (Application.isPlaying)
        {
            Destroy(generatedMesh);
        }
        else
        {
            DestroyImmediate(generatedMesh);
        }
    }
}

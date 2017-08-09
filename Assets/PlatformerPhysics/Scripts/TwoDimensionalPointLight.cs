using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal struct LightPoint
{
    public Vector2 pos;
    public float angle;
    public bool endpoint;
}

public class TwoDimensionalPointLight : MonoBehaviour {

    /* ========== Serialized vars ============= */
    [SerializeField]
    public float LightRadius = 10f;

    [SerializeField]
    public Material _LightMat;

    [SerializeField]
    private LayerMask _CollisionLayers;


    private Collider2D[] _Colliders;

    /// <summary>
    /// All points of raycast collision
    /// </summary>
    private List<LightPoint> Points;

    private Mesh _LightMesh;

	// Use this for initialization
	void Start () {
        MeshRenderer rend = gameObject.AddComponent<MeshRenderer>();
        rend.material = _LightMat;

        MeshFilter filter = gameObject.AddComponent<MeshFilter>();
        _LightMesh = new Mesh();
        _LightMesh.name = "LightMesh";
        filter.mesh = _LightMesh;
        _LightMesh.MarkDynamic();
        Points = new List<LightPoint>();
	}
	
	// Update is called once per frame
	void Update () {
        CollectColliders();
        CastPoints();
        SortPoints();
        FillGaps();
        UpdateMesh();
        ResetBounds();
	}

    private void CollectColliders()
    {
        _Colliders = Physics2D.OverlapCircleAll(transform.position, LightRadius, _CollisionLayers);
    }

    private void CastPoints()
    {
        Points.Clear();

        for (int i = 0; i < _Colliders.Length; i++)
        {
            //Ignore non-polygon colliders
            if (_Colliders[i].GetType() != typeof(PolygonCollider2D))
                continue;

            //Cast collider to a polygon collider
            PolygonCollider2D currentCollider = _Colliders[i] as PolygonCollider2D;

            //Iterate over each point of this collider, and cast a ray to it
            for (int j = 0; j < currentCollider.points.Length; j++)
            {
                CheckPoint(currentCollider.transform.TransformPoint(currentCollider.points[j]));
            }
        } 
    }

    /// <summary>
    /// Casts appropriate rays to a given Vector3 and adds them to the points array if neededs
    /// </summary>
    /// <param name="point"></param>
    private void CheckPoint(Vector2 point)
    {
        //Do a cast from the light position to the edge of the circle.
        Vector2 dir = point - (Vector2)transform.position;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, LightRadius, _CollisionLayers);
        LightPoint p = new LightPoint();

        if (hit)
        {
            p.pos = hit.point;
            p.pos = transform.InverseTransformPoint(p.pos);
            p.angle = Mathf.Atan2(dir.y, dir.x);
            Points.Add(p);
        }
        else
        {
            p.pos = point;
            p.pos = transform.InverseTransformPoint(p.pos);
            p.endpoint = true;
            Points.Add(p);
        }
    }

    private void SortPoints()
    {
        Points.Sort((item1, item2) => (item2.angle.CompareTo(item1.angle)));
    }

    private void FillGaps()
    {
        List<float> endangles = new List<float>();
        for (int i = 0; i < Points.Count; i++)
        {
            if (Points[i].endpoint)
                endangles.Add(Points[i].angle);
        }
    }

    private void ResetBounds()
    {
        Bounds b = _LightMesh.bounds;
        b.center = Vector3.zero;
        _LightMesh.bounds = b;
    }

    private void UpdateMesh()
    {
        Vector3[] verts = new Vector3[Points.Count + 1];
        verts[0] = Vector3.zero;
        for (int i = 0; i < verts.Length - 1; i++)
        {
            verts[i + 1] = Points[i].pos;
        }

        _LightMesh.vertices = verts;

        // triangles
        int idx = 0;
        int[] triangles = new int[(Points.Count * 3)];
        for (int i = 0; i < (Points.Count * 3); i += 3)
        {

            triangles[i] = 0;
            triangles[i + 1] = idx + 1;


            if (i == (Points.Count * 3) - 3)
            {
                //-- if is the last vertex (one loop)
                triangles[i + 2] = 1;
            }
            else
            {
                triangles[i + 2] = idx + 2; //next next vertex	
            }

            idx++;
        }


        _LightMesh.triangles = triangles;
    }

    private bool Approx(Vector2 a, Vector2 b)
    {
        return Mathf.Abs(a.x - b.x) < 0.01f && Mathf.Abs(a.y - b.y) < 0.01f;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Detects and resolves collisions within a scene. 
/// Ignores all actual Unity physics, so it's probably best to turn those off.
/// </summary>
public class PlatformerCollisionManager : MonoBehaviour {

    /// <summary>
    /// The layers that this manages.
    /// </summary>
    [SerializeField]
    public LayerMask _CollisionLayers;

    /// <summary>
    /// The collider attached to this object                
    /// </summary>
    private BoxCollider2D _Collider;

    /// <summary>
    /// Player's current velocity
    /// </summary>
    private Vector2 _Velocity;

	// Use this for initialization
	void Start () {
        _Collider = GetComponent<BoxCollider2D>();
    }
	
	// Update is called once per frame
	void Update () {
	}

    /// <summary>
    /// Updates and resolves all collisions
    /// </summary>
    public void Tick()
    {
        DetectAndResolveCollisions();
    }

    private void DetectAndResolveCollisions()
    {
        Collider2D[] colliders = GetRelevantColliders();

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].GetType() != typeof(PolygonCollider2D))
            {
		// Implementation only supports polygon colliders
                continue;
            }
            else
            {
                if (colliders[i].isTrigger)
                    continue;

                Vector2 coll = GetCollision_AABB2Poly(_Collider, colliders[i] as PolygonCollider2D);
                if (coll != Vector2.zero)
                    transform.position += (Vector3)coll;
            }
        }
    }

    /// <summary>
    /// Detects if there is a collision between a box collider and a polygon collider. If there isn't, returns 0,0
    /// otherwise the returned value is the separation vector.
    /// </summary>
    /// <param name="box"></param>
    /// <param name="poly"></param>
    /// <returns>Separation vector</returns>
    private Vector2 GetCollision_AABB2Poly(BoxCollider2D box, PolygonCollider2D poly)
    {
        Vector2 separationVector = Vector2.zero;

        //All the axes to test for for our box
        Vector2[] boxAxes = new Vector2[] { box.transform.up, box.transform.right};

        for (int i = 0; i < boxAxes.Length; i++)
        {
            Vector2 pA = Project(box, boxAxes[i]);
            Vector2 pB = Project(poly, boxAxes[i]);

            //if there is an overlap between the two shapes on this axis
            float sep = Overlap(pA, pB);
            if (sep != 0)
            {
                Vector2 tmpVec = sep * boxAxes[i];//new Vector2(axes[i].y, -axes[i].x);

                if (separationVector.magnitude > tmpVec.magnitude || separationVector == Vector2.zero)
                {
                    separationVector = tmpVec;
                    /*Vector2 dist = box.transform.position - poly.transform.position;
                    if (dist.x + dist.y < 0)
                        separationVector *= -1;*/
                }
            }
            else
            {
                return Vector2.zero;
            }
        }

        //The points in the polygon converted to world space coordinates
        Vector2[] polygonPointsWS = new Vector2[poly.points.Length];

        for (int i = 0; i < polygonPointsWS.Length; i++)
        {
            polygonPointsWS[i] = poly.transform.TransformPoint(poly.points[i] + poly.offset);
        }

        Vector2[] polyAxes = new Vector2[poly.points.Length];

        for (int i = 0; i < polyAxes.Length; i++)
        {
            Vector2 dif = polygonPointsWS[(i + 1) % polygonPointsWS.Length] - polygonPointsWS[i] ;
            dif.Normalize();
            polyAxes[i] = new Vector2(dif.y, -dif.x); 
        }

        for (int i = 0; i < polyAxes.Length; i++)
        {
            Vector2 pA = Project(box, polyAxes[i]);
            Vector2 pB = Project(poly, polyAxes[i]);

            Vector3 linepos = transform.position + new Vector3(polyAxes[i].y, -polyAxes[i].x) * 3;
            Debug.DrawLine(linepos + pA.x * (Vector3)polyAxes[i], linepos + pA.y * (Vector3)polyAxes[i], Color.green);
            linepos += new Vector3(polyAxes[i].y, -polyAxes[i].x) * 0.1f;
            Debug.DrawLine(linepos + pB.x * (Vector3)polyAxes[i], linepos + pB.y * (Vector3)polyAxes[i], Color.green);

            //if there is an overlap between the two shapes on this axis
            float sep = Overlap(pA, pB);
            if (sep != 0)
            {
                Vector2 tmpVec = sep * polyAxes[i];

                if (separationVector.magnitude > tmpVec.magnitude || separationVector == Vector2.zero)
                    separationVector = tmpVec;

                linepos += new Vector3(polyAxes[i].y, -polyAxes[i].x) * 0.1f;
                Debug.DrawLine(linepos, linepos + sep * (Vector3)polyAxes[i], Color.red);
            }
            else
            {
                return Vector2.zero;
            }
        }

        Debug.DrawLine(transform.position, transform.position + (Vector3)separationVector, Color.red);
        Debug.DrawLine(transform.position + (Vector3)separationVector, transform.position + (Vector3)separationVector + (Vector3)separationVector/2, Color.green);
        //Debug.Log(separationVector);

        return separationVector;
    }

    private float Overlap(Vector2 aV, Vector2 bV)
    {
        if (Mathf.Approximately(aV.x, bV.y) || Mathf.Approximately(bV.x, aV.y))
            return 0;

        if (aV.x < bV.x)
        {
            if (aV.y < bV.x)
            {
                return 0f;
            }

            return bV.x - aV.y;
        }

        if (bV.y < aV.x)
        {
            return 0f;
        }

        return bV.y - aV.x;
    }

    /// <summary>
    /// Returns the projection of "shape" onto axis "axis"
    /// </summary>
    /// <param name="shape"></param>
    /// <param name="axis"></param>
    /// <returns></returns>
    private Vector2 Project(PolygonCollider2D shape, Vector2 axis)
    {
        float min = float.MaxValue;
        float max = float.MinValue;

        for (int i = 0; i < shape.points.Length; i++)
        {
            Vector2 pointWS = shape.transform.TransformPoint(shape.points[i] + shape.offset);


            float proj = Vector2.Dot(pointWS, axis) / axis.magnitude;

            if (proj < min)
                min = proj;

            if (proj > max)
                max = proj;
        }


        return new Vector2(min, max);
    }

    /// <summary>
    /// Returns the projection of "shape" onto axis "axis"
    /// </summary>
    /// <param name="shape"></param>
    /// <param name="axis"></param>
    /// <returns></returns>
    private Vector2 Project(BoxCollider2D shape, Vector2 axis)
    {
        Vector2[] points = new Vector2[]
        {
            shape.transform.TransformPoint(new Vector3(-shape.size.x, -shape.size.y)/2),//Top left
            shape.transform.TransformPoint(new Vector3(shape.size.x, -shape.size.y)/2),//Top right
            shape.transform.TransformPoint(new Vector3(-shape.size.x, shape.size.y)/2),//Bottom left
            shape.transform.TransformPoint(new Vector3(shape.size.x, shape.size.y)/2),//Bottom right
        };

        float min = float.MaxValue;
        float max = min;

        for (int i = 0; i < 4; i++)
        {
            float proj = Vector2.Dot(points[i],axis) / axis.magnitude;

            if (proj < min)
                min = proj;

            if (proj > max)
                max = proj;
        }

        return new Vector2(min, max);
    }

    /// <summary>
    /// CircleCast to get all nearby colliders
    /// </summary>
    /// <returns></returns>
    private Collider2D[] GetRelevantColliders()
    {
        return Physics2D.OverlapCircleAll(transform.position, 5, _CollisionLayers);
    }
}

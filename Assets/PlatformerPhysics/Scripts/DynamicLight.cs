/****************************************************************************
 Copyright (c) 2014 Martin Ysa

 Permission is hereby granted, free of charge, to any person obtaining a copy
 of this software and associated documentation files (the "Software"), to deal
 in the Software without restriction, including without limitation the rights
 to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 copies of the Software, and to permit persons to whom the Software is
 furnished to do so, subject to the following conditions:
 
 The above copyright notice and this permission notice shall be included in
 all copies or substantial portions of the Software.
 
 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 THE SOFTWARE.
 ****************************************************************************/

namespace DynamicLight2D
{
    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;       // This allows for the use of lists, like <GameObject>
                                            //using pseudoSinCos;


    public class verts
    {
        public float angle { get; set; }
        public int location { get; set; } // 1= left end point    0= middle     -1=right endpoint
        public Vector3 pos { get; set; }
        public bool endpoint { get; set; }

    }


    public class DynamicLight : MonoBehaviour
    {



        // Public variables

        public string version = "1.0.5"; //release date 09/01/2017

        public Material lightMaterial;

        [HideInInspector]
        public PolygonCollider2D[] allMeshes;                                   // Array for all of the meshes in our scene


        [HideInInspector]
        public List<verts> allVertices = new List<verts>();                             // Array for all of the vertices in our meshes

        [SerializeField]
        public float PlayerDamage = 10; //How much damage should this do to the player

        [SerializeField]
        public float lightRadius = 20f;

        public int lightSegments = 8;

        public LayerMask layer;

        private float PointsPerArcLen;

        // Private variables
        Mesh lightMesh;                                                 // Mesh for our light mesh

        // Called at beginning of script execution
        void Start()
        {

            TablaSenoCoseno.initSenCos();

            //Debug.Log((int) LayerMask.NameToLayer("Default"));


            //-- Step 1: obtain all active meshes in the scene --//
            //---------------------------------------------------------------------//

            MeshFilter meshFilter = (MeshFilter)gameObject.AddComponent(typeof(MeshFilter));                // Add a Mesh Filter component to the light game object so it can take on a form
            MeshRenderer renderer = gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;      // Add a Mesh Renderer component to the light game object so the form can become visible
                                                                                                        //gameObject.name = "2DLight";
                                                                                                        //renderer.material.shader = Shader.Find ("Transparent/Diffuse");							// Find the specified type of material shader
            renderer.sharedMaterial = lightMaterial;                                                        // Add this texture
            lightMesh = new Mesh();                                                                 // create a new mesh for our light mesh
            meshFilter.mesh = lightMesh;                                                            // Set this newly created mesh to the mesh filter
            lightMesh.name = "Light Mesh";                                                          // Give it a name
            lightMesh.MarkDynamic();

            PointsPerArcLen = 360f / lightSegments;
        }


        int cycles = 0, watchcount = 0;
        void FixedUpdate()
        {

            //Check if the camera can see this light, and if it can't, don't update
            if (!CheckShouldUpdate())
                return;

            System.Diagnostics.Stopwatch s = new System.Diagnostics.Stopwatch();
            s.Start();
            getAllMeshes();
            setLight();
            renderLightMesh();
            resetBounds();
            CheckPlayerCollision();

            s.Stop();
            cycles += (int)s.ElapsedTicks;
            watchcount++;
            //Debug.Log("AVG: " + (float)cycles / (float)watchcount);
        }

        /// <summary>
        ///  Returns true if the light is currently visible
        /// </summary>
        bool CheckShouldUpdate()
        {
            var cam = Camera.main;
            Vector2 pixPosition = cam.WorldToScreenPoint(transform.position);

            //If the center of the light is within the camera rect
            if (cam.pixelRect.Contains(pixPosition))
            {
                return true;
            }


            float dist = cam.pixelWidth / 2;
            dist += lightRadius * cam.orthographicSize;
            //If any of the points of the camera rect are within this light
            if (Vector2.Distance(pixPosition, new Vector2(cam.pixelRect.xMax, cam.pixelRect.yMax)) < dist
                || Vector2.Distance(pixPosition, new Vector2(cam.pixelRect.xMax, cam.pixelRect.yMin)) < dist
                || Vector2.Distance(pixPosition, new Vector2(cam.pixelRect.xMin, cam.pixelRect.yMax)) < dist
                || Vector2.Distance(pixPosition, new Vector2(cam.pixelRect.xMin, cam.pixelRect.yMin)) < dist)
                return true;

            return false;
        }

        //Cast rays to all points of the player's bounds. If any are a hit, the player is inside the light
        void CheckPlayerCollision()
        {
            /*GameObject player = GameObject.FindGameObjectWithTag("Player");
            Bounds playerBounds = player.GetComponent<BoxCollider2D>().bounds;

            float left = playerBounds.center.x - playerBounds.size.x / 2;
            float right = playerBounds.center.x + playerBounds.size.x / 2;
            float bottom = playerBounds.center.y - playerBounds.size.y / 2;
            float top = playerBounds.center.y + playerBounds.size.y / 2;

            LayerMask tmpMask = layer | (1 << LayerMask.NameToLayer("Player"));

            RaycastHit2D hitTL = Physics2D.Raycast(transform.position, new Vector2(left - transform.position.x, top - transform.position.y).normalized, lightRadius, tmpMask);
            RaycastHit2D hitBL = Physics2D.Raycast(transform.position, new Vector2(left - transform.position.x, bottom - transform.position.y).normalized, lightRadius, tmpMask);
            RaycastHit2D hitTR = Physics2D.Raycast(transform.position, new Vector2(right - transform.position.x, top - transform.position.y).normalized, lightRadius, tmpMask);
            RaycastHit2D hitBR = Physics2D.Raycast(transform.position, new Vector2(right - transform.position.x, bottom - transform.position.y).normalized, lightRadius, tmpMask);

            Debug.DrawLine(transform.position, hitTL.point, Color.red);
            Debug.DrawLine(transform.position, hitTR.point, Color.red);
            Debug.DrawLine(transform.position, hitBL.point, Color.red);
            Debug.DrawLine(transform.position, hitBR.point, Color.red);


            if (hitTL.collider == player.GetComponent<BoxCollider2D>()
                || hitBL.collider == player.GetComponent<BoxCollider2D>()
                || hitTR.collider == player.GetComponent<BoxCollider2D>()
                || hitBR.collider == player.GetComponent<BoxCollider2D>())
                player.GetComponent<PlayerEnergy>().ConsumeEnergyTimed(PlayerDamage);*/
        }

        void getAllMeshes()
        {
            //allMeshes = FindObjectsOfType(typeof(PolygonCollider2D)) as PolygonCollider2D[];


            Collider2D[] allColl2D = Physics2D.OverlapCircleAll(transform.position, lightRadius, layer);
            allMeshes = new PolygonCollider2D[allColl2D.Length];

            for (int i = 0; i < allColl2D.Length; i++)
            {
                if (allColl2D[i].GetType() != typeof(PolygonCollider2D))
                {
                    //Ignore non polygon colliders
                    continue;
                }
                allMeshes[i] = (PolygonCollider2D)allColl2D[i];
            }



        }

        void resetBounds()
        {
            Bounds b = lightMesh.bounds;
            b.center = Vector3.zero;
            lightMesh.bounds = b;
        }

        void setLight()
        {

            bool sortAngles = false;

            allVertices.Clear();// Since these lists are populated every frame, clear them first to prevent overpopulation


            //layer = 1 << 8;


            //--Step 2: Obtain vertices for each mesh --//
            //---------------------------------------------------------------------//

            // las siguientes variables usadas para arregla bug de ordenamiento cuando
            // los angulos calcuados se encuentran en cuadrantes mixtos (1 y 4)
            bool lows = false; // check si hay menores a -0.5
            bool his = false; // check si hay mayores a 2.0
            float magRange = 0.15f;

            List<verts> tempVerts = new List<verts>();

            int total = 0;
            for (int m = 0; m < allMeshes.Length; m++)
            {
                //for (int m = 0; m < 1; m++) {
                tempVerts.Clear();
                PolygonCollider2D mf = allMeshes[m];

                // las siguientes variables usadas para arregla bug de ordenamiento cuando
                // los angulos calcuados se encuentran en cuadrantes mixtos (1 y 4)
                lows = false; // check si hay menores a -0.5
                his = false; // check si hay mayores a 2.0

                if (((1 << mf.transform.gameObject.layer) & layer) != 0)
                {
                    for (int i = 0; i < mf.GetTotalPointCount(); i++)
                    {                                // ...and for ever vertex we have of each mesh filter...
                        total++;
                        verts v = new verts();
                        // Convert to world space
                        Vector3 worldPoint = mf.transform.TransformPoint(mf.points[i]);



                        // Reforma fecha 24/09/2014 (ultimo argumento lighradius X worldPoint.magnitude (expensivo pero preciso))
                        RaycastHit2D ray = Physics2D.Raycast(transform.position, worldPoint - transform.position, lightRadius, layer);


                        if (ray)
                        {
                            v.pos = ray.point;
                            if (worldPoint.sqrMagnitude >= (ray.point.sqrMagnitude - magRange) && worldPoint.sqrMagnitude <= (ray.point.sqrMagnitude + magRange))
                                v.endpoint = true;

                        }
                        else
                        {
                            v.pos = worldPoint;
                            v.endpoint = true;
                        }

                        Debug.DrawLine(transform.position, v.pos, Color.white);

                        //--Convert To local space for build mesh (mesh craft only in local vertex)
                        v.pos = transform.InverseTransformPoint(v.pos);
                        //--Calculate angle
                        v.angle = getVectorAngle(true, v.pos.x, v.pos.y);



                        // -- bookmark if an angle is lower than 0 or higher than 2f --//
                        //-- helper method for fix bug on shape located in 2 or more quadrants
                        if (v.angle < 0f)
                            lows = true;

                        if (v.angle > 2f)
                            his = true;


                        //--Add verts to the main array
                        if ((v.pos).sqrMagnitude <= lightRadius * lightRadius)
                        {
                            tempVerts.Add(v);
                        }

                        if (sortAngles == false)
                            sortAngles = true;


                    }

                }





                // Indentify the endpoints (left and right)
                if (tempVerts.Count > 0)
                {

                    sortList(tempVerts); // sort first

                    int posLowAngle = 0; // save the indice of left ray
                    int posHighAngle = 0; // same last in right side

                    //Debug.Log(lows + " " + his);

                    if (his == true && lows == true)
                    {  //-- FIX BUG OF SORTING CUANDRANT 1-4 --//
                        float lowestAngle = -1f;//tempVerts[0].angle; // init with first data
                        float highestAngle = tempVerts[0].angle;


                        for (int d = 0; d < tempVerts.Count; d++)
                        {



                            if (tempVerts[d].angle < 1f && tempVerts[d].angle > lowestAngle)
                            {
                                lowestAngle = tempVerts[d].angle;
                                posLowAngle = d;
                            }

                            if (tempVerts[d].angle > 2f && tempVerts[d].angle < highestAngle)
                            {
                                highestAngle = tempVerts[d].angle;
                                posHighAngle = d;
                            }
                        }


                    }
                    else
                    {
                        //-- convencional position of ray points
                        // save the indice of left ray
                        posLowAngle = 0;
                        posHighAngle = tempVerts.Count - 1;

                    }


                    tempVerts[posLowAngle].location = 1; // right
                    tempVerts[posHighAngle].location = -1; // left



                    //--Add vertices to the main meshes vertexes--//
                    allVertices.AddRange(tempVerts);
                    //allVertices.Add(tempVerts[0]);
                    //allVertices.Add(tempVerts[tempVerts.Count - 1]);



                    // -- r ==0 --> right ray
                    // -- r ==1 --> left ray
                    for (int r = 0; r < 2; r++)
                    {

                        //-- Cast a ray in same direction continuos mode, start a last point of last ray --//
                        Vector3 fromCast = new Vector3();
                        bool isEndpoint = false;

                        if (r == 0)
                        {
                            fromCast = transform.TransformPoint(tempVerts[posLowAngle].pos);
                            isEndpoint = tempVerts[posLowAngle].endpoint;

                        }
                        else if (r == 1)
                        {
                            fromCast = transform.TransformPoint(tempVerts[posHighAngle].pos);
                            isEndpoint = tempVerts[posHighAngle].endpoint;
                        }

                        if (isEndpoint == true)
                        {
                            Vector2 from = (Vector2)fromCast;
                            Vector2 dir = (from - (Vector2)transform.position);

                            float mag = (lightRadius);// - fromCast.magnitude;
                            const float checkPointLastRayOffset = 0.005f;

                            from += (dir * checkPointLastRayOffset);


                            RaycastHit2D rayCont = Physics2D.Raycast(from, dir, mag, layer);
                            Vector3 hitp;
                            if (rayCont)
                            {
                                hitp = rayCont.point;
                            }
                            else
                            {
                                Vector2 newDir = transform.InverseTransformDirection(dir);  //local p
                                hitp = (Vector2)transform.TransformPoint(newDir.normalized * mag); //world p
                            }

                            if (((Vector2)hitp - (Vector2)transform.position).sqrMagnitude > (lightRadius * lightRadius))
                            {
                                dir = (Vector2)transform.InverseTransformDirection(dir);    //local p
                                hitp = (Vector2)transform.TransformPoint(dir.normalized * mag);
                            }

                            Debug.DrawLine(fromCast, hitp, Color.green);

                            verts vL = new verts();
                            vL.pos = transform.InverseTransformPoint(hitp);

                            vL.angle = getVectorAngle(true, vL.pos.x, vL.pos.y);
                            allVertices.Add(vL);
                        }


                    }


                }


            }




            //--Step 3: Generate vectors for light cast--//
            //---------------------------------------------------------------------//

            /*int theta = 0;
            //float amount = (Mathf.PI * 2) / lightSegments;
            int amount = 360 / lightSegments;

            for (int i = 0; i < lightSegments; i++)
            {

                theta = amount * (i);
                if (theta == 360) theta = 0;

                verts v = new verts();
                //v.pos = new Vector3((Mathf.Sin(theta)), (Mathf.Cos(theta)), 0); // in radians low performance
                v.pos = new Vector3((TablaSenoCoseno.SenArray[theta]), (TablaSenoCoseno.CosArray[theta]), 0); // in dregrees (previous calculate)

                v.angle = getVectorAngle(true, v.pos.x, v.pos.y);
                v.pos *= lightRadius;
                v.pos += transform.position;



                RaycastHit2D ray = Physics2D.Raycast(transform.position, v.pos - transform.position, lightRadius, layer);
                //Debug.DrawRay(transform.position, v.pos - transform.position, Color.white);

                if (!ray)
                {

                    //Debug.DrawLine(transform.position, v.pos, Color.white);

                    v.pos = transform.InverseTransformPoint(v.pos);
                    allVertices.Add(v);

                }

            }*/

            /*

            for (int i = 0; i < 4; i++)
            {
                Vector2 dir = new Vector2(
                    (i % 2 == 0) ? 1: -1,
                    (i >1) ? 1 : -1
                    );
                
                verts v = new DynamicLight2D.verts();
                //Todo: find a better way of calculating these
                v.angle = Mathf.Atan2(dir.y, dir.x);
                //Debug.Log(v.angle);

                RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, lightRadius * 1.293f, layer);
                if (!hit)
                {
                    v.pos = (Vector2)transform.position + dir * lightRadius;
                    v.pos = transform.InverseTransformPoint(v.pos);
                    allVertices.Add(v);
                }
            }
            */


            //-- Step 4: Sort each vertice by angle (along sweep ray 0 - 2PI)--//
            //---------------------------------------------------------------------//
            if (sortAngles == true)
            {
                sortList(allVertices);
            }
            //-----------------------------------------------------------------------------


            //--auxiliar step (change order vertices close to light first in position when has same direction) --//
            float rangeAngleComparision = 0.00001f;
            for (int i = 0; i < allVertices.Count - 1; i += 1)
            {

                verts uno = allVertices[i];
                verts dos = allVertices[i + 1];

                // -- Comparo el angulo local de cada vertex y decido si tengo que hacer un exchange-- //
                if (uno.angle >= dos.angle - rangeAngleComparision && uno.angle <= dos.angle + rangeAngleComparision)
                {

                    if (dos.location == -1)
                    { // Right Ray

                        if (uno.pos.sqrMagnitude > dos.pos.sqrMagnitude)
                        {
                            allVertices[i] = dos;
                            allVertices[i + 1] = uno;
                            //Debug.Log("changing left");
                        }
                    }


                    // ALREADY DONE!!
                    if (uno.location == 1)
                    { // Left Ray
                        if (uno.pos.sqrMagnitude < dos.pos.sqrMagnitude)
                        {

                            allVertices[i] = dos;
                            allVertices[i + 1] = uno;
                            //Debug.Log("changing");
                        }
                    }



                }

            }

            //After sorting, fill in the gaps
            List<verts> endpoints = new List<verts>();

            for (int i = 0; i < allVertices.Count; i++)
            {
                if (allVertices[i].endpoint)
                    endpoints.Add(allVertices[i]);
            }

            int lastend = 0;
            for (int i = 1; i < endpoints.Count-1; i++)
            {
                if (lastend == 0 && endpoints[i].location == 1)
                    lastend = endpoints[i].location;
                else
                {
                    if (endpoints[i].location == -1)
                    {
                        Debug.DrawLine(transform.TransformPoint(endpoints[i].pos), transform.TransformPoint(endpoints[i - 1].pos), Color.red, .1f);
                    }
                }
                
            }
            
        }

        //Places points on the radius of the circle from beginArc to endArc
        void FillArc(float beginArc, float endArc, int index)
        {
            Debug.Log(beginArc * Mathf.Rad2Deg + "," + endArc * Mathf.Rad2Deg);

            Debug.DrawRay(transform.position, new Vector3(Mathf.Cos(beginArc), Mathf.Sin(beginArc)),Color.red);
            Debug.DrawRay(transform.position, new Vector3(Mathf.Cos(endArc), Mathf.Sin(endArc)), Color.green);
            Debug.DrawLine(transform.position +
                new Vector3(Mathf.Cos(endArc), Mathf.Sin(endArc)) * lightRadius,
                transform.position +
                new Vector3(Mathf.Cos(beginArc), Mathf.Sin(beginArc)) * lightRadius,
                Color.red,.1f);

            float angle = beginArc;

            int points = 10;

            float delta = (endArc - beginArc) / (float)points;

            for (int i = 0; i < points; i++)
            {
                verts v = new verts();
                v.pos = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * lightRadius;
                v.angle = angle;
                allVertices.Insert(index,v);

                angle += delta;
            }
        }

        void renderLightMesh()
        {
            //-- Step 5: fill the mesh with vertices--//
            //---------------------------------------------------------------------//

            //interface_touch.vertexCount = allVertices.Count; // notify to UI

            Vector3[] initVerticesMeshLight = new Vector3[allVertices.Count + 1];

            initVerticesMeshLight[0] = Vector3.zero;


            for (int i = 0; i < allVertices.Count; i++)
            {
                //Debug.Log(allVertices[i].angle);
                initVerticesMeshLight[i + 1] = allVertices[i].pos;

                //if(allVertices[i].endpoint == true)
                //Debug.Log(allVertices[i].angle);

            }

            lightMesh.Clear();
            lightMesh.vertices = initVerticesMeshLight;

            Vector2[] uvs = new Vector2[initVerticesMeshLight.Length];
            for (int i = 0; i < initVerticesMeshLight.Length; i++)
            {
                uvs[i] = new Vector2(initVerticesMeshLight[i].x, initVerticesMeshLight[i].y);
            }
            lightMesh.uv = uvs;

            // triangles
            int idx = 0;
            int[] triangles = new int[(allVertices.Count * 3)];
            for (int i = 0; i < (allVertices.Count * 3); i += 3)
            {

                triangles[i] = 0;
                triangles[i + 1] = idx + 1;


                if (i == (allVertices.Count * 3) - 3)
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


            lightMesh.triangles = triangles;
            //lightMesh.RecalculateNormals();
            GetComponent<Renderer>().sharedMaterial = lightMaterial;
        }

        void sortList(List<verts> lista)
        {
            lista.Sort((item1, item2) => (item2.angle.CompareTo(item1.angle)));
        }

        private Vector2[] Vec3ToVec2Array(Vector3[] val)
        {
            Vector2[] ret = new Vector2[val.Length];

            for (int i = 0; i < val.Length; i++)
            {
                ret[i] = new Vector2(val[i].x, val[i].y);
            }

            return ret;
        }

        void drawLinePerVertex()
        {
            for (int i = 0; i < allVertices.Count; i++)
            {
                if (i < (allVertices.Count - 1))
                {
                    Debug.DrawLine(allVertices[i].pos, allVertices[i + 1].pos, new Color(i * 0.02f, i * 0.02f, i * 0.02f));
                }
                else
                {
                    Debug.DrawLine(allVertices[i].pos, allVertices[0].pos, new Color(i * 0.02f, i * 0.02f, i * 0.02f));
                }
            }
        }

        float getVectorAngle(bool pseudo, float x, float y)
        {
            float ang = 0;
            if (pseudo == true)
            {
                ang = pseudoAngle(x, y);
            }
            else
            {
                ang = Mathf.Atan2(y, x);
            }
            return ang;
        }

        float pseudoAngle(float dx, float dy)
        {
            // Hight performance for calculate angle on a vector (only for sort)
            // APROXIMATE VALUES -- NOT EXACT!! //
            float ax = Mathf.Abs(dx);
            float ay = Mathf.Abs(dy);
            float p = dy / (ax + ay);
            if (dx < 0)
            {
                p = 2 - p;

            }
            return p;
        }

    }
}


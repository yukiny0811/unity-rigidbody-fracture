using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TriggerParent : MonoBehaviour
{
    // Hyper Parameters
    public static readonly int triggerPointCount = 1000;
    public static readonly int maxmumActiveTriggerPoint = 40;
    public static readonly float dotPowerThreshold = 0.4f;
    public static readonly float velClampMultiplier = 2.5f;
    public static readonly int verticeMinimumCount = 5;
    public static readonly float clusteringGridScaleMultiplier = 100f;
    public static readonly float meshgridSeparation = 200;
    public static readonly int firstProcessFrame = 2;
    public static readonly int secondProcessFrame = 5;

    // Array Objects
    private GameObject[] triggers;
    private float[] grid;
    private int[] intGrid;
    List<int> triggeredIndex = new List<int>();

    // trigger variables
    Vector2 firstCenter = new Vector2(0, 0);
    Vector2 secondCentor = new Vector2(0, 0);

    // Timer Counts
    private int enterCount = 0;
    private bool isFractureActive = true;

    // SerializeField
    [SerializeField]
    private GameObject tri;

    [SerializeField]
    private Material glassMaterial;

    [SerializeField]
    private GameObject polygon;

    [SerializeField]
    private GameObject gridNode;

    // GetComponents
    private PolygonCollider2D thisPolygonCollider2D;
    private Rigidbody2D parentRigidbody2D;
    
    // Start is called before the first frame update
    void Start() {
        // Get Components and set
        thisPolygonCollider2D = this.gameObject.GetComponent<PolygonCollider2D>();
        parentRigidbody2D = this.transform.parent.gameObject.GetComponent<Rigidbody2D>();

        this.transform.localScale = new Vector3(Random.Range(3f, 6f), Random.Range(3f, 6f), 1);
        thisPolygonCollider2D.attachedRigidbody.AddForce(new Vector2(10f, 10f));
    }

    // Update is called once per frame
    void Update() {
        if (Input.GetKeyDown(KeyCode.C)) {
            clearAll();
            return;
        }
    }

    void OnTriggerEnter2D(Collider2D other) {
        if (other.tag == "circle") {
            isFractureActive = true;
            triggers = new GameObject[triggerPointCount];
            for (int i = 0; i < triggerPointCount; i++) {
                float localScaleX = this.transform.localScale.x * 0.5f;
                float localScaleY = this.transform.localScale.y * 0.5f;
                tri.transform.localPosition = new Vector2(Random.Range(-localScaleX, localScaleX), Random.Range(-localScaleY, localScaleY));
                TriggerPoint triTriggerPoint = tri.GetComponent<TriggerPoint>();
                triTriggerPoint.vel = Vector2.zero;
                triTriggerPoint.power =0;
                triTriggerPoint.savePos = tri.transform.position;
                triggers[i] = Instantiate(tri, this.transform);
            }
            enterCount = 1;
        }
    }

    void OnTriggerExit2D(Collider2D other) {
        if (other.tag == "circle") {
            clearAll();
            isFractureActive = true;
            return;
        }
    }

    void OnTriggerStay2D(Collider2D other) {

        if (isFractureActive == false) return;

        if (other.tag != "circle") {
            return;
        }

        if (enterCount == 0) return;

        if (enterCount == firstProcessFrame) {
            int tempCount = 0;
            Vector2 sum = new Vector2(0, 0);
            if (triggers.Count(v => v.GetComponent<TriggerPoint>().enter == true) >= maxmumActiveTriggerPoint) {
                clearAll();
                return;
            }
            for (int i = 0; i < triggerPointCount; i++) {
                TriggerPoint triTriggerPoint = triggers[i].GetComponent<TriggerPoint>();
                if (triTriggerPoint.enter == true) {
                    triTriggerPoint.enter = false;
                    tempCount += 1;
                    triggeredIndex.Add(i);
                    sum += new Vector2(triggers[i].transform.localPosition.x, triggers[i].transform.localPosition.y);
                }
            }
            if (tempCount == 0) {
                enterCount = 0;
                clearAll();
                return;
            }
            firstCenter = sum / tempCount;
        }

        if (enterCount == secondProcessFrame) {
            int tempCount = 0;
            Vector2 sum = new Vector2(0, 0);
            for (int i = 0; i < triggerPointCount; i++) {
                if (triggers[i].GetComponent<TriggerPoint>().enter == true) {
                    triggers[i].GetComponent<TriggerPoint>().enter = false;
                    tempCount += 1;
                    sum += new Vector2(triggers[i].transform.localPosition.x, triggers[i].transform.localPosition.y);
                    triggeredIndex.Add(i);
                }
            }
            secondCentor = sum / tempCount;
        }

        enterCount += 1;

        if (enterCount == secondProcessFrame + 1) {

            Rigidbody2D otherRigidBody2D = other.gameObject.GetComponent<Rigidbody2D>();
            enterCount = 0;
            if (triggeredIndex.Count >= maxmumActiveTriggerPoint) {
                clearAll();
                return;
            }

            foreach(int i in triggeredIndex) {
                Vector2 powerVector = new Vector2(triggers[i].transform.localPosition.x, triggers[i].transform.localPosition.y) - firstCenter;
                powerVector += (secondCentor - firstCenter) * 1.0f;
                for (int j = 0; j < triggerPointCount; j++) {
                    Vector2 tempVec = new Vector2(triggers[j].transform.localPosition.x, triggers[j].transform.localPosition.y) - firstCenter;
                    float dot = Vector2.Dot(powerVector, tempVec);
                    float dist = Vector2.Distance(powerVector, tempVec);
                    if (dot > dotPowerThreshold){
                        triggers[j].GetComponent<TriggerPoint>().vel += powerVector * dot * minusExp(dist);
                    }
                    if (i == j) {
                        triggers[i].GetComponent<TriggerPoint>().vel += new Vector2((this.transform.position + triggers[j].transform.localPosition - other.transform.position).x, (this.transform.position + triggers[j].transform.localPosition - other.transform.position).y);
                    }
                }
            }

            float diffTriggerX = triggers.Max(v => v.transform.localPosition.x) - triggers.Min(v => v.transform.localPosition.x);
            float diffTriggerY = triggers.Max(v => v.transform.localPosition.y) - triggers.Min(v => v.transform.localPosition.y);

            triggers.All(v => {
                if (v.transform.localPosition.x > diffTriggerX * velClampMultiplier) {
                    v.transform.localPosition = new Vector3(diffTriggerX * velClampMultiplier, v.transform.localPosition.y, 0);
                }
                if (v.transform.localPosition.y > diffTriggerY * velClampMultiplier) {
                    v.transform.localPosition = new Vector3(v.transform.localPosition.x, diffTriggerY * velClampMultiplier, 0);
                }
                return v;
            });

            triggers.All(v => {
                TriggerPoint triTriggerPoint = v.GetComponent<TriggerPoint>();
                v.transform.localPosition += new Vector3(triTriggerPoint.vel.x, triTriggerPoint.vel.y, 0);
                return v;
            });

            float maxX = triggers.Max(v => v.transform.localPosition.x);
            float maxY = triggers.Max(v => v.transform.localPosition.y);
            float minX = triggers.Min(v => v.transform.localPosition.x);
            float minY = triggers.Min(v => v.transform.localPosition.y);

            float diffX = maxX - minX;
            float diffY = maxY - minY;

            int mulScale = (int)(clusteringGridScaleMultiplier / diffX);

            diffX *= mulScale;
            diffY *= mulScale;

            int gridX = (int)diffX + 10;
            int gridY = (int)diffY + 10;

            grid = new float[gridX * gridY];
            intGrid = new int[gridX * gridY];
            
            for (int i = 0; i < triggerPointCount; i++) {
                grid[GetIndex((triggers[i].transform.localPosition.x - minX) * mulScale + 5, (triggers[i].transform.localPosition.y - minY) * mulScale + 5, gridX)] += 1.0f;
                for (int x = -1; x <= 1; x++) {
                    for (int y = -1; y <= 1; y++) {
                        grid[GetIndex((triggers[i].transform.localPosition.x - minX) * mulScale + 5+x, (triggers[i].transform.localPosition.y - minY) * mulScale + 5+y, gridX)] += 1f;
                    }
                }
            }

            int powerCount = 1;

            for (int i = 0; i < intGrid.Length; i++) {
                intGrid[i] = 0;
            }

            List<int> sameA = new List<int>();
            List<int> sameB = new List<int>();

            for (int i = 1; i < gridX-1; i++) {
                for (int j = 1; j < gridY-1; j++) {
                    if (grid[GetIndex(i, j, gridX)] < 0.9f) {
                        intGrid[GetIndex(i, j, gridX)] = 0;
                        continue;
                    }
                    if (intGrid[GetIndex(i-1, j, gridX)] != 0 && intGrid[GetIndex(i, j-1, gridX)] != 0 && intGrid[GetIndex(i-1, j, gridX)] != intGrid[GetIndex(i, j-1, gridX)]){
                        sameA.Add(intGrid[GetIndex(i-1, j, gridX)]);
                        sameB.Add(intGrid[GetIndex(i, j-1, gridX)]);
                        intGrid[GetIndex(i, j, gridX)] = intGrid[GetIndex(i, j-1, gridX)];
                        continue;
                    }
                    if (intGrid[GetIndex(i-1, j, gridX)] != 0) {
                        intGrid[GetIndex(i, j, gridX)] = intGrid[GetIndex(i-1, j, gridX)];
                        continue;
                    }
                    if (intGrid[GetIndex(i, j-1, gridX)] != 0) {
                        intGrid[GetIndex(i, j, gridX)] = intGrid[GetIndex(i, j-1, gridX)];
                        continue;
                    }
                    intGrid[GetIndex(i, j, gridX)] = powerCount;
                    powerCount += 1;
                    continue;
                }
            }

            for (int s = 0; s < sameA.Count; s++) {
                for (int i = 1; i < gridX-1; i++) {
                    for (int j = 1; j < gridY-1; j++) {
                        if (intGrid[GetIndex(i, j, gridX)] == sameB[s]) {
                            intGrid[GetIndex(i, j, gridX)] = sameA[s];
                        }
                    }
                }
            }

            for (int i = 0; i < triggerPointCount; i++) {
                int tempIndex = GetIndex((triggers[i].transform.localPosition.x - minX) * mulScale + 5, (triggers[i].transform.localPosition.y - minY) * mulScale + 5, gridX);
                TriggerPoint triTriggerPoint = triggers[i].GetComponent<TriggerPoint>();
                triTriggerPoint.power = intGrid[tempIndex];
                if (intGrid[tempIndex] == 0) {
                    for (int x = -1; x <= 1; x++) {
                        for (int y = -1; y <= 1; y++) {
                            int offsetIndex = GetIndex((triggers[i].transform.localPosition.x - minX) * mulScale + 5+x, (triggers[i].transform.localPosition.y - minY) * mulScale + 5+y, gridX);
                            if (intGrid[offsetIndex] != 0) {
                                triTriggerPoint.power = intGrid[offsetIndex];
                                break;
                            }
                        }
                    }
                }
            }

            for (int p = 1; p < powerCount; p++) {
                Mesh mesh = new Mesh();
                List<Vector3> vList = new List<Vector3>();

                for (int i = 0; i < triggerPointCount; i++) {
                    TriggerPoint triTriggerPoint = triggers[i].GetComponent<TriggerPoint>();
                    if (triTriggerPoint.power == p) {
                        vList.Add(triTriggerPoint.savePos);
                    }
                }

                if (vList.Count < verticeMinimumCount) {
                    continue;
                }

                Vector3 averagePos = new Vector3(0, 0, 0);
                Vector3 closestToCenter = new Vector3(100, 100, 100);
                Vector3[] vertices = new Vector3[vList.Count];
                for (int i = 0; i < vList.Count; i++) {
                    vertices[i] = vList[i];
                    averagePos += vList[i];
                    if (vList[i].magnitude < closestToCenter.magnitude) {
                        closestToCenter = vList[i];
                    }
                }

                float smallestX = vList.Min(v => v.x);
                float largestX = vList.Max(v => v.x);
                float smallestY = vList.Min(v => v.y);
                float largestY = vList.Max(v => v.y);
                
                float tempDiffX = largestX - smallestX;
                float tempDiffY = largestY - smallestY;

                int separateX = 5;
                int separateY = 5;

                float multiplier = 1.0f;

                if (tempDiffX > tempDiffY) {
                    multiplier = (float)meshgridSeparation / tempDiffX;
                } else {
                    multiplier = (float)meshgridSeparation / tempDiffY;
                }

                separateX = (int)(tempDiffX * multiplier);
                separateY = (int)(tempDiffY * multiplier);

                bool[,] vert = new bool[separateX,separateY];
                
                for (int i = 0; i < vList.Count; i++) {
                    int tX = Mathf.FloorToInt((vList[i].x - smallestX) / tempDiffX * (float)separateX);
                    int tY = Mathf.FloorToInt((vList[i].y - smallestY) / tempDiffY * (float)separateY);
                    if (tX >= separateX) {
                        tX -= 1;
                    }
                    if (tY >= separateY) {
                        tY -= 1;
                    }
                    if (tX < 0) {
                        tX += 1;
                    }
                    if (tY < 0) {
                        tY += 1;
                    }
                    try {
                        vert[tX, tY] = true;
                    } catch {

                    }
                    
                }

                List<Vector3> list1 = new List<Vector3>();
                List<Vector3> list2 = new List<Vector3>();
                List<Vector3> list3 = new List<Vector3>();
                List<Vector3> list4 = new List<Vector3>();

                for (int x = 0; x < separateX; x++) {
                    int argX = -1;
                    int argY = -1;
                    int minArgX = -1;
                    int minArgY = -1;
                    for (int y = 0; y < Mathf.FloorToInt((float)separateY / 1.2f); y++) {
                        if (vert[x,y] == true) {
                            argX = x;
                            argY = y;
                            break;
                        }
                    }
                    for (int y = separateY-1; y > separateY-1 - Mathf.FloorToInt((float)separateY / 1.2f); y--) {
                        if (vert[x,y] == true) {
                            minArgX = x;
                            minArgY = y;
                            break;
                        }
                    }
                    if (argX != -1 && argY != -1) {
                        list1.Add(new Vector3(
                            smallestX + tempDiffX * ((float)argX / (float)separateX),
                            smallestY + tempDiffY * ((float)argY / (float)separateY),
                            0
                            ));
                    }
                    if (minArgX != -1 && minArgY != -1) {
                        list3.Prepend(new Vector3(
                            smallestX + tempDiffX * (minArgX / separateX),
                            smallestY + tempDiffY * (minArgY / separateY),
                            0
                            ));
                    }
                }

                for (int y = 0; y < separateY; y++) {
                    int argX = -1;
                    int argY = -1;
                    int minArgX = -1;
                    int minArgY = -1;
                    for (int x = 0; x < Mathf.FloorToInt((float)separateX / 1.2f); x++) {
                        if (vert[x,y] == true) {
                            argX = x;
                            argY = y;
                            break;
                        }
                    }
                    for (int x = separateX-1; x > separateX-1 - Mathf.FloorToInt((float)separateX / 1.2f); x--) {
                        if (vert[x,y] == true) {
                            minArgX = x;
                            minArgY = y;
                            break;
                        }
                    }
                    if (argX != -1 && argY != -1) {
                        list4.Prepend(new Vector3(
                            smallestX + tempDiffX * ((float)argX / (float)separateX),
                            smallestY + tempDiffY * ((float)argY / (float)separateY),
                            0
                            ));
                    }
                    if (minArgX != -1 && minArgY != -1) {
                        list2.Add(new Vector3(
                            smallestX + tempDiffX * (minArgX / separateX),
                            smallestY + tempDiffY * (minArgY / separateY),
                            0
                            ));
                    }
                }

                vertices = new Vector3[list1.Count + list2.Count + list3.Count + list4.Count];
                if (vertices.Length < verticeMinimumCount) {
                    continue;
                }

                for (int i = 0; i < list1.Count; i++) {
                    vertices[i] = list1[i];
                }
                int plus = list1.Count;
                for (int i = 0; i < list2.Count; i++) {
                    vertices[i + plus] = list2[i];
                }
                plus += list2.Count;
                for (int i = 0; i < list3.Count; i++) {
                    vertices[i + plus] = list3[i];
                }
                plus += list3.Count;
                for (int i = 0; i < list4.Count; i++) {
                    vertices[i + plus] = list4[i];
                }

                averagePos /= vList.Count;
                mesh.SetVertices(vertices);

                try {
                    int[] triangles = new int[(vertices.Length-2) * 3];
                    for (int i = 0; i < vertices.Length-2; i++) {
                        triangles[i*3] = 0;
                        triangles[i*3+1] = i+1;
                        triangles[i*3+2] = i+2;
                    }
                    mesh.SetTriangles(triangles, 0);
                } catch {

                }

                GameObject obj = Instantiate(polygon);

                obj.GetComponent<MeshFilter>().mesh = mesh;

                List<Vector2> edgeList = new List<Vector2>();
                for (int i = 0; i < vertices.Length; i++) {
                    edgeList.Add(new Vector2(vertices[i].x, vertices[i].y));
                }
                edgeList.Add(new Vector2(vertices[0].x, vertices[0].y));

                PolygonCollider2D objPolygonCollider2D = obj.AddComponent<PolygonCollider2D>();
                objPolygonCollider2D.SetPath(0, edgeList);

                Rigidbody2D objRigidbody2D = obj.AddComponent<Rigidbody2D>();

                obj.transform.position = this.transform.position + closestToCenter;
                objRigidbody2D.centerOfMass = averagePos;

                objRigidbody2D.velocity = 
                new Vector3(
                    parentRigidbody2D.velocity.x, 
                    parentRigidbody2D.velocity.y, 
                    0) 
                + 
                (this.transform.position + averagePos - other.transform.position).normalized * 
                    Vector3.Dot(
                        this.transform.position + averagePos - other.transform.position, 
                        otherRigidBody2D.velocity)
                    * 0.3f;
                objRigidbody2D.mass = 1f * ((float)vertices.Length / (float)triggerPointCount);
                objRigidbody2D.collisionDetectionMode = CollisionDetectionMode2D.Discrete;

                MeshRenderer objMeshRenderer = obj.GetComponent<MeshRenderer>();
                objMeshRenderer.material.color = Color.white;
                objMeshRenderer.material = glassMaterial;

                Physics2D.SyncTransforms();
            }

            Destroy(this.gameObject);

        }
    }

    private float minusExp(float x) {
        return Mathf.Pow(3, -x);
    }

    private int GetIndex(float x, float y, int width) {
        return width * (int)y + (int)x;
    }

    void clearAll() {
        isFractureActive = false;
        enterCount = 0;
        for (int i = 0; i < this.transform.childCount; i++) {
            Destroy(this.transform.GetChild(i).gameObject);
        }
        triggeredIndex = new List<int>();
        triggers = null;
        grid = null;
        intGrid = null;
    }
}

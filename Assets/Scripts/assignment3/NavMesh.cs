using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Mathematics;
using UnityEngine.Rendering;
using UnityEditor.Rendering;
using System.Data.Common;
using System.Net.Sockets;

public class NavMesh : MonoBehaviour
{
    // implement NavMesh generation here:
    //    the outline are Walls in counterclockwise order
    //    iterate over them, and if you find a reflex angle
    //    you have to split the polygon into two
    //    then perform the same operation on both parts
    //    until no more reflex angles are present
    //
    //    when you have a number of polygons, you will have
    //    to convert them into a graph: each polygon is a node
    //    you can find neighbors by finding shared edges between
    //    different polygons (or you can keep track of this while 
    //    you are splitting)
    
    private bool CollidingWalls(List<Wall> walls, Vector3 from, Vector3 to) {
        foreach (Wall w in walls) {
            if (w.start == from || w.end == to) {
                if (w.direction == (to-from).normalized) {
                    return true;
                }
            } else if (w.end == from || w.start == to) {
                if (w.direction == (from-to).normalized) {
                    return true;
                }
            } else {
                if (w.Crosses(from, to)) {
                    return true;
                }
            }
        }
        return false;
    }
    
    private (int, List<GraphNode>) SplitPolygon(int id, List<Wall> outline) {
        int oc = outline.Count;
        for (int i = 0; i < oc; i++) {
            Wall wall1 = outline[i];
            Wall wall2 = outline[(i + 1) % oc];
            float sangle = Vector3.SignedAngle(-wall1.direction, wall2.direction, Vector3.up);
            if (sangle < 0) {
                // search for where to insert wall
                Vector3 p1 = wall1.end;
                for (int j = oc >> 1; j > 1; j--) {
                    Vector3 p2 = outline[(i + j) % oc].end;
                    float sangle2 = Vector3.SignedAngle(-wall1.direction, (p2-p1), Vector3.up);
                    if ((sangle2 >= 0 || sangle2 < sangle) && !CollidingWalls(outline, p1, p2)) {
                        List<Wall> nol1 = new();
                        for (int k = i + 1; k <= i + j; k++) {
                            nol1.Add(outline[k % oc]);
                        }
                        nol1.Add(new Wall(p2,p1));
                        List<Wall> nol2 = new();
                        nol2.Add(new Wall(p1,p2));
                        for (int k = (i + j + 1) % oc; k != (i + 1) % oc; k = (k + 1) % oc) {
                            nol2.Add(outline[k % oc]);
                        }
                        (int id1, List<GraphNode> lg1) = SplitPolygon(id, nol1);
                        (int id2, List<GraphNode> lg2) = SplitPolygon(id1, nol2);
                        lg1.AddRange(lg2);
                        return (id2, lg1);
                    }
                }
                for (int j = (oc >> 1) + 1; j < oc - 1; j++) {
                    Vector3 p2 = outline[(i + j) % oc].end;
                    float sangle2 = Vector3.SignedAngle(-wall1.direction, (p2-p1), Vector3.up);
                    if ((sangle2 >= 0 || sangle2 < sangle) && !CollidingWalls(outline, p1, p2)) {
                        List<Wall> nol1 = new();
                        for (int k = i + 1; k <= (i + j); k++) {
                            nol1.Add(outline[k % oc]);
                        }
                        nol1.Add(new Wall(p2,p1));
                        List<Wall> nol2 = new();
                        nol2.Add(new Wall(p1,p2));
                        for (int k = (i + j + 1) % oc; k != (i + 1) % oc; k = (k + 1) % oc) {
                            nol2.Add(outline[k % oc]);
                        }
                        (int id1, List<GraphNode> lg1) = SplitPolygon(id, nol1);
                        (int id2, List<GraphNode> lg2) = SplitPolygon(id1, nol2);
                        lg1.AddRange(lg2);
                        return (id2, lg1);
                    }
                }
            }
        }
        List<GraphNode> lg = new();
        GraphNode g = new(id, outline);
        lg.Add(g);
        return (id + 1, lg);
    }
    public Graph MakeNavMesh(List<Wall> outline)
    {
        Graph g = new Graph();
        g.outline = outline;
        g.all_nodes = SplitPolygon(0, outline).Item2;
        foreach (GraphNode g1 in g.all_nodes) {
            foreach (GraphNode g2 in g.all_nodes) {
                if (g1.GetID() != g2.GetID()) {
                    for (int i = 0; i < g1.GetPolygon().Count; i++) {
                        Wall w1 = g1.GetPolygon()[i];
                        for (int j = 0; j < g2.GetPolygon().Count; j++) {
                            Wall w2 = g2.GetPolygon()[j];
                            if (w1.Same(w2)) {
                                g1.AddNeighbor(g2, i);
                                g2.AddNeighbor(g1, j);
                            }
                        }
                    }
                }
            }
        }
        return g;
    }

    List<Wall> outline;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        EventBus.OnSetMap += SetMap;
    }

    // Update is called once per frame
    void Update()
    {
       

    }

    public void SetMap(List<Wall> outline)
    {
        Graph navmesh = MakeNavMesh(outline);
        if (navmesh != null)
        {
            Debug.Log("got navmesh: " + navmesh.all_nodes.Count);
            EventBus.SetGraph(navmesh);
        }
    }

    


    
}

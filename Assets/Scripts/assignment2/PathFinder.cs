using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.InputSystem;

public class PathFinder : MonoBehaviour
{
    // Assignment 2: Implement AStar
    //
    // DO NOT CHANGE THIS SIGNATURE (parameter types + return type)
    // AStar will be given the start node, destination node and the target position, and should return 
    // a path as a list of positions the agent has to traverse to reach its destination, as well as the
    // number of nodes that were expanded to find this path
    // The last entry of the path will be the target position, and you can also use it to calculate the heuristic
    // value of nodes you add to your search frontier; the number of expanded nodes tells us if your search was
    // efficient
    //
    // Take a look at StandaloneTests.cs for some test cases
    private static Vector3 GetMidpoint(GraphNode start, GraphNode destination) {
        List<GraphNeighbor> neighbors = start.GetNeighbors();
        for (int i = 0; i < neighbors.Count; i++) {
            GraphNeighbor gn = neighbors[i];
            GraphNode nn = gn.GetNode();
            if (nn.GetID() == destination.GetID()) {
                return gn.GetWall().midpoint;
            }
        }
        return new Vector3(0,0,0);
    }

    private static float GetNeighborDist(GraphNode start, GraphNode destination) {
        if (start.GetID() == destination.GetID()) {
            return 0f;
        }
        Vector3 m = GetMidpoint(start, destination);
        float mag1 = (start.GetCenter() - m).magnitude;
        float mag2 = (destination.GetCenter() - m).magnitude;
        return mag1 + mag2;
    }



    public static (List<Vector3>, int) AStar(GraphNode start, GraphNode destination, Vector3 target)
    {
        // Implement A* here
        List<Vector3> path = new List<Vector3>() { target };

        if (start.GetID() == destination.GetID()) {
            return (path, 0);
        }

        int expanded = 0;
        HashSet<int> visited = new();
        Dictionary<int, float> nodedistpair = new();
        nodedistpair[start.GetID()] = 0;
        Dictionary<int, GraphNode> nodeparentpair = new();
        nodeparentpair[start.GetID()] = null;
        SortedList<float, Queue<GraphNode>> pq = new();
        Queue<GraphNode> q0 = new();
        q0.Enqueue(start);
        pq.Add(0f,q0);

        while (pq.Count > 0) {
            float key0 = pq.Keys[0];
            GraphNode node = pq[key0].Dequeue();
            bool v = visited.Add(node.GetID());
            if (v) {
                expanded++;
            }

            if (node.GetID() == destination.GetID()) {
                // Debug.Log("got path");
                GraphNode prev = node;
                while (node != null && node.GetID() != start.GetID()) {
                    prev = node;
                    node = nodeparentpair[node.GetID()];
                    path.Insert(0, GetMidpoint(node, prev));
                    // Debug.Log("pathlen++");
                }
                break;
            }

            if (pq[key0].Count == 0) {
                pq.RemoveAt(0);
            }

            float pastdist = nodedistpair[node.GetID()];

            List<GraphNeighbor> neighbors = node.GetNeighbors();
            for (int i = 0; i < neighbors.Count; i++) {
                GraphNeighbor gn = neighbors[i];
                GraphNode nn = gn.GetNode();
                if (!visited.Contains(nn.GetID())) {
                    float nndist = pastdist + GetNeighborDist(node, nn);
                    if (!(nodedistpair.ContainsKey(nn.GetID()) && (nodedistpair[nn.GetID()] <= nndist))) {
                        nodedistpair[nn.GetID()] = nndist;
                        nodeparentpair[nn.GetID()] = node;
                    }                    
                    float weight = nndist + (nn.GetCenter() - target).magnitude;
                    if (pq.ContainsKey(weight)) {
                        pq[weight].Enqueue(nn);
                    } else {
                        Queue<GraphNode> nq = new();
                        nq.Enqueue(nn);
                        pq.Add(weight, nq);
                    }
                }
            }
        }
        // return path and number of nodes expanded
        // Debug.Log(path.Count);
        return (path, expanded);

    }

    public Graph graph;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        EventBus.OnTarget += PathFind;
        EventBus.OnSetGraph += SetGraph;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetGraph(Graph g)
    {
        graph = g;
    }

    // entry point
    public void PathFind(Vector3 target)
    {
        if (graph == null) return;

        // find start and destination nodes in graph
        GraphNode start = null;
        GraphNode destination = null;
        foreach (var n in graph.all_nodes)
        {
            if (Util.PointInPolygon(transform.position, n.GetPolygon()))
            {
                start = n;
            }
            if (Util.PointInPolygon(target, n.GetPolygon()))
            {
                destination = n;
            }
        }
        if (destination != null)
        {
            // only find path if destination is inside graph
            EventBus.ShowTarget(target);
            (List<Vector3> path, int expanded) = PathFinder.AStar(start, destination, target);

            Debug.Log("found path of length " + path.Count + " expanded " + expanded + " nodes, out of: " + graph.all_nodes.Count);
            EventBus.SetPath(path);
        }
        

    }

    

 
}

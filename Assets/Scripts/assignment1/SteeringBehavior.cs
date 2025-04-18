using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Numerics;
using Vector3 = UnityEngine.Vector3;
using System;
using Unity.Mathematics;
using Unity.VisualScripting;
using System.Data;

public class SteeringBehavior : MonoBehaviour
{
    public Vector3 target;
    public KinematicBehavior kinematic;
    public List<Vector3> path;
    // you can use this label to show debug information,
    // like the distance to the (next) target
    public TextMeshProUGUI label;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        kinematic = GetComponent<KinematicBehavior>();
        target = transform.position;
        path = null;
        EventBus.OnSetMap += SetMap;
    }
    int frameCount = 0;

    // Update is called once per frame
    void Update()
    {
        // Assignment 1: If a single target was set, move to that target
        //                If a path was set, follow that path ("tightly")

        // you can use kinematic.SetDesiredSpeed(...) and kinematic.SetDesiredRotationalVelocity(...)
        //    to "request" acceleration/decceleration to a target speed/rotational velocity


        Vector3 currentTarget = target;
        if (path != null && path.Count > 0) {
            currentTarget = path[0];
        }

        Vector3 forward = transform.forward;
        Vector3 toTarget = currentTarget - transform.position;
        toTarget.y = 0;
        
        float angleErr = Vector3.SignedAngle(forward, toTarget, Vector3.up);
        float pureRotSpeed = math.sqrt(2 * kinematic.rotational_acceleration * math.abs(angleErr)) / kinematic.max_rotational_velocity;
        float proportionalRotSpeed = math.abs(angleErr) * 0.001f;
        float rotMultiplier = math.min(1, pureRotSpeed + proportionalRotSpeed);
        if (rotMultiplier  < 0.05f) {
            rotMultiplier = 0;
        }
        float drv = kinematic.max_rotational_velocity * math.sign(angleErr) * rotMultiplier;

        
        float distance = toTarget.magnitude;
        float pureSpeed = math.sqrt(2 * kinematic.linear_acceleration * distance) / kinematic.max_speed;
        float pureProportionalSpeed = distance * 0.001f;
        float rotRatio = math.max(0.3f, (180 - math.abs(angleErr)) / 60 - 2);
        float speedMultiplier = math.min(1, (pureSpeed + pureProportionalSpeed)) * rotRatio;
        if (speedMultiplier  < 0.05f) {
            speedMultiplier = 0;
        }
        float ds = kinematic.max_speed * speedMultiplier;

        frameCount++;
        if (frameCount % 60 == 0) {
            Debug.Log(target);
            // Debug.Log(pureSpeed.ToString() + " " + pureProportionalSpeed.ToString() + " " + rotRatio.ToString() + " " + speedMultiplier.ToString() + " " + ds.ToString());
        }

        kinematic.SetDesiredRotationalVelocity(drv);
        kinematic.SetDesiredSpeed(ds);

        if (toTarget.magnitude < 2.5f) {
            // Debug.Log("reached point");
            if (path != null && path.Count > 0) {
                path.RemoveAt(0);
            } else {
                SetTarget(transform.position);
            }
        }
    }

    public void SetTarget(Vector3 target)
    {
        this.target = target;
        EventBus.ShowTarget(target);
    }

    public void SetPath(List<Vector3> path)
    {
        this.path = path;
    }

    public void SetMap(List<Wall> outline)
    {
        this.path = null;
        this.target = transform.position;
    }
}

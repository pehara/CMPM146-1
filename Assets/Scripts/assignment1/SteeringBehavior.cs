using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Numerics;
using Vector3 = UnityEngine.Vector3;
using System;
using Unity.Mathematics;
using Unity.VisualScripting;
using System.Data;
using Unity.PlasticSCM.Editor.WebApi;

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

    private float getPureSpeed(Vector3 pos1, Vector3 pos2, float speedAtPos2) {
        float distance = (pos2 - pos1).magnitude;
        return math.sqrt(2 * kinematic.linear_acceleration * distance + speedAtPos2 * speedAtPos2) / kinematic.max_speed;
    }
    private float getRotRatio(float angle) {
        return math.max(0.3f, (180 - math.abs(angle)) / 60 - 2);
    }
    private void MoveToPos(Vector3 pos, float speedAtPos) {
        Vector3 forward = transform.forward;
        Vector3 toTarget = pos - transform.position;
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
        float pureSpeed = getPureSpeed(transform.position, pos, speedAtPos);
        float pureProportionalSpeed = distance * 0.001f;
        float rotRatio = getRotRatio(angleErr);
        float speedMultiplier = math.min(1, pureSpeed + pureProportionalSpeed) * rotRatio;
        if (speedMultiplier  < 0.05f) {
            speedMultiplier = 0;
        }
        float ds = kinematic.max_speed * speedMultiplier;

        kinematic.SetDesiredRotationalVelocity(drv);
        kinematic.SetDesiredSpeed(ds);
    }

    // Update is called once per frame
    void Update()
    {
        // Assignment 1: If a single target was set, move to that target
        //                If a path was set, follow that path ("tightly")

        // you can use kinematic.SetDesiredSpeed(...) and kinematic.SetDesiredRotationalVelocity(...)
        //    to "request" acceleration/decceleration to a target speed/rotational velocity


        Vector3 currentTarget = target;
        float speedAtTarget = 0;
        if (path != null && path.Count > 0) {
            currentTarget = path[0];
            if (path.Count > 2) {
                Vector3 toTarget = currentTarget - transform.position;
                toTarget.y = 0;
                Vector3 targetToNext = path[2] - currentTarget;
                targetToNext.y = 0;
                float rotRatioAtTarget = getRotRatio(Vector3.Angle(toTarget, targetToNext));
                float pureSpeedAtTarget = math.min(1, getPureSpeed(currentTarget, path[2], 0));
                speedAtTarget = rotRatioAtTarget * pureSpeedAtTarget * kinematic.max_speed;
            }
        }
        MoveToPos(currentTarget, speedAtTarget);

        if ((currentTarget - transform.position).magnitude < 2.5f) {
            // Debug.Log("reached point");
            if (path != null && path.Count > 0) {
                path.RemoveAt(0);
                if (path.Count == 0) {
                    this.target = target;
                    EventBus.SetTarget(transform.position);
                }
            } else {
                this.target = target;
                EventBus.SetTarget(transform.position);
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

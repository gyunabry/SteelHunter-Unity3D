using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(FieldOfView))]
public class FieldOfViewEditor : Editor
{
    void OnSceneGUI()
    {
        FieldOfView fow = (FieldOfView)target;
        Vector3 origin = fow.ViewOrigin;
        Handles.color = Color.white;
        Handles.DrawWireArc(origin, Vector3.up, Vector3.forward, 360, fow.ViewRadius);
        Vector3 viewAngleA = fow.DirFromAngle(-fow.viewAngle / 2, false);
        Vector3 viewAngleB = fow.DirFromAngle(fow.viewAngle / 2, false);

        Handles.DrawLine(origin, origin + viewAngleA * fow.ViewRadius);
        Handles.DrawLine(origin, origin + viewAngleB * fow.ViewRadius);

        Handles.color = Color.red;
        foreach (Transform visible in fow.VisibleTargets)
        {
            Handles.DrawLine(origin, visible.transform.position);
        }
    }
}
using Lean.Pool;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetIndicateHandeler : MonoBehaviour
{
    [SerializeField]LineRenderer lineRendererTemplete;
    LineRenderer lineRenderer;
    List<LineRenderer> lineRenderers = new List<LineRenderer>();
    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }
    public void DrawIndicate(Vector3 origin,Vector3 target,int divide,bool createInstance=false)
    {
        //함수로 라인 그래프를 형성한다 사인 커브면 될것같음.
        List<Vector3> vector3s= new List<Vector3>();
        for (int i = 0; i < divide; i++)
        {
            InfiniteLoopDetector.Run();
            float divalue = i / (float)divide;
            Vector3 v=new Vector3();
            v=Vector3.Lerp(origin, target, divalue);
            v.y += Mathf.Sin(divalue*Mathf.PI)*Vector3.Distance(origin,target)/2;
            vector3s.Add(v);
        }
        LineRenderer current=null;
        if(createInstance)
        {
            var instance = LeanPool.Spawn(lineRendererTemplete, transform);
            current = instance;
            lineRenderers.Add(current);
        }
        else
        {
            current = lineRenderer;
        }
        current.positionCount = vector3s.Count;
        current.SetPositions(vector3s.ToArray());

    }
    public void ClearInstances()
    {
        foreach (var l in lineRenderers)
        {
            lineRenderers.Remove(l);
            LeanPool.Despawn(l);
        }
    }
    //private void Update() 
    //{
    //    DrawIndicate(Vector3.zero, RayCast().point,10);
    //    if (Input.GetMouseButtonDown(0))
    //    {
    //        DrawIndicate(Vector3.zero, RayCast().collider.transform.position,10,true);
    //    }
    //}

}

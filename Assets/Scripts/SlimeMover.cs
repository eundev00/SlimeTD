using UnityEngine;
using UnityEngine.Splines;

public class SlimeMover : MonoBehaviour
{
    private void Start()
    {
        var splineAnimate = GetComponent<SplineAnimate>();
        splineAnimate.Completed += () => Debug.Log("경로 끝 도달!");
    }
}

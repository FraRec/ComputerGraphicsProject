using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "OceanSettings", menuName = "Ocean/Settings")]
public class OceanSettings : ScriptableObject {
    public int _L;
    public float _A;
    public float _WindSpeed;
    public float _Gravity = 9.81f;
    public Vector2 _WindDirection;
}


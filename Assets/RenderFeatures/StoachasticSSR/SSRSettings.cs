using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[CreateAssetMenu(fileName = "StochasticSSR", menuName = "SSRSettings")]
public class SSRSettings : ScriptableObject {
    [SerializeField]
    [Range(1, 4)]
    public int NumResolve = 4;

    [SerializeField]
    [Range(1, 4)]
    public int NumPrefilter = 4;

    [SerializeField]
    public Texture2D BlurTexture;

    [SerializeField]
    public Texture2D BRDFTexture;
 
}

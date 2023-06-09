using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

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
    public Texture2D NoiseTexture;

    [SerializeField]
    public Texture2D BRDFTexture;

    [SerializeField] 
    public int PassEvent = (int)RenderPassEvent.AfterRenderingPostProcessing;

    [SerializeField] [Range(0, 1)] 
    public float BRDFBias = 0.7f;
}

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class CutoutMaskUI : Image
{
    private static readonly int StencilComp = Shader.PropertyToID("_StencilComp");

    public override Material materialForRendering
    {
        get
        {
            var mat = new Material(base.materialForRendering);
            mat.SetInt(StencilComp, (int)CompareFunction.NotEqual);
            return mat;
        }
    }
}

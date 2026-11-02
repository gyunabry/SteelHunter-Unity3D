using UnityEngine;
using UnityEngine.Rendering;

public class FogOfWarController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera visibilityCamera;
    [SerializeField] private RenderTexture visibilityTexture;
    [SerializeField] private Material compositeMaterial;
    [SerializeField] private Shader blurShader;

    [Header("Softness")]
    [Tooltip("블러가 주변을 읽는 최대 거리")]
    [SerializeField, Min(0f)]
    private float blurRadiusWorld = 1f;

    [Tooltip("변화한 시야를 시간적으로 얼마나 천천히 따라갈 것인가?")]
    [SerializeField, Min(0f)]
    private float transitionSeconds = 0.08f;

    private Material blurMaterial;

    private RenderTexture horizontal;
    private RenderTexture blurred;
    private RenderTexture history;
    private RenderTexture nextHistory;

    private bool initialized;
    private Vector4 previousWorldRect;

    private void OnEnable()
    {
        if (visibilityCamera == null ||
            visibilityTexture == null ||
            compositeMaterial == null ||
            blurShader == null)
        {
            Debug.LogError(
                "Fog Of War의 Camera, Texture, Material, Shader를 연결해 주세요.",
                this);

            enabled = false;
            return;
        }

        blurMaterial = new Material(blurShader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        // 스크립트 참조뿐 아니라 카메라 출력도 연결합니다.
        visibilityCamera.targetTexture = visibilityTexture;

        CreateBuffers();

        compositeMaterial.SetTexture(
            "_FogTex", Texture2D.blackTexture);

        RenderPipelineManager.endCameraRendering += OnCameraRendered;
    }

    private RenderTexture CreateBuffer(string textureName)
    {
        // 시야는 색상이 아닌 데이터이므로 Linear로 저장합니다.
        var texture = new RenderTexture(
            visibilityTexture.width,
            visibilityTexture.height,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Linear)
        {
            name = textureName,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
            autoGenerateMips = false,
            antiAliasing = 1
        };

        texture.Create();
        return texture;
    }

    private void CreateBuffers()
    {
        horizontal = CreateBuffer("Fog Horizontal");
        blurred = CreateBuffer("Fog Blurred");
        history = CreateBuffer("Fog History");
        nextHistory = CreateBuffer("Fog Next History");

        initialized = false;
    }

    private void OnCameraRendered(
        ScriptableRenderContext context,
        Camera renderedCamera)
    {
        if (renderedCamera != visibilityCamera)
            return;

        // 실행 중 해상도가 바뀌었을 때 다시 생성합니다.
        if (horizontal.width != visibilityTexture.width ||
            horizontal.height != visibilityTexture.height)
        {
            ReleaseBuffers();
            CreateBuffers();
        }

        // Rotation (90, 0, 0)의 Orthographic 카메라 기준입니다.
        float worldHeight = visibilityCamera.orthographicSize * 2f;
        float worldWidth = worldHeight * visibilityCamera.aspect;

        Vector3 cameraPosition = visibilityCamera.transform.position;

        var worldRect = new Vector4(
            cameraPosition.x,
            cameraPosition.z,
            worldWidth,
            worldHeight);

        // 촬영 영역이 바뀌면 이전 텍스처와 좌표가 달라집니다.
        // 잘못된 잔상을 남기지 않도록 시간 보간을 초기화합니다.
        if (worldRect != previousWorldRect)
            initialized = false;

        previousWorldRect = worldRect;

        // 가장 먼 샘플이 중심에서 4칸 떨어져 있으므로 4로 나눕니다.
        float stepU = blurRadiusWorld /
                      Mathf.Max(worldWidth, 0.001f) / 4f;

        float stepV = blurRadiusWorld /
                      Mathf.Max(worldHeight, 0.001f) / 4f;

        blurMaterial.SetVector(
            "_StepUV",
            new Vector4(stepU, stepV, 0f, 0f));

        RenderTexture savedActive = RenderTexture.active;
        bool savedSRGBWrite = GL.sRGBWrite;

        try
        {
            GL.sRGBWrite = false;

            // 1. 가로 블러
            Graphics.Blit(
                visibilityTexture,
                horizontal,
                blurMaterial,
                0);

            // 2. 세로 블러
            Graphics.Blit(
                horizontal,
                blurred,
                blurMaterial,
                1);

            if (!initialized || transitionSeconds <= 0f)
            {
                Graphics.Blit(blurred, history);
                initialized = true;
            }
            else
            {
                // 프레임 속도에 덜 의존하는 지수 보간입니다.
                float response = 1f - Mathf.Exp(
                    -3f * Time.unscaledDeltaTime /
                    Mathf.Max(transitionSeconds, 0.001f));

                blurMaterial.SetTexture("_HistoryTex", history);
                blurMaterial.SetFloat("_Response", response);

                // 3. 이전 결과와 현재 시야 혼합
                Graphics.Blit(
                    blurred,
                    nextHistory,
                    blurMaterial,
                    2);

                // 읽기용과 쓰기용 텍스처를 교환합니다.
                RenderTexture temporary = history;
                history = nextHistory;
                nextHistory = temporary;
            }
        }
        finally
        {
            RenderTexture.active = savedActive;
            GL.sRGBWrite = savedSRGBWrite;
        }

        compositeMaterial.SetTexture("_FogTex", history);
        compositeMaterial.SetVector("_WorldRect", worldRect);
    }

    private void OnDisable()
    {
        RenderPipelineManager.endCameraRendering -= OnCameraRendered;

        if (compositeMaterial != null)
        {
            compositeMaterial.SetTexture(
                "_FogTex", Texture2D.blackTexture);
        }

        ReleaseBuffers();

        if (blurMaterial != null)
        {
            Destroy(blurMaterial);
            blurMaterial = null;
        }
    }

    private void ReleaseBuffers()
    {
        ReleaseBuffer(ref horizontal);
        ReleaseBuffer(ref blurred);
        ReleaseBuffer(ref history);
        ReleaseBuffer(ref nextHistory);

        initialized = false;
    }

    private void ReleaseBuffer(ref RenderTexture texture)
    {
        if (texture == null)
            return;

        texture.Release();
        Destroy(texture);
        texture = null;
    }
}
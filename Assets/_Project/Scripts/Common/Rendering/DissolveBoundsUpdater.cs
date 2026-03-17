using UnityEngine;

/// <summary>
/// SpriteLitDissolve 셰이더의 디졸브 효과를 에디터에서 미리보기하기 위한 컴포넌트.
/// 런타임에서는 동작하지 않음 — 런타임 바운드 세팅은 SpriteCharacterView.SetDisolveShader()에서 1회 수행.
/// </summary>
[ExecuteAlways, RequireComponent(typeof(SpriteRenderer))]
public class DissolveBoundsUpdater : MonoBehaviour
{
#if UNITY_EDITOR
    private static readonly int BoundsMinID = Shader.PropertyToID("_BoundsMin");
    private static readonly int BoundsMaxID = Shader.PropertyToID("_BoundsMax");
    private static readonly int DissolveID = Shader.PropertyToID("_Dissolve");
    private static readonly int DissolveDirectionID = Shader.PropertyToID("_DissolveDirection");
    private static readonly int DirectionStrengthID = Shader.PropertyToID("_DirectionStrength");

    [Header("Editor Preview")]
    [Tooltip("디졸브 미리보기 활성화")]
    public bool enablePreview = false;

    [Range(0f, 1f)]
    [Tooltip("디졸브 진행도 (0 = 완전 표시, 1 = 완전 사라짐)")]
    public float previewDissolve = 0f;

    [Tooltip("디졸브 방향 (정규화됨)")]
    public Vector2 previewDirection = new Vector2(0f, 1f);

    [Range(0f, 1f)]
    [Tooltip("방향 강도 (0 = 노이즈만, 1 = 방향만)")]
    public float previewDirectionStrength = 0.5f;

    [Header("Bounds Override")]
    [Tooltip("수동 바운드 설정 사용")]
    public bool useManualBounds = false;

    [Tooltip("수동 바운드 - 최소값 (로컬 좌표)")]
    public Vector2 manualBoundsMin = new Vector2(-0.5f, -0.5f);

    [Tooltip("수동 바운드 - 최대값 (로컬 좌표)")]
    public Vector2 manualBoundsMax = new Vector2(0.5f, 0.5f);

    private Renderer _renderer;
    private SpriteRenderer _spriteRenderer;
    private MaterialPropertyBlock _mpb;

    private void OnEnable()
    {
        if (Application.isPlaying) return;
        CacheComponents();
        UpdatePreview();
    }

    private void OnDisable()
    {
        if (Application.isPlaying) return;
        ResetPreview();
    }

    private void Update()
    {
        if (Application.isPlaying) return;
        if (enablePreview)
        {
            UpdatePreview();
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying) return;
        CacheComponents();
        if (enablePreview)
            UpdatePreview();
        else
            ResetPreview();
    }

    private void CacheComponents()
    {
        _renderer = GetComponent<Renderer>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _mpb ??= new MaterialPropertyBlock();
    }

    private void UpdatePreview()
    {
        if (_renderer == null || _mpb == null) return;

        Vector4 boundsMin, boundsMax;
        if (useManualBounds)
        {
            boundsMin = new Vector4(manualBoundsMin.x, manualBoundsMin.y, 0, 0);
            boundsMax = new Vector4(manualBoundsMax.x, manualBoundsMax.y, 0, 0);
        }
        else
        {
            Bounds localBounds = GetLocalBounds();
            boundsMin = new Vector4(localBounds.min.x, localBounds.min.y, localBounds.min.z, 0);
            boundsMax = new Vector4(localBounds.max.x, localBounds.max.y, localBounds.max.z, 0);
        }

        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetVector(BoundsMinID, boundsMin);
        _mpb.SetVector(BoundsMaxID, boundsMax);
        _mpb.SetFloat(DissolveID, previewDissolve);
        _mpb.SetVector(DissolveDirectionID, new Vector4(previewDirection.x, previewDirection.y, 0, 0));
        _mpb.SetFloat(DirectionStrengthID, previewDirectionStrength);
        _renderer.SetPropertyBlock(_mpb);
    }

    private void ResetPreview()
    {
        if (_renderer == null) return;
        _mpb ??= new MaterialPropertyBlock();
        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(DissolveID, 0f);
        _renderer.SetPropertyBlock(_mpb);
    }

    private Bounds GetLocalBounds()
    {
        if (_spriteRenderer != null && _spriteRenderer.sprite != null)
            return _spriteRenderer.sprite.bounds;

        if (useManualBounds)
        {
            Vector3 center = new Vector3(
                (manualBoundsMin.x + manualBoundsMax.x) * 0.5f,
                (manualBoundsMin.y + manualBoundsMax.y) * 0.5f,
                0f);
            Vector3 size = new Vector3(
                manualBoundsMax.x - manualBoundsMin.x,
                manualBoundsMax.y - manualBoundsMin.y,
                0f);
            return new Bounds(center, size);
        }

        return new Bounds(Vector3.zero, Vector3.one);
    }

    private void OnDrawGizmosSelected()
    {
        if (_renderer == null) CacheComponents();

        Bounds bounds;
        if (useManualBounds)
        {
            Vector3 center = (manualBoundsMin + manualBoundsMax) * 0.5f;
            Vector3 size = manualBoundsMax - manualBoundsMin;
            bounds = new Bounds(center, size);
        }
        else
        {
            bounds = GetLocalBounds();
        }

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(bounds.center, bounds.size);

        // 디졸브 방향 화살표 표시
        if (enablePreview)
        {
            Gizmos.color = Color.cyan;
            Vector3 dirStart = bounds.center - (Vector3)(previewDirection.normalized * bounds.extents.magnitude * 0.5f);
            Vector3 dirEnd = bounds.center + (Vector3)(previewDirection.normalized * bounds.extents.magnitude * 0.5f);
            Gizmos.DrawLine(dirStart, dirEnd);
            // 화살촉
            Vector2 perp = Vector2.Perpendicular(previewDirection.normalized) * 0.05f;
            Gizmos.DrawLine(dirEnd, dirEnd - (Vector3)(previewDirection.normalized * 0.1f) + (Vector3)perp);
            Gizmos.DrawLine(dirEnd, dirEnd - (Vector3)(previewDirection.normalized * 0.1f) - (Vector3)perp);
        }
    }
#endif
}
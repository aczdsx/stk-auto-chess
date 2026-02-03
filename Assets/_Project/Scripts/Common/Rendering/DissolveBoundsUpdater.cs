using UnityEngine;

/// <summary>
/// SpriteLitDissolve 쉐이더에 바운드 정보를 전달하는 컴포넌트
/// SpriteRenderer 또는 MeshRenderer가 있는 오브젝트에 추가
/// </summary>
[ExecuteAlways, RequireComponent(typeof(SpriteRenderer))]
public class DissolveBoundsUpdater : MonoBehaviour
{
    private static readonly int BoundsMinID = Shader.PropertyToID("_BoundsMin");
    private static readonly int BoundsMaxID = Shader.PropertyToID("_BoundsMax");

    [Header("Settings")]
    [Tooltip("자동으로 바운드 업데이트 (매 프레임)")]
    public bool autoUpdate = true;

    [Tooltip("수동 바운드 설정 사용")]
    public bool useManualBounds = false;

    [Tooltip("수동 바운드 - 최소값 (로컬 좌표)")]
    public Vector2 manualBoundsMin = new Vector2(-0.5f, -0.5f);

    [Tooltip("수동 바운드 - 최대값 (로컬 좌표)")]
    public Vector2 manualBoundsMax = new Vector2(0.5f, 0.5f);

    private Renderer _renderer;
    private SpriteRenderer _spriteRenderer;
    private MaterialPropertyBlock _mpb;

    private void Awake()
    {
        CacheComponents();
    }

    private void CacheComponents()
    {
        _renderer = GetComponent<Renderer>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _mpb = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        UpdateBounds();
    }

    private void LateUpdate()
    {
        if (autoUpdate)
        {
            UpdateBounds();
        }
    }

    /// <summary>
    /// 바운드 정보를 쉐이더에 업데이트
    /// </summary>
    public void UpdateBounds()
    {
        if (_mpb == null)
        {
            _mpb = new MaterialPropertyBlock();
        }

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
        _renderer.SetPropertyBlock(_mpb);
    }

    /// <summary>
    /// 메쉬의 로컬 바운드를 가져옴 (positionOS와 일치하는 좌표계)
    /// Sprite가 없으면 수동 바운드 또는 기본 바운드를 반환하여 NRE를 방지합니다.
    /// </summary>
    private Bounds GetLocalBounds()
    {
        if (_spriteRenderer == null)
        {
            return GetFallbackBounds();
        }

        Sprite sprite = _spriteRenderer.sprite;
        if (sprite == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[DissolveBoundsUpdater] {gameObject.name}: SpriteRenderer.sprite가 할당되지 않았습니다. 수동 바운드 또는 기본값을 사용합니다.", this);
#endif
            return GetFallbackBounds();
        }

        return sprite.bounds;
    }

    /// <summary>
    /// Sprite를 사용할 수 없을 때의 fallback 바운드 (수동 설정 우선, 없으면 단위 정육면체)
    /// </summary>
    private Bounds GetFallbackBounds()
    {
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

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            CacheComponents();
            UpdateBounds();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Bounds bounds;
        if (useManualBounds)
        {
            Vector3 center = (manualBoundsMin + manualBoundsMax) * 0.5f;
            Vector3 size = manualBoundsMax - manualBoundsMin;
            bounds = new Bounds(center, size);
        }
        else
        {
            if (_renderer == null) CacheComponents();
            bounds = GetLocalBounds();
        }

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
#endif
}

using UnityEngine;

public class TowerRangeIndicator : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    [NotNull][SerializeField] private ParticleSystem _rangeParticle;
    [SerializeField] private Color _defaultColor = new Color(0.345f, 0.858f, 0.271f, 1f);
    [SerializeField] private Color _validColor = new Color(0.25f, 0.6f, 1f, 1f);
    [SerializeField] private Color _invalidColor = new Color(1f, 0.3f, 0.25f, 1f);

    private Material _materialInstance;

    private void Awake()
    {
        Hide();
        ResetColor();
    }

    private void OnDestroy()
    {
        if (_materialInstance != null)
        {
            Destroy(_materialInstance);
            _materialInstance = null;
        }
    }

    public void SetValid(bool isValid)
    {
        SetColor(isValid ? _validColor : _invalidColor);
    }

    public void ResetColor()
    {
        SetColor(_defaultColor);
    }

    private void SetColor(Color color)
    {
        var material = GetMaterialInstance();
        if (material == null)
            return;

        material.SetColor(BaseColorId, color);
    }

    private Material GetMaterialInstance()
    {
        if (_materialInstance != null)
            return _materialInstance;

        if (_rangeParticle == null)
            return null;

        var particleRenderer = _rangeParticle.GetComponent<ParticleSystemRenderer>();
        if (particleRenderer == null)
        {
            Debug.Log("[TowerRangeIndicator] ParticleSystemRenderer를 찾을 수 없습니다.", this);
            return null;
        }

        // sharedMaterial은 4개 에셋이 공유하므로 건드리면 에셋이 영구 오염된다.
        _materialInstance = particleRenderer.material;
        return _materialInstance;
    }

    public void UpdateRangeVisual(float range)
    {
        _rangeParticle.transform.localScale = Vector3.one * range;
    }

    public void Show()
    {
        if (_rangeParticle == null)
            return;

        _rangeParticle.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (_rangeParticle == null)
            return;

        _rangeParticle.gameObject.SetActive(false);
    }
}

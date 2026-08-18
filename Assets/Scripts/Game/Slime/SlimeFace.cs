using Cysharp.Threading.Tasks;
using System;
using UniRx;
using UnityEngine;

public class SlimeFace : MonoBehaviour, IPoolable
{
    [SerializeField] private SkinnedMeshRenderer _renderer;
    [SerializeField] private Material _normalFace;
    [SerializeField] private Material _damagedFace;
    [SerializeField] private Material _deadFace;
    [SerializeField] private float _damagedFaceDuration = 0.3f;

    private BaseSlime _baseSlime;
    private CompositeDisposable _disposables;

    #region Unity Lifecycle

    private void Awake()
    {
        _baseSlime = GetComponentInParent<BaseSlime>();
        if (_baseSlime == null)
        {
            Debug.LogError("[SlimeFace] BaseSlime 컴포넌트를 찾을 수 없습니다.", this);
        }

        if (_renderer == null)
        {
            Debug.LogError("[SlimeFace] _renderer가 연결되지 않았습니다.", this);
        }
    }

    #endregion

    #region IPoolable

    public void OnGetFromPool()
    {
        _disposables = new CompositeDisposable();

        SetNormalFace();

        if (_baseSlime?.Stats?.CurrentHealth == null)
            return;

        // 체력 변화 감지 - 스스로 표정 변경
        _baseSlime.Stats.CurrentHealth
            .Pairwise()
            .Where(pair => pair.Previous > pair.Current)
            .Subscribe(pair =>
            {
                if (pair.Current > 0)
                {
                    SetDamagedFaceAsync().Forget();
                }
                else
                {
                    SetDeadFace();
                }
            })
            .AddTo(_disposables);
    }

    public void OnReturnToPool()
    {
        _disposables?.Dispose();
        _disposables = null;
    }

    #endregion

    public void SetNormalFace()
    {
        if (_renderer == null || _normalFace == null)
            return;

        _renderer.sharedMaterial = _normalFace;
    }

    public async UniTaskVoid SetDamagedFaceAsync()
    {
        if (_renderer == null || _damagedFace == null)
            return;

        _renderer.sharedMaterial = _damagedFace;

        await UniTask.Delay(
            TimeSpan.FromSeconds(_damagedFaceDuration),
            cancellationToken: this.GetCancellationTokenOnDestroy());

        if (_renderer != null && _normalFace != null)
        {
            _renderer.sharedMaterial = _normalFace;
        }
    }

    public void SetDeadFace()
    {
        if (_renderer == null || _deadFace == null)
            return;

        _renderer.sharedMaterial = _deadFace;
    }
}

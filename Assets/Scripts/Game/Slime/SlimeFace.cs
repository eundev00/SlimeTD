using Cysharp.Threading.Tasks;
using System;
using UniRx;
using UnityEngine;

public class SlimeFace : MonoBehaviour, IPoolable
{
    [NotNull][SerializeField] private SkinnedMeshRenderer _renderer;
    [NotNull][SerializeField] private Material _normalFace;
    [NotNull][SerializeField] private Material _damagedFace;
    [NotNull][SerializeField] private Material _deadFace;
    [SerializeField] private float _damagedFaceDuration = 0.3f;

    private BaseSlime _baseSlime;
    private CompositeDisposable _disposables;


    private void Awake()
    {
        _baseSlime = GetComponentInParent<BaseSlime>();
        if (_baseSlime == null)
        {
            Debug.Log("[SlimeFace] BaseSlime 컴포넌트를 찾을 수 없습니다.", this);
        }

        if (_renderer == null)
        {
            Debug.Log("[SlimeFace] _renderer가 연결되지 않았습니다.", this);
        }
    }



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

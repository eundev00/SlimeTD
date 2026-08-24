using UniRx;
using UnityEngine;

public class SlimeAnimation : MonoBehaviour, IPoolable
{
    private BaseSlime _baseSlime;
    private Animator _animator;
    private CompositeDisposable _disposables;

    private static readonly int MoveStateHash = Animator.StringToHash("Move");
    private static readonly int HitStateHash = Animator.StringToHash("Hit");
    private static readonly int DieStateHash = Animator.StringToHash("Die");


    private void Awake()
    {
        _baseSlime = GetComponent<BaseSlime>();
        if (_baseSlime == null)
        {
            Debug.Log("[SlimeAnimation] BaseSlime 컴포넌트가 없습니다.", this);
            return;
        }

        _animator = GetComponentInChildren<Animator>();
        if (_animator == null)
        {
            Debug.Log("[SlimeAnimation] Animator 컴포넌트가 없습니다.", this);
            return;
        }
    }



    public void OnGetFromPool()
    {
        if (_animator == null || _baseSlime == null)
            return;

        _disposables = new CompositeDisposable();

        // Move 애니메이션 재생
        _animator.Play(MoveStateHash);

        // 체력 변화 감지 (피격/사망 애니메이션)
        _baseSlime.Stats.CurrentHealth
            .Pairwise()
            .Where(pair => pair.Previous > pair.Current)
            .Subscribe(pair =>
            {
                if (pair.Current > 0)
                {
                    OnDamaged();
                }
                else
                {
                    OnDied();
                }
            })
            .AddTo(_disposables);
    }

    public void OnReturnToPool()
    {
        _disposables?.Dispose();
        _disposables = null;
    }


    private void OnDamaged()
    {
        if (_animator != null)
        {
            _animator.Play(HitStateHash);
        }
    }

    private void OnDied()
    {
        if (_animator != null)
        {
            _animator.Play(DieStateHash);
        }
    }
}

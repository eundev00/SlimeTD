using UniRx;
using UnityEngine;

public class SlimeAnimation : MonoBehaviour, IPoolItem
{
    [NotNull][SerializeField] private Animator _animator;
    [NotNull][SerializeField] private Animator _faceAnimator;

    private ISlime _slime;
    private CompositeDisposable _disposables;

    private static readonly int IdleStateHash = Animator.StringToHash("Idle");
    private static readonly int HitStateHash = Animator.StringToHash("Hit");
    private static readonly int DieStateHash = Animator.StringToHash("Die");


    private void Awake()
    {
        _slime = GetComponent<ISlime>();
        if (_slime == null)
        {
            Debug.Log("[SlimeAnimation] ISlime 컴포넌트가 없습니다.", this);
            return;
        }

        if (_animator == null)
        {
            Debug.Log("[SlimeAnimation] _animator가 연결되지 않았습니다.", this);
        }

        if (_faceAnimator == null)
        {
            Debug.Log("[SlimeAnimation] _faceAnimator가 연결되지 않았습니다.", this);
        }
    }

    // 풀을 쓰지 않는 슬라임(DummySlime)은 OnGetFromPool이 호출되지 않는다.
    private void Start()
    {
        if (_disposables == null)
            Subscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }



    public void OnGetFromPool()
    {
        Subscribe();
    }

    public void OnReturnToPool()
    {
        Unsubscribe();
    }


    private void Subscribe()
    {
        if (_slime?.Stats?.CurrentHealth == null)
            return;

        Unsubscribe();

        _disposables = new CompositeDisposable();

        Play(IdleStateHash);

        _slime.Stats.CurrentHealth
            .Pairwise()
            .Where(pair => pair.Previous > pair.Current)
            .Subscribe(pair =>
            {
                if (pair.Current > 0)
                {
                    Play(HitStateHash);
                }
                else
                {
                    Play(DieStateHash);
                }
            })
            .AddTo(_disposables);
    }

    private void Unsubscribe()
    {
        _disposables?.Dispose();
        _disposables = null;
    }

    private void Play(int stateHash)
    {
        if (_animator != null && _animator.runtimeAnimatorController != null)
            _animator.Play(stateHash);

        if (_faceAnimator != null && _faceAnimator.runtimeAnimatorController != null)
            _faceAnimator.Play(stateHash);
    }
}

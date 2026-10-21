using UnityEngine;
using VContainer;
using VContainer.Unity;

public class LobbyLifetimeScope : LifetimeScope
{
    [SerializeField] private RectTransform _popupRoot;
    [SerializeField] private WaveTableData _waveTable;

    protected override void Configure(IContainerBuilder builder)
    {
        // 씬에 이미 놓인 칩은 컨테이너 빌드 직후 직접 주입한다 (팝업 칩은 PopupService가 주입)
        builder.RegisterBuildCallback(resolver =>
        {
            foreach (var chip in FindObjectsByType<CurrencyChip>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                resolver.Inject(chip);
        });

        builder.RegisterInstance(_waveTable);
        // 이 등록이 없으면 [Inject]가 호출되지 않는다 (씬 전체 자동 주입은 없음)
        builder.RegisterComponentInHierarchy<WaveScrollView>();
        builder.RegisterEntryPoint<WaveScrollPresenter>();

        builder.RegisterInstance(_popupRoot);
        builder.RegisterEntryPoint<PopupService>();

        builder.RegisterComponentInHierarchy<HunterCardView>();
        builder.RegisterEntryPoint<HunterCardPresenter>();
    }
}
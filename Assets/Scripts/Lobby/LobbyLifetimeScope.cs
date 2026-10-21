using UnityEngine;
using VContainer;
using VContainer.Unity;

public class LobbyLifetimeScope : LifetimeScope
{
    [SerializeField] private RectTransform _popupRoot;
    [SerializeField] private WaveTableData _waveTable;

    protected override void Configure(IContainerBuilder builder)
    {
        // 이 등록이 없으면 [Inject]가 호출되지 않는다 (씬 전체 자동 주입은 없음)
        builder.RegisterComponentInHierarchy<LobbyHeader>();
        builder.RegisterEntryPoint<LobbyHeaderPresenter>();

        builder.RegisterInstance(_waveTable);
        builder.RegisterComponentInHierarchy<WaveScrollView>();
        builder.RegisterEntryPoint<WaveScrollPresenter>();

        builder.RegisterInstance(_popupRoot);
        builder.RegisterEntryPoint<PopupService>();

        builder.RegisterComponentInHierarchy<HunterCardView>();
        builder.RegisterEntryPoint<HunterCardPresenter>();
    }
}
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class MeinLifeTimeScope : LifetimeScope
{
    [SerializeField] private Player _player;
    [SerializeField] private SlotGrid _slotGrid;
    [SerializeField] private Slot _slot;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private GameOverUIManager _gameOverUIManager;
    [SerializeField] private Enemy _enemy;
    [SerializeField] private SceneLoadManager _sceneLoadManager;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterInstance(_player);
        builder.RegisterInstance(_slotGrid);
        builder.RegisterInstance(_slot);
        builder.RegisterInstance(_canvas);
        builder.RegisterInstance(_gameOverUIManager);
        builder.RegisterInstance(_enemy);
        builder.RegisterInstance(_sceneLoadManager);
    }
}

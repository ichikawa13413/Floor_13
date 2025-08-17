using UnityEngine;
using VContainer;
using VContainer.Unity;

public class MeinLifeTimeScope : LifetimeScope
{
    [SerializeField] private Player _player;
    [SerializeField] private SlotGrid _slotGrid;
    [SerializeField] private Slot _slot;
    [SerializeField] private Canvas _canvas;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterInstance(_player);
        builder.RegisterInstance(_slotGrid);
        builder.RegisterInstance(_slot);
        builder.RegisterInstance(_canvas);
    }
}

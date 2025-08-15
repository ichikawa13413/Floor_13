using UnityEngine;
using VContainer;
using VContainer.Unity;

public class MeinLifeTimeScope : LifetimeScope
{
    [SerializeField] private Player _player;
    [SerializeField] private SlotGrid _slotGrid;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterInstance(_player);
        builder.RegisterInstance(_slotGrid);
    }
}

using System;

namespace Jewelry.Collections;

internal sealed class ObservableCollectionMonitor
{
    public bool IsBusy => _busyCount > 0;

    public IDisposable Enter()
    {
        ++_busyCount;
        return new Scope(this);
    }

    private int _busyCount;

    private sealed class Scope(ObservableCollectionMonitor owner) : IDisposable
    {
        public void Dispose()
        {
            if (_owner is null)
                return;

            --_owner._busyCount;
            _owner = null;
        }

        private ObservableCollectionMonitor? _owner = owner;
    }
}

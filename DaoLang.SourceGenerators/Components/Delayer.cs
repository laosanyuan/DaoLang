using System.Threading;

namespace DaoLang.SourceGenerators.Components
{
    public delegate void DelayerHandler();

    /// <summary>
    ///延时
    /// </summary>
    internal class Delayer
    {
        private readonly Timer _timer = null;

        public int DelayTime { get; }

        public event DelayerHandler DelayerExpired;

        public Delayer(int delayTime)
        {
            this.DelayTime = delayTime;
            this._timer = new Timer(Expired, null, Timeout.Infinite, Timeout.Infinite);
        }

        public void SetDelayer() => this._timer.Change(this.DelayTime, Timeout.Infinite);

        private void Expired(object state)
        {
            this._timer.Change(Timeout.Infinite, Timeout.Infinite);
            this.DelayerExpired?.Invoke();
        }
    }
}

using System.Numerics;
using Entities;
using Sim;
using Utils;

namespace PointMasses.Systems
{
    public class NbodySystem : ISystem
    {
        public ManualResetEventSlim PauseEvent { get; init; }
        public float _gravConstant = 0.01f;
        public float _minDist = 0f;
        public float _threshold = 0.01f;
        public bool _running;
        public bool _postNewtonianEnabled;
        private const int UpdateIntervalMs = 50;
        private readonly BarnesHutTree _barnesHutTree;
        private readonly Thread _updateThread;
        private readonly Context _ctx;

        public NbodySystem(Context ctx)
        {
            _ctx = ctx;
            _barnesHutTree = new(
                UnitConv.PixelsToMeters(new Vector2(Constants.WinW * 0.5f, Constants.WinH * 0.5f)),
                UnitConv.PixelsToMeters(new Vector2(Constants.WinW, Constants.WinH))
            );
            PauseEvent = new ManualResetEventSlim(false);
            _updateThread = new Thread(new ThreadStart(ThreadUpdate), 0)
            {
                IsBackground = true
            };
            _updateThread.Start();
        }

        private void ThreadUpdate()
        {
            for (;;)
            {
                PauseEvent.Wait(Timeout.Infinite);
                Thread.Sleep(UpdateIntervalMs);
                _barnesHutTree.Update(_ctx);
            }
        }

        public void Update() 
        {
            if (_running)
            {
                PauseEvent.Set();
            }
            else
            {
                PauseEvent.Reset();
            }
        }

        public void UpdateInput() {}

        public void Draw() {}

    }
}

using System.Numerics;
using Entities;
using Sim;
using SimSystems;
using Utils;

namespace Systems
{
    public class NbodySystem : ISystem
    {
        public float _gravConstant = 0.01f;
        public float _minDist = 0f;
        public float _threshold = 0.01f;
        public bool _running;
        private const int UpdateIntervalMs = 50;
        private readonly BarnesHutTree _barnesHutTree;
        private readonly Thread _updateThread;
        private readonly Context _ctx;

        public NbodySystem(Context ctx)
        {
            _ctx = ctx;
            _barnesHutTree = new(
                UnitConv.PixelsToMeters(new Vector2(Program.WinW / 2f, Program.WinH / 2f)),
                UnitConv.PixelsToMeters(new Vector2(Program.WinW, Program.WinH))
            );
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
                Thread.Sleep(UpdateIntervalMs);
                if (!_running || _ctx._simPaused)
                {
                    continue;
                }
                _barnesHutTree.Update(_ctx);
            }
        }

        public void Update() {}

        public void Draw() {}

    }
}

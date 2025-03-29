using System.Numerics;
using Entities;
using PointMasses.Input;
using PointMasses.Sim;
using PointMasses.Utils;

namespace PointMasses.Systems
{
    public class NbodySystem : ISystem
    {
        public ManualResetEventSlim ResumeEvent { get; init; }
        public ManualResetEventSlim ThreadResetEvent { get; init; }
        public float _gravConstant = 0.01f;
        public float _minDist = 0f;
        public float _threshold = 0.01f;
        public bool _running;
        public bool _postNewtonianEnabled;
        public int _updateIntervalMs = Constants.NbodySystemUpdateMs;
        private Thread _updateThread;
        private readonly BarnesHutTree _barnesHutTree;
        private readonly Context _ctx;

        public NbodySystem(Context ctx)
        {
            _ctx = ctx;
            InputManager.PauseChanged += OnPauseChanged;
            _barnesHutTree = new(
                UnitConv.PtoM(new Vector2(ctx.WinSize.X * 0.5f, ctx.WinSize.Y * 0.5f)),
                UnitConv.PtoM(new Vector2(ctx.WinSize.X, ctx.WinSize.Y))
            );
            ResumeEvent = new ManualResetEventSlim(false);
            ThreadResetEvent = new ManualResetEventSlim(true);
        }

        public bool ShutdownUpdateThread()
        {
            if (_updateThread is null || !_updateThread.IsAlive)
            {
                return false;
            }
            ResumeEvent.Set();
            ThreadResetEvent.Reset(); // Kill update thread
            ThreadResetEvent.Wait();// Wait for termination finished signal from thread
            return true;
        }

        public bool StartUpdateThread()
        {
            if (_updateThread is not null && _updateThread.IsAlive)
            {
                return false;
            }
            _updateThread = new Thread(new ThreadStart(ThreadUpdate), 0)
            {
                IsBackground = true,
                Name = "N-body system thread"
            };
            _updateThread.Start();
            AsyncLogger.Info("Started n-body system thread");
            return true;
        }

        private void ThreadUpdate()
        {
            while (ThreadResetEvent.IsSet)
            {
                ResumeEvent.Wait(Timeout.Infinite);
                Thread.Sleep(_updateIntervalMs);
                                _barnesHutTree.Update(_ctx);
            }
            ThreadResetEvent.Set();
            AsyncLogger.Info("N-body system thread terminated");
        }

        private void OnPauseChanged(object _, bool paused)
        {
            if (_running && paused)
            {
                _running = false;
            }
        }

        public void Update() 
        {
            if (_running)
            {
                ResumeEvent.Set();
            }
            else
            {
                ResumeEvent.Reset();
            }
        }

        public void UpdateInput() {}
        public void Draw() {}

    }
}

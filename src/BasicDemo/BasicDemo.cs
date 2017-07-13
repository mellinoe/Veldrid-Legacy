using System;
using System.Diagnostics;
using Veldrid.Graphics;
using Veldrid.Platform;
using Veldrid.RenderDemo;
using Veldrid.Sdl2;

namespace BasicDemo
{
    public class BasicDemoApp
    {
        private readonly Sdl2Window _window;
        private readonly RenderContext _rc;
        private double _desiredFrameTimeSeconds = 1.0 / 60.0;
        private bool _running = true;
        private bool _windowResized = false;

        public BasicDemoApp(Sdl2Window window, RenderContext rc)
        {
            _window = window;
            _rc = rc;

            _window.Closed += () => _running = false;
            _window.Resized += () => _windowResized = true;
        }

        public void Run()
        {
            Stopwatch sw = Stopwatch.StartNew();
            long previousFrameTicks = 0;
            long currentFrameTicks;
            long swFrequency = Stopwatch.Frequency;
            long desiredFramerateTicks = (long)(_desiredFrameTimeSeconds * swFrequency);
            while (_running)
            {
                while ((currentFrameTicks = sw.ElapsedTicks) < desiredFramerateTicks)
                { }
                double elapsed = (currentFrameTicks - previousFrameTicks) * swFrequency;

                Update(elapsed);
                Draw();

                previousFrameTicks = currentFrameTicks;
            }
        }

        public void Update(double deltaSeconds)
        {
            // Poll input
            InputSnapshot snapshot = _window.PumpEvents();
            InputTracker.UpdateFrameInput(snapshot);
        }

        public void Draw()
        {
            if (_windowResized)
            {
                _windowResized = false;
                _rc.ResizeMainWindow(_window.Width, _window.Height);
            }

            _rc.Viewport = new Viewport(0, 0, _window.Width, _window.Height);
            _rc.ClearBuffer(new RgbaFloat((Environment.TickCount / 1000.0f) % 1.0f, (Environment.TickCount / 3000.0f) % 1.0f, (Environment.TickCount / 100.0f) % 1.0f, 1f));
            _rc.SwapBuffers();
        }
    }
}

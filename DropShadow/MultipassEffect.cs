using PaintDotNet;
using PaintDotNet.Effects;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace AssortedPlugins.DropShadow
{
    /// <summary>
    ///   Renders a rectangle for a single phase.
    /// </summary>
    /// <param name="dst">The destination for rendering the current phase.</param>
    /// <param name="src">The source from the previous phase.</param>
    /// <param name="rect">The rectangle to render within.</param>
    public delegate void RenderPhase(RenderArgs dst, RenderArgs src, Rectangle rect);

    /// <summary>
    ///   An effect which uses a multipass multithreaded rendering schedule.
    ///   This allows combining effects where each pixel has contributions from other pixels, such as blur.
    /// </summary>
    public abstract class MultipassEffect : PropertyBasedEffect
    {
        private const int NUM_THREADS = 4;

        // No rendering schedule, manual threading
        // This is to ensure that the whole image is processed at each phase
        public MultipassEffect(string name, [Optional] Image image, [Optional] string subMenuName, EffectOptions options) : base(
            name,
            image,
            subMenuName,
            new EffectOptions() { Flags = options.Flags, RenderingSchedule = EffectRenderingSchedule.None })
        {
        }

        protected abstract RenderPhase[] Phases { get; }

        protected override void OnRender(Rectangle[] renderRects, int startIndex, int length)
        {
            OnRender();
        }

        protected void OnRender()
        {
            // If the selection is one or two rectangular regions, the renderer passes those full rectangles.
            // Otherwise, it passes thousands of scanline rectangles.
            // For consistency and to be able to parallelize normal rectangular selections,
            // ignore the given rects and slice the selection bounding box manually.
            // (The normal method of slicing rectangles is internal)
            IList<Rectangle> renderRects = Tileize(EnvironmentParameters.SelectionBounds, 128);

            // Cache phases
            RenderPhase[] phases = Phases;
            RenderArgs[] phaseResults = new RenderArgs[phases.Length + 1];

            // First and last phases use existing src/dst surfaces
            phaseResults[0] = SrcArgs;
            phaseResults[phases.Length] = DstArgs;
            
            // Remaining phases use temporary intermediate surfaces
            for (int i = 1; i < phases.Length; i++)
            {
                phaseResults[i] = new RenderArgs(new Surface(SrcArgs.Size));
            }

            // Divide the work equally between threads
            // The last thread will get any remainder
            int chunkSize = renderRects.Count / NUM_THREADS;

            // Synchronize between phases using a barrier
            using (Barrier barrier = new Barrier(NUM_THREADS))
            {
                Parallel.For(0, NUM_THREADS, threadIndex =>
                {
                    int threadStartIndex = threadIndex * chunkSize;

                    // Last thread will get any remainder
                    int threadEndIndex;
                    if (threadIndex == NUM_THREADS - 1)
                    {
                        threadEndIndex = renderRects.Count;
                    }
                    else
                    {
                        threadEndIndex = threadStartIndex + chunkSize;
                    }

                    // Loop blocks until all threads have completed their phase
                    for (int phaseIndex = 0; phaseIndex < phases.Length; phaseIndex++)
                    {
                        for (int rectIndex = threadStartIndex; rectIndex < threadEndIndex; rectIndex++)
                        {
                            phases[phaseIndex](phaseResults[phaseIndex + 1], phaseResults[phaseIndex], renderRects[rectIndex]);
                        }
                        barrier.SignalAndWait();
                    }
                });
            }
        }

        /// <summary>
        ///   Divides a bounding rectangle into equal-sized chunks.
        /// </summary>
        /// <returns>An array of chunks covering the entire area of bounds.</returns>
        /// <seealso cref="PaintDotNet.Rendering.TileizeRenderer{TPixel}"/>
        private static IList<Rectangle> Tileize(Rectangle bounds, int tileSize)
        {
            List<Rectangle> tiles = new List<Rectangle>();

            for (int y = bounds.Top; y < bounds.Bottom; y += tileSize)
            {
                for (int x = bounds.Left; x < bounds.Right; x += tileSize)
                {
                    int right = Math.Min(x + tileSize, bounds.Right);
                    int bottom = Math.Min(y + tileSize, bounds.Bottom);

                    tiles.Add(new Rectangle(x, y, right - x, bottom - y));
                }
            }
            return tiles;
        }
    }
}

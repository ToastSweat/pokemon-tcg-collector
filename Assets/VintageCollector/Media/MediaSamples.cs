using System;
namespace VintageCollector.Media
{
    public static class MediaSamples
    {
        public const int SampleRate = 22050;
        // Local xorshift state is intentionally unrelated to any game PRNG/state.
        public static float[] Synthesize(string cue)
        {
            int kind = Array.IndexOf(new[] { "generate", "open", "next", "skip" }, cue);
            if (kind < 0) throw new ArgumentException("Unknown committed cue");
            int count = new[] { 6174, 7938, 2646, 3969 }[kind];
            var samples = new float[count];
            uint state = (uint)(0x19590321 + kind * 7919);
            double smooth = 0;
            for (int i = 0; i < count; i++)
            {
                state ^= state << 13; state ^= state >> 17; state ^= state << 5;
                double noise = (state / (double)uint.MaxValue) * 2 - 1;
                smooth = 0.68 * smooth + 0.32 * noise;
                double u = i / (double)(count - 1), time = i / (double)SampleRate;
                double envelope = Math.Sin(Math.PI * u) * Math.Sin(Math.PI * u) * Math.Exp(-2 * u);
                double rustle = kind == 1 ? noise * 0.75 + smooth * 0.25 : smooth;
                double tone = Math.Sin(2 * Math.PI * (170 + kind * 115) * time + 9 * u * u);
                samples[i] = (float)(0.21 * envelope * (0.8 * rustle + 0.2 * tone));
            }
            return samples;
        }
    }
}

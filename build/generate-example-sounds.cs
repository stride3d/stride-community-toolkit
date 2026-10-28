// Generates the .wav files the audio examples load, into examples/code-only/Resources.
//
//   dotnet run --file build/generate-example-sounds.cs
//
// The files are synthesised here rather than taken from a sample library so the repository carries
// no third-party audio and no attribution obligation: a bell-like chime for one-shots, and a slow
// two-note pad that loops seamlessly. Both are mono, which is what spatialised playback requires,
// and 16-bit PCM, the format Stride's audio layer takes as-is. Re-run after changing the recipe;
// the outputs are committed so nobody needs to.

using System.Buffers.Binary;
using System.Text;

// dotnet run --file builds into a temp folder, so the repository is found from the working directory.
var resources = FindUp(Directory.GetCurrentDirectory(), Path.Combine("examples", "code-only", "Resources"))
    ?? throw new DirectoryNotFoundException("Run from inside the repository: examples/code-only/Resources was not found above the working directory.");

Write(Path.Combine(resources, "chime.wav"), Chime(44100), 44100);
Write(Path.Combine(resources, "pad-loop.wav"), PadLoop(22050), 22050);

return;

// A struck bell: three partials at 1, 2.4 and 3.9 times the fundamental, each decaying at its own rate.
static short[] Chime(int rate)
{
    const float Seconds = 0.9f;
    const float Fundamental = 880f;

    var samples = new short[(int)(rate * Seconds)];

    for (var i = 0; i < samples.Length; i++)
    {
        var t = (float)i / rate;
        var value = 0.55f * MathF.Sin(2 * MathF.PI * Fundamental * t) * MathF.Exp(-3.5f * t)
                  + 0.30f * MathF.Sin(2 * MathF.PI * Fundamental * 2.4f * t) * MathF.Exp(-6f * t)
                  + 0.15f * MathF.Sin(2 * MathF.PI * Fundamental * 3.9f * t) * MathF.Exp(-9f * t);

        // A 3 ms attack so the first sample does not click.
        var attack = MathF.Min(1f, t / 0.003f);

        samples[i] = (short)(value * attack * 0.8f * short.MaxValue);
    }

    return samples;
}

// Eight seconds of a steady organ-like chord. Steady on purpose: the spatial example uses it, and
// any loudness change baked into the file would be heard as distance. That rules out a swell and
// also detuned pairs, whose beating is a slow tremolo. Every frequency completes a whole number of
// cycles in the loop length, so the end meets the start with no seam.
static short[] PadLoop(int rate)
{
    const int Seconds = 8;

    var length = rate * Seconds;
    var samples = new short[length];

    // Whole cycles over the loop: f = n / Seconds, which every multiple of 0.125 Hz satisfies.
    float[] notes = [220f, 330f, 440f, 660f, 880f];
    float[] gains = [0.34f, 0.22f, 0.16f, 0.10f, 0.05f];

    for (var i = 0; i < length; i++)
    {
        var t = (float)i / rate;
        var value = 0f;

        for (var n = 0; n < notes.Length; n++)
            value += gains[n] * MathF.Sin(2 * MathF.PI * notes[n] * t);

        samples[i] = (short)(value * short.MaxValue);
    }

    return samples;
}

static void Write(string path, short[] samples, int rate)
{
    var data = new byte[samples.Length * 2];

    for (var i = 0; i < samples.Length; i++)
        BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(i * 2), samples[i]);

    using var stream = File.Create(path);
    using var writer = new BinaryWriter(stream, Encoding.ASCII);

    writer.Write("RIFF"u8);
    writer.Write(36 + data.Length);
    writer.Write("WAVE"u8);
    writer.Write("fmt "u8);
    writer.Write(16);
    writer.Write((ushort)1);                 // PCM
    writer.Write((ushort)1);                 // mono
    writer.Write(rate);
    writer.Write(rate * 2);                  // byte rate
    writer.Write((ushort)2);                 // block align
    writer.Write((ushort)16);                // bits per sample
    writer.Write("data"u8);
    writer.Write(data.Length);
    writer.Write(data);

    Console.WriteLine($"{path}: {samples.Length / (float)rate:0.00} s, {rate} Hz mono, {data.Length / 1024} KB");
}

static string? FindUp(string start, string relative)
{
    for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
    {
        var candidate = Path.Combine(dir.FullName, relative);

        if (Directory.Exists(candidate)) return candidate;
    }

    return null;
}
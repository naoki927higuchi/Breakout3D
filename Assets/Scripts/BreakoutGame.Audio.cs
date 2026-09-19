using UnityEngine;

public sealed partial class BreakoutGame
{
    enum Sound { Launch, Paddle, Wall, Armor, Break, Ready, Multiball, Lost, GameOver, Clear }
    AudioClip[] soundClips;
    AudioSource[] soundVoices;
    int nextVoice;
    bool soundMuted;
    readonly int[] soundEvents = new int[10];

    void InitializeAudio()
    {
        cam.gameObject.AddComponent<AudioListener>();
        soundVoices = new AudioSource[8];
        for (int i = 0; i < soundVoices.Length; i++)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0;
            source.volume = .6f;
            soundVoices[i] = source;
        }
        // Original synthesized arcade sounds. Generate once, never during collisions.
        soundClips = new[] {
            Synth("Launch", .19f, new[] { 330f, 660f }, .32f),
            Synth("Paddle", .10f, new[] { 420f }, .42f, 1.5f),
            Synth("Wall", .055f, new[] { 720f }, .16f),
            Synth("Armor", .12f, new[] { 170f }, .34f, .65f, .25f),
            Synth("Break", .15f, new[] { 880f, 1175f }, .34f, .8f, .12f),
            Synth("Ready", .25f, new[] { 660f, 880f }, .26f),
            Synth("Multiball", .36f, new[] { 523f, 659f, 784f, 1047f }, .35f),
            Synth("Lost", .23f, new[] { 330f, 220f }, .32f, .7f),
            Synth("Game Over", .65f, new[] { 392f, 330f, 262f, 131f }, .38f),
            Synth("All Clear", .75f, new[] { 523f, 659f, 784f, 1047f }, .38f)
        };
    }

    AudioClip Synth(string name, float duration, float[] notes, float gain, float sweep = 1, float noise = 0)
    {
        const int rate = 44100;
        var data = new float[Mathf.CeilToInt(rate * duration)];
        var random = new System.Random(73);
        float phase = 0;
        int previousNote = -1;
        for (int i = 0; i < data.Length; i++)
        {
            float progress = (float)i / data.Length;
            int note = Mathf.Min(notes.Length - 1, (int)(progress * notes.Length));
            if (note != previousNote) { phase = 0; previousNote = note; }
            float local = progress * notes.Length - note;
            float noteDuration = duration / notes.Length;
            float envelope = Mathf.Clamp01(local * noteDuration / .004f)
                * Mathf.Clamp01((1 - local) * noteDuration / .018f) * Mathf.Exp(-2.6f * local);
            phase += 2 * Mathf.PI * notes[note] * Mathf.Lerp(1, sweep, local) / rate;
            float tone = .8f * Mathf.Sin(phase) + .2f * Mathf.Sin(phase * 2);
            data[i] = gain * envelope * ((1 - noise) * tone + noise * ((float)random.NextDouble() * 2 - 1));
        }
        data[data.Length - 1] = 0;
        var clip = AudioClip.Create(name, data.Length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    void PlaySound(Sound sound, float pitch = 1)
    {
        soundEvents[(int)sound]++;
        if (soundVoices == null || soundMuted) return;
        var source = soundVoices[nextVoice];
        nextVoice = (nextVoice + 1) % soundVoices.Length;
        source.Stop();
        source.clip = soundClips[(int)sound];
        source.pitch = pitch;
        source.Play();
    }

    void StopSounds()
    {
        if (soundVoices == null) return;
        foreach (var source in soundVoices) source.Stop();
    }

    void ToggleSound()
    {
        soundMuted = !soundMuted;
        if (soundMuted) StopSounds();
    }

    void OnDestroy()
    {
        if (soundClips != null)
            foreach (var clip in soundClips) Destroy(clip);
    }
}

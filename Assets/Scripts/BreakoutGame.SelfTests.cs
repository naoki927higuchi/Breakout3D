using UnityEngine;

public sealed partial class BreakoutGame
{
    int passedChecks;
    void Require(bool condition, string name)
    {
        if (!condition) throw new System.Exception("FAIL: " + name);
        passedChecks++;
        Debug.Log("PASS: " + name);
    }
    void Hit(Brick brick)
    {
        balls[0].pos = brick.pos + new Vector2(0,-.425f-Radius-.02f);
        balls[0].dir = Vector2.up;
        StepSimulation(.006f);
    }
    void PaddleHit()
    {
        balls[0].pos = new Vector2(paddleX,-7.14f); balls[0].dir = Vector2.down;
        StepSimulation(.006f);
    }
    void RunSelfTests()
    {
        try
        {
            Require(cam.GetComponent<AudioListener>() != null && soundVoices.Length == 8, "Audio listener and bounded voice pool initialized");
            foreach (var clip in soundClips)
            {
                var samples = new float[clip.samples];
                Require(clip.GetData(samples, 0), "Audio data readable: " + clip.name);
                float peak = 0;
                foreach (float sample in samples) peak = Mathf.Max(peak, Mathf.Abs(sample));
                Require(peak > .01f && peak < .5f && samples[0] == 0 && samples[samples.Length-1] == 0,
                    "Audible bounded waveform with click-free ends: " + clip.name);
            }
            Require(bricks.Count == 54 && balls.Count == 1, "Initial 54 bricks and one ball");
            var armored = bricks.Find(b => b.hp == 2);
            Hit(armored);
            Require(soundEvents[(int)Sound.Armor] == 1 && soundEvents[(int)Sound.Break] == 0, "Armor hit plays armor sound only");
            Require(armored.hp == 1 && bricks.Count == 54 && speed == BaseSpeed, "Red brick survives first hit without acceleration");
            Hit(armored);
            Require(soundEvents[(int)Sound.Break] == 1, "Brick destruction plays break sound");
            Require(!bricks.Contains(armored) && destroyed == 1 && speed > BaseSpeed, "Red brick breaks on second hit and accelerates ball");
            Require(Mathf.Approximately(speed, 8.32f), "Each destroyed brick adds 0.32 speed");
            for (int i=0;i<3;i++) Hit(bricks.Find(b => b.hp == 1));
            Require(balls.Count == 1 && destroyed == 4 && bestCombo == 4, "No early multiball and combo still recorded");
            balls[0].pos = new Vector2(paddleX,-7.14f); balls[0].dir = Vector2.down;
            StepSimulation(.006f);
            Require(balls[0].combo == 0 && bestCombo == 4 && destroyed == 4, "Paddle resets current combo but preserves total and best");
            Hit(bricks.Find(b => b.hp == 1));
            Require(balls.Count == 1 && multiballPending && balls[0].combo == 1 && bestCombo == 4, "Fifth total break reserves ball across combo reset");
            Require(Mathf.Approximately(speed, 9.6f), "No multiball acceleration before paddle return");
            PaddleHit();
            Require(soundEvents[(int)Sound.Paddle] == 2 && soundEvents[(int)Sound.Ready] == 1 && soundEvents[(int)Sound.Multiball] == 1, "Paddle, ready and multiball sounds follow actual gameplay events");
            Require(balls.Count == 2 && !multiballPending && Mathf.Approximately(speed,10.2f), "Paddle return consumes reservation and adds 0.6 speed");
            Require(balls[0].dir.y > 0 && balls[1].dir.y > 0 && Vector2.Angle(balls[0].dir,balls[1].dir) > 10, "Both balls launch upward on distinct trajectories");
            PaddleHit();
            Require(balls.Count == 2 && Mathf.Approximately(speed,10.2f), "Subsequent paddle bounce adds no ball or speed");
            for (int i=0;i<5;i++) Hit(bricks.Find(b => b.hp == 1));
            Require(balls.Count == 2 && !multiballPending && Mathf.Approximately(speed,11.8f), "Two balls at tenth break skip spawn and extra acceleration");
            balls[0].pos = new Vector2(paddleX,-7.14f); balls[0].dir = Vector2.down;
            StepSimulation(.006f);
            Require(balls[0].combo == 0 && balls[0].dir.y > 0, "Paddle bounce resets streak and reflects ball");
            balls[1].pos = new Vector2(0,-11); StepSimulation(.006f);
            Require(balls.Count == 1 && !ended, "Losing one ball continues game");
            for (int i=0;i<4;i++) Hit(bricks.Find(b => b.hp == 1));
            Require(balls.Count == 1 && destroyed == 14, "Skipped milestone is not banked after ball loss");
            Hit(bricks.Find(b => b.hp == 1));
            Require(balls.Count == 1 && multiballPending && Mathf.Approximately(speed,13.4f), "Fifteenth break reserves second ball again");
            PaddleHit();
            Require(balls.Count == 2 && Mathf.Approximately(speed,14f), "Reserved ball respawns at paddle with modest speed increase");
            // Let the second ball deliver the next milestone to verify shared counting.
            var first = balls[0]; balls[0] = balls[1]; balls[1] = first;
            balls[1].pos = new Vector2(0,-11); StepSimulation(.006f);
            for (int i=0;i<5;i++) Hit(bricks.Find(b => b.hp == 1));
            Require(destroyed == 20 && balls.Count == 1 && multiballPending && Mathf.Approximately(speed,15.6f), "Destruction total is shared between balls");
            PaddleHit();
            Require(balls.Count == 2 && Mathf.Approximately(speed,16.2f), "New ball also activates reservation on paddle return");
            balls[1].pos = new Vector2(0,-11); StepSimulation(.006f);
            balls[0].pos = new Vector2(0,-11); StepSimulation(.006f);
            Require(soundEvents[(int)Sound.Lost] > 0 && soundEvents[(int)Sound.GameOver] == 1, "Ball loss and game over have distinct sounds");
            Require(balls.Count == 0 && ended && !won, "Losing every ball ends game");
            ResetGame();
            Require(!ended && !started && speed == BaseSpeed && bricks.Count == 54 && destroyed == 0 && bestCombo == 0, "Restart restores initial state and counters");
            speed = MaxSpeed;
            Hit(bricks.Find(b => b.hp == 1));
            Require(speed == MaxSpeed, "Speed cap is enforced");
            for (int i=0;i<4;i++) Hit(bricks.Find(b => b.hp == 1));
            PaddleHit();
            Require(balls.Count == 2 && speed == MaxSpeed, "Multiball also respects speed cap");
            ResetGame();
            for (int i=0;i<10;i++) Hit(bricks.Find(b => b.hp == 1));
            Require(balls.Count == 1 && multiballPending && bestCombo == 10, "Multiple milestones before return keep one reservation and preserve combo");
            PaddleHit();
            Require(balls.Count == 2 && !multiballPending && Mathf.Approximately(speed,11.8f), "Stacked milestones only spawn and accelerate once");
            ResetGame();
            for (int i=0;i<5;i++) Hit(bricks.Find(b => b.hp == 1));
            balls[0].pos = new Vector2(0,-11); StepSimulation(.006f);
            Require(ended && balls.Count == 0 && !multiballPending, "Missing paddle while reserved still causes game over");
            ResetGame();
            for (int i=0;i<5;i++) Hit(bricks.Find(b => b.hp == 1));
            ResetGame();
            Require(!multiballPending, "Restart clears unconsumed reservation");
            PaddleHit();
            Require(balls.Count == 1 && speed == BaseSpeed, "No stale multiball after restart");
            ResetGame();
            var last = bricks[0];
            for (int i=bricks.Count-1;i>0;i--) { Destroy(bricks[i].view.gameObject); bricks.RemoveAt(i); }
            Hit(last);
            Require(ended && won && bricks.Count == 0, "Final brick triggers all clear");
            Require(soundEvents[(int)Sound.Clear] == 1, "Final brick plays clear cue once");
            ToggleSound();
            ResetGame();
            Require(soundMuted, "Mute preference survives restart");
            PlaySound(Sound.Launch);
            bool allStopped = true;
            foreach (var voice in soundVoices) allStopped &= !voice.isPlaying;
            Require(allStopped, "Muted sounds do not start playback");
            ToggleSound();
            Require(!soundMuted, "Sound can be enabled again");
            Debug.Log($"SELF TESTS PASSED ({passedChecks} checks)");
            Application.Quit(0);
        }
        catch (System.Exception e) { Debug.LogException(e); Application.Quit(1); }
    }
}

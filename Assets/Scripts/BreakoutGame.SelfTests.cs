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
    void RunSelfTests()
    {
        try
        {
            Require(bricks.Count == 54 && balls.Count == 1, "Initial 54 bricks and one ball");
            var armored = bricks.Find(b => b.hp == 2);
            Hit(armored);
            Require(armored.hp == 1 && bricks.Count == 54 && speed == BaseSpeed, "Red brick survives first hit without acceleration");
            Hit(armored);
            Require(!bricks.Contains(armored) && destroyed == 1 && speed > BaseSpeed, "Red brick breaks on second hit and accelerates ball");
            Require(Mathf.Approximately(speed, 8.55f), "Each destroyed brick adds 0.55 speed");
            for (int i=0;i<3;i++) Hit(bricks.Find(b => b.hp == 1));
            Require(balls.Count == 1 && destroyed == 4 && bestCombo == 4, "No early multiball and combo still recorded");
            balls[0].pos = new Vector2(paddleX,-7.14f); balls[0].dir = Vector2.down;
            StepSimulation(.006f);
            Require(balls[0].combo == 0 && bestCombo == 4 && destroyed == 4, "Paddle resets current combo but preserves total and best");
            Hit(bricks.Find(b => b.hp == 1));
            Require(balls.Count == 2 && balls[0].combo == 1 && bestCombo == 4, "Fifth total break spawns ball across combo reset");
            Require(Mathf.Approximately(speed, 12.25f), "Multiball adds 1.5 on top of normal acceleration");
            for (int i=0;i<5;i++) Hit(bricks.Find(b => b.hp == 1));
            Require(balls.Count == 2 && Mathf.Approximately(speed,15f), "Two balls at tenth break skip spawn and extra acceleration");
            balls[0].pos = new Vector2(paddleX,-7.14f); balls[0].dir = Vector2.down;
            StepSimulation(.006f);
            Require(balls[0].combo == 0 && balls[0].dir.y > 0, "Paddle bounce resets streak and reflects ball");
            balls[1].pos = new Vector2(0,-11); StepSimulation(.006f);
            Require(balls.Count == 1 && !ended, "Losing one ball continues game");
            for (int i=0;i<4;i++) Hit(bricks.Find(b => b.hp == 1));
            Require(balls.Count == 1 && destroyed == 14, "Skipped milestone is not banked after ball loss");
            Hit(bricks.Find(b => b.hp == 1));
            Require(balls.Count == 2 && Mathf.Approximately(speed,19.25f), "Fifteenth break respawns second ball and accelerates again");
            // Let the second ball deliver the next milestone to verify shared counting.
            var first = balls[0]; balls[0] = balls[1]; balls[1] = first;
            balls[1].pos = new Vector2(0,-11); StepSimulation(.006f);
            for (int i=0;i<5;i++) Hit(bricks.Find(b => b.hp == 1));
            Require(destroyed == 20 && balls.Count == 2 && Mathf.Approximately(speed,23.5f), "Destruction total is shared between balls");
            balls[1].pos = new Vector2(0,-11); StepSimulation(.006f);
            balls[0].pos = new Vector2(0,-11); StepSimulation(.006f);
            Require(balls.Count == 0 && ended && !won, "Losing every ball ends game");
            ResetGame();
            Require(!ended && !started && speed == BaseSpeed && bricks.Count == 54 && destroyed == 0 && bestCombo == 0, "Restart restores initial state and counters");
            speed = MaxSpeed;
            Hit(bricks.Find(b => b.hp == 1));
            Require(speed == MaxSpeed, "Speed cap is enforced");
            for (int i=0;i<4;i++) Hit(bricks.Find(b => b.hp == 1));
            Require(balls.Count == 2 && speed == MaxSpeed, "Multiball also respects speed cap");
            ResetGame();
            var last = bricks[0];
            for (int i=bricks.Count-1;i>0;i--) { Destroy(bricks[i].view.gameObject); bricks.RemoveAt(i); }
            Hit(last);
            Require(ended && won && bricks.Count == 0, "Final brick triggers all clear");
            Debug.Log($"SELF TESTS PASSED ({passedChecks} checks)");
            Application.Quit(0);
        }
        catch (System.Exception e) { Debug.LogException(e); Application.Quit(1); }
    }
}

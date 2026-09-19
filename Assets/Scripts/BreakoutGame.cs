using System.Collections.Generic;
using UnityEngine;

public sealed partial class BreakoutGame : MonoBehaviour
{
    const float Radius = .23f, BaseSpeed = 8f, MaxSpeed = 28f;
    const float BreakSpeedIncrease = .55f, MultiballSpeedIncrease = 1.5f;
    sealed class Ball { public Transform view; public Vector2 pos, dir; public int combo; }
    sealed class Brick { public Transform view; public Vector2 pos; public int hp; public Renderer renderer; }
    readonly List<Ball> balls = new List<Ball>();
    readonly List<Brick> bricks = new List<Brick>();
    readonly List<GameObject> pieces = new List<GameObject>();
    Transform paddle;
    Camera cam;
    Material cyan, white, red, damaged, gold;
    float paddleX, speed, flashUntil;
    int destroyed, bestCombo;
    bool started, ended, won;
    string notice = "";
    GUIStyle title, small, label, banner;
    Vector3 lastMouse;

    Material Mat(Color c, float glow = 0)
    {
        var m = new Material(Resources.Load<Material>("GameMaterial"));
        m.color = c;
        m.SetFloat("_Glossiness", .65f);
        if (glow > 0) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * glow); }
        return m;
    }
    GameObject Box(string name, Vector3 p, Vector3 scale, Material mat, PrimitiveType type = PrimitiveType.Cube)
    {
        var g = GameObject.CreatePrimitive(type); g.name = name; g.transform.position = p;
        g.transform.localScale = scale; g.GetComponent<Renderer>().sharedMaterial = mat;
        Destroy(g.GetComponent<Collider>()); return g;
    }
    void Start()
    {
        Application.targetFrameRate = 120;
        cyan = Mat(new Color(.08f, .85f, 1), .45f); white = Mat(Color.white, .7f);
        red = Mat(new Color(1, .17f, .27f), .25f); damaged = Mat(new Color(1, .55f, .45f), .4f);
        gold = Mat(new Color(1, .72f, .2f), .25f);
        cam = new GameObject("Game Camera", typeof(Camera)).GetComponent<Camera>();
        cam.transform.position = new Vector3(0, 24, -12); cam.transform.LookAt(new Vector3(0, 0, .5f));
        cam.orthographic = true; cam.orthographicSize = 12.2f; cam.backgroundColor = new Color(.025f, .035f, .075f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        var light = new GameObject("Key Light", typeof(Light)).GetComponent<Light>();
        light.type = LightType.Directional; light.intensity = 1.4f; light.transform.rotation = Quaternion.Euler(50, -25, 0);
        RenderSettings.ambientLight = new Color(.4f, .45f, .6f);
        Box("Arena", new Vector3(0, -.5f, 0), new Vector3(17, .6f, 20), Mat(new Color(.045f, .075f, .12f)));
        for (int x = -8; x <= 8; x++) Box("Grid", new Vector3(x, -.185f, 0), new Vector3(.015f, .01f, 20), Mat(new Color(.09f,.14f,.2f)));
        for (int z = -10; z <= 10; z++) Box("Grid", new Vector3(0, -.185f, z), new Vector3(17,.01f,.015f), Mat(new Color(.09f,.14f,.2f)));
        Box("Left Rail", new Vector3(-8.4f, 0, 0), new Vector3(.25f,.65f,20), cyan);
        Box("Right Rail", new Vector3(8.4f, 0, 0), new Vector3(.25f,.65f,20), cyan);
        Box("Top Rail", new Vector3(0,0,10), new Vector3(17,.65f,.25f), cyan);
        Box("Drain", new Vector3(0,-.15f,-9.7f), new Vector3(16.5f,.06f,.08f), red);
        paddle = Box("Paddle", new Vector3(0,.25f,-7.7f), new Vector3(2.7f,.5f,.6f), cyan).transform;
        ResetGame();
        if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-selftest") >= 0) RunSelfTests();
    }
    void ResetGame()
    {
        foreach (var b in balls) Destroy(b.view.gameObject); balls.Clear();
        foreach (var b in bricks) Destroy(b.view.gameObject); bricks.Clear();
        foreach (var p in pieces) if (p) Destroy(p); pieces.Clear();
        speed = BaseSpeed; destroyed = bestCombo = 0; started = ended = won = false; notice = "";
        paddleX = 0; paddle.position = new Vector3(0,.25f,-7.7f); lastMouse = Input.mousePosition;
        for (int row = 0; row < 6; row++) for (int col = 0; col < 9; col++)
        {
            bool armored = row >= 4 || (row == 2 && col % 2 == 0);
            var pos = new Vector2((col - 4) * 1.72f, 1.5f + row * 1.25f);
            var g = Box(armored ? "Armored Brick (2 HP)" : "Brick", new Vector3(pos.x,.35f,pos.y), new Vector3(1.55f,.7f, .85f), armored ? red : (row % 2 == 0 ? gold : cyan));
            bricks.Add(new Brick { view = g.transform, pos = pos, hp = armored ? 2 : 1, renderer = g.GetComponent<Renderer>() });
            if (armored) { var mark = Box("Armor Stripe", new Vector3(pos.x,.711f,pos.y), new Vector3(.8f,.025f,.09f), white); mark.transform.SetParent(g.transform, true); }
        }
        AddBall(new Vector2(0,-7), new Vector2(.25f,1));
    }
    void AddBall(Vector2 pos, Vector2 dir)
    {
        var g = Box("Ball", new Vector3(pos.x,.4f,pos.y), Vector3.one * Radius * 2, white, PrimitiveType.Sphere);
        var trail = g.AddComponent<TrailRenderer>(); trail.time = .18f; trail.startWidth = .28f; trail.endWidth = 0;
        trail.material = cyan; trail.minVertexDistance = .06f;
        balls.Add(new Ball { view = g.transform, pos = pos, dir = dir.normalized });
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) { ResetGame(); return; }
        if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
        if (ended) return;
        float axis = (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A) ? 1 : 0);
        if (axis != 0) paddleX += axis * 13 * Time.deltaTime;
        else if (Input.mousePosition != lastMouse)
        {
            var ray = cam.ScreenPointToRay(Input.mousePosition);
            if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float d)) paddleX = ray.GetPoint(d).x;
        }
        lastMouse = Input.mousePosition;
        paddleX = Mathf.Clamp(paddleX,-6.75f,6.75f); paddle.position = new Vector3(paddleX,.25f,-7.7f);
        if (!started)
        {
            balls[0].pos = new Vector2(paddleX,-7); Sync(balls[0]);
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)) started = true;
            return;
        }
        StepSimulation(Time.deltaTime);
    }
    void StepSimulation(float frameDelta)
    {
        int count = balls.Count;
        for (int i = count - 1; i >= 0; i--)
        {
            Ball ball = balls[i];
            float remaining = Mathf.Min(frameDelta,.05f);
            while (remaining > 0 && !ended)
            {
                float dt = Mathf.Min(remaining,.006f); remaining -= dt;
                Vector2 prev = ball.pos; ball.pos += ball.dir * speed * dt;
                if (Mathf.Abs(ball.pos.x) > 8.04f) { ball.pos.x = Mathf.Sign(ball.pos.x)*8.04f; ball.dir.x = -Mathf.Sign(ball.pos.x)*Mathf.Abs(ball.dir.x); }
                if (ball.pos.y > 9.64f) { ball.pos.y = 9.64f; ball.dir.y = -Mathf.Abs(ball.dir.y); }
                if (ball.dir.y < 0 && prev.y >= -7.17f && ball.pos.y <= -7.17f && Mathf.Abs(ball.pos.x - paddleX) < 1.35f + Radius)
                {
                    float offset = Mathf.Clamp((ball.pos.x-paddleX)/1.35f,-1,1);
                    ball.dir = new Vector2(offset*.85f,1).normalized; ball.pos.y = -7.17f; ball.combo = 0;
                }
                for (int j = bricks.Count - 1; j >= 0; j--)
                {
                    Brick brick = bricks[j]; Vector2 diff = ball.pos - brick.pos;
                    if (Mathf.Abs(diff.x) >= .775f+Radius || Mathf.Abs(diff.y) >= .425f+Radius) continue;
                    if (Mathf.Abs(prev.x-brick.pos.x) >= .775f+Radius) { ball.dir.x *= -1; ball.pos.x = brick.pos.x + Mathf.Sign(prev.x-brick.pos.x)*(.775f+Radius+.002f); }
                    else { ball.dir.y *= -1; ball.pos.y = brick.pos.y + Mathf.Sign(prev.y-brick.pos.y)*(.425f+Radius+.002f); }
                    brick.hp--;
                    if (brick.hp == 0)
                    {
                        Burst(brick.pos, brick.renderer.sharedMaterial); Destroy(brick.view.gameObject); bricks.RemoveAt(j);
                        destroyed++; speed = Mathf.Min(MaxSpeed, speed + BreakSpeedIncrease); ball.combo++; bestCombo = Mathf.Max(bestCombo, ball.combo);
                        if (destroyed % 5 == 0 && balls.Count == 1)
                        {
                            AddBall(ball.pos, new Vector2(-ball.dir.x + .35f,ball.dir.y));
                            speed = Mathf.Min(MaxSpeed, speed + MultiballSpeedIncrease);
                            notice = "MULTIBALL / SPEED UP!"; flashUntil = Time.time+2.5f;
                        }
                        if (bricks.Count == 0) { ended = won = true; }
                    }
                    else { brick.renderer.sharedMaterial = damaged; if (brick.view.childCount > 0) Destroy(brick.view.GetChild(0).gameObject); }
                    break;
                }
                if (ball.pos.y < -10.5f) break;
            }
            Sync(ball);
            if (ball.pos.y < -10.5f) { Destroy(ball.view.gameObject); balls.RemoveAt(i); }
        }
        if (balls.Count == 0) { ended = true; won = false; }
    }
    void Sync(Ball b) { b.view.position = new Vector3(b.pos.x,.4f,b.pos.y); }
    void Burst(Vector2 pos, Material mat)
    {
        pieces.RemoveAll(p => !p);
        for (int i=0;i<7;i++)
        {
            var p = Box("Spark",new Vector3(pos.x,.5f,pos.y),Vector3.one*.16f,mat);
            p.AddComponent<BreakoutSpark>().velocity = new Vector3(Random.Range(-3f,3f),Random.Range(2f,5f),Random.Range(-3f,3f));
            pieces.Add(p); Destroy(p,.6f);
        }
    }
    void OnGUI()
    {
        float scale = Mathf.Min(Screen.width/1280f,Screen.height/800f);
        GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)/2,0,0),Quaternion.identity,Vector3.one*scale);
        if (title == null)
        {
            title = new GUIStyle(GUI.skin.label) { fontSize=32,fontStyle=FontStyle.Bold };
            label = new GUIStyle(GUI.skin.label) { fontSize=20,fontStyle=FontStyle.Bold };
            small = new GUIStyle(GUI.skin.label) { fontSize=15 };
            banner = new GUIStyle(title) { alignment=TextAnchor.MiddleCenter,fontSize=42 };
        }
        GUI.color = new Color(.15f,.9f,1); GUI.Label(new Rect(34,22,500,48),"NEON BREAK / 3D",title);
        GUI.color = Color.white; GUI.Label(new Rect(36,65,500,30),"54 BLOCKS  /  ONE CHANCE",small);
        GUI.Label(new Rect(820,28,430,35),$"BROKEN {destroyed:00}/54     BALLS {balls.Count}/2",label);
        GUI.Label(new Rect(820,62,430,30),$"SPEED {speed/BaseSpeed:0.00}x     BEST COMBO {bestCombo}",small);
        GUI.Label(new Rect(34,748,1220,30),"MOVE  Mouse / A D / Arrows     LAUNCH  Click / Space     RESTART  R     QUIT  Esc",small);
        GUI.color = new Color(1,.5f,.5f); GUI.Label(new Rect(34,716,1000,25),"RED = 2 HITS     /     EVERY 5 BLOCKS = MULTIBALL + SPEED UP",small);
        GUI.color = Color.white;
        if (Time.time < flashUntil && !ended) GUI.Label(new Rect(0,140,1280,65),notice,banner);
        if (!started || ended)
        {
            GUI.color = new Color(.025f,.035f,.075f,.94f); GUI.DrawTexture(new Rect(280,290,720,195),Texture2D.whiteTexture); GUI.color = Color.white;
            GUI.Label(new Rect(280,310,720,65),ended ? (won ? "ALL CLEAR!" : "GAME OVER") : "READY TO BREAK?",banner);
            var center = new GUIStyle(label) { alignment=TextAnchor.MiddleCenter };
            GUI.Label(new Rect(280,384,720,45),ended ? $"{destroyed} BLOCKS  /  BEST COMBO {bestCombo}   —   PRESS R" : "Click or press SPACE to launch",center);
        }
    }
}

public sealed class BreakoutSpark : MonoBehaviour
{
    public Vector3 velocity;
    void Update() { velocity += Vector3.down * 12 * Time.deltaTime; transform.position += velocity * Time.deltaTime; transform.Rotate(180*Time.deltaTime,120*Time.deltaTime,0); transform.localScale *= Mathf.Exp(-3*Time.deltaTime); }
}

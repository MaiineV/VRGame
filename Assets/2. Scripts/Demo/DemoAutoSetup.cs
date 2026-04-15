using System.Reflection;
using Data.Enums;
using Data.SO;
using Gameplay.Interactions;
using Gameplay.Liquid;
using Gameplay.Systems;
using Services;
using Services.Audio;
using Services.Database;
using Services.Economy;
using Services.GameState;
using Services.Night;
using Services.Recipe;
using Services.Save;
using Services.UpdateService;
using TMPro;
using UI.Diegetic;
using UnityEngine;

namespace Demo
{
    /// <summary>
    /// One-click demo bootstrap. Put this (and ONLY this) on an empty GameObject in
    /// an empty scene, press Play, and you get a full Pour Decisions mock running:
    /// bar, glass, bottle, clipboard, cash register, and an on-screen IMGUI panel
    /// that replaces physical poke/grab.
    ///
    /// NOT optimized, NOT VR-ready. This is a visual mock for demo day.
    /// Camera is a free-fly (WASD + RMB drag) non-XR camera.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class DemoAutoSetup : MonoBehaviour
    {
        private static bool s_built;

        private NightConfigSO _nightConfig;
        private NightClipboard _clipboard;
        private IEconomyService _economy;
        private IGameStateService _state;
        private ISaveService _save;
        private PokeButton _startBtn, _abortBtn, _continueBtn;
        private Transform _fingerProxy;

        void Awake()
        {
            if (s_built) { Destroy(gameObject); return; }
            s_built = true;
            DontDestroyOnLoad(gameObject);

            BuildServices();
            BuildEnvironment();
            BuildCamera();
            BuildLighting();
            BuildBar();
            BuildGlass();
            BuildBottle();
            BuildCashRegister();
            _clipboard = BuildClipboard();

            _state = ServiceLocator.Get<IGameStateService>();
            _economy = ServiceLocator.Get<IEconomyService>();
            _save = ServiceLocator.Get<ISaveService>();

            Debug.Log("[DemoAutoSetup] Scene ready. Use the on-screen panel to drive the flow.");
        }

        // ------------------------------------------------------------------ services
        private void BuildServices()
        {
            ServiceLocator.Initialize();

            ServiceLocator.Register<IUpdateService, UpdateService>();
            ServiceLocator.Register<IDatabaseService, DatabaseService>(mImmediateInit: true);
            ServiceLocator.Register<ISaveService, SaveService>(mImmediateInit: true);
            ServiceLocator.Register<IRecipeService, RecipeService>(mImmediateInit: true);
            ServiceLocator.Register<IEconomyService, EconomyService>(mImmediateInit: true);
            ServiceLocator.Register<IBreakablePoolService, BreakablePoolService>(mImmediateInit: true);
            ServiceLocator.Register<IAudioService, AudioService>(mImmediateInit: true);
            ServiceLocator.Register<INightService, NightService>(mImmediateInit: true);
            ServiceLocator.Register<IGameStateService, GameStateService>(mImmediateInit: true);

            var pump = new GameObject("[UpdatePump]");
            pump.AddComponent<UpdateServiceObject>();
            DontDestroyOnLoad(pump);
        }

        // ------------------------------------------------------------------ camera
        private void BuildCamera()
        {
            var camGo = new GameObject("[DemoCamera]");
            camGo.transform.position = new Vector3(0f, 1.6f, -1.2f);
            camGo.transform.rotation = Quaternion.Euler(15f, 0f, 0f);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.05f, 0.10f);
            cam.nearClipPlane = 0.02f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<FreeFlyCamera>();
            camGo.tag = "MainCamera";

            // Finger proxy that follows the mouse on a plane — used to poke buttons.
            var finger = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            finger.name = "[FingerProxy]";
            finger.transform.localScale = Vector3.one * 0.03f;
            foreach (var c in finger.GetComponents<Collider>()) Destroy(c);
            var col = finger.AddComponent<SphereCollider>();
            col.radius = 0.5f;
            col.isTrigger = true;
            var rb = finger.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            var mr = finger.GetComponent<MeshRenderer>();
            mr.sharedMaterial = MakeMat(new Color(1f, 0.4f, 0.4f));
            _fingerProxy = finger.transform;

            var fc = finger.AddComponent<FingerMouseFollower>();
            fc.Camera = cam;
        }

        private void BuildLighting()
        {
            var sun = new GameObject("[Sun]");
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var l = sun.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.1f;
            RenderSettings.ambientLight = new Color(0.25f, 0.22f, 0.28f);
        }

        private void BuildEnvironment()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "[Floor]";
            floor.transform.localScale = new Vector3(2f, 1f, 2f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = MakeMat(new Color(0.15f, 0.12f, 0.14f));
        }

        private void BuildBar()
        {
            var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = "[Bar]";
            bar.transform.position = new Vector3(0f, 0.5f, 0.6f);
            bar.transform.localScale = new Vector3(3f, 1f, 0.6f);
            bar.GetComponent<MeshRenderer>().sharedMaterial = MakeMat(new Color(0.35f, 0.20f, 0.12f));
        }

        // ------------------------------------------------------------------ glass
        private void BuildGlass()
        {
            var glass = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            glass.name = "[Glass]";
            glass.transform.position = new Vector3(-0.4f, 1.15f, 0.4f);
            glass.transform.localScale = new Vector3(0.06f, 0.08f, 0.06f);
            glass.GetComponent<MeshRenderer>().sharedMaterial = MakeMat(new Color(0.7f, 0.9f, 1f, 1f));

            var rb = glass.AddComponent<Rigidbody>();
            rb.mass = 0.3f;
            rb.linearDamping = 0.5f;

            glass.AddComponent<GrabBridge>();

            // Liquid inside — child cylinder slightly smaller, amber tint
            var liquid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            liquid.name = "Liquid";
            liquid.transform.SetParent(glass.transform, false);
            liquid.transform.localScale = new Vector3(0.85f, 0.6f, 0.85f);
            liquid.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            Destroy(liquid.GetComponent<Collider>());
            liquid.GetComponent<MeshRenderer>().sharedMaterial = MakeMat(new Color(0.85f, 0.5f, 0.15f));
        }

        // ------------------------------------------------------------------ bottle
        private void BuildBottle()
        {
            var bottle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bottle.name = "[Bottle]";
            bottle.transform.position = new Vector3(0.4f, 1.2f, 0.4f);
            bottle.transform.localScale = new Vector3(0.08f, 0.2f, 0.08f);
            bottle.GetComponent<MeshRenderer>().sharedMaterial = MakeMat(new Color(0.15f, 0.35f, 0.15f));

            var rb = bottle.AddComponent<Rigidbody>();
            rb.mass = 0.6f;

            bottle.AddComponent<GrabBridge>();
        }

        // ------------------------------------------------------------------ cash register
        private void BuildCashRegister()
        {
            var reg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            reg.name = "[CashRegister]";
            reg.transform.position = new Vector3(1.1f, 1.2f, 0.4f);
            reg.transform.localScale = new Vector3(0.3f, 0.2f, 0.25f);
            reg.GetComponent<MeshRenderer>().sharedMaterial = MakeMat(new Color(0.6f, 0.55f, 0.3f));
            reg.AddComponent<Gameplay.CashRegister.CashRegister>();
        }

        // ------------------------------------------------------------------ clipboard + UI
        private NightClipboard BuildClipboard()
        {
            _nightConfig = ScriptableObject.CreateInstance<NightConfigSO>();
            _nightConfig.name = "RuntimeNightConfig";

            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "[NightClipboard]";
            board.transform.position = new Vector3(-1.3f, 1.25f, 0.4f);
            board.transform.rotation = Quaternion.Euler(20f, -35f, 0f);
            board.transform.localScale = new Vector3(0.4f, 0.5f, 0.02f);
            board.GetComponent<MeshRenderer>().sharedMaterial = MakeMat(new Color(0.85f, 0.8f, 0.65f));

            // Rigidbody kinematic so it just sits there
            var rb = board.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            var grab = board.AddComponent<GrabBridge>();
            grab.SetHeld(true); // stays "held" so buttons always interactable

            // Build 3 group containers
            var idle = MakeGroup(board.transform, "IdleGroup");
            var running = MakeGroup(board.transform, "RunningGroup");
            var summary = MakeGroup(board.transform, "SummaryGroup");

            // Idle content
            var idleNightTxt = MakeLabel(idle.transform, "NightNum", new Vector3(0f, 0.35f, -0.55f), 0.5f, "Night 1");
            var idleBestTxt = MakeLabel(idle.transform, "Best", new Vector3(0f, 0.18f, -0.55f), 0.35f, "Best: $0");
            var idleCashTxt = MakeLabel(idle.transform, "Cash", new Vector3(0f, 0f, -0.55f), 0.35f, "$0");
            _startBtn = MakeButton(idle.transform, "StartBtn", new Vector3(0f, -0.25f, -0.55f), new Color(0.3f, 0.8f, 0.3f), "START");

            // Running content
            MakeLabel(running.transform, "RunTitle", new Vector3(0f, 0.35f, -0.55f), 0.5f, "NIGHT RUNNING");
            _abortBtn = MakeButton(running.transform, "AbortBtn", new Vector3(0f, -0.25f, -0.55f), new Color(0.8f, 0.3f, 0.3f), "ABORT");

            // Summary content
            var sumCash = MakeLabel(summary.transform, "SumCash", new Vector3(0f, 0.45f, -0.55f), 0.35f, "$0");
            var sumSales = MakeLabel(summary.transform, "SumSales", new Vector3(0f, 0.30f, -0.55f), 0.25f, "0");
            var sumFailed = MakeLabel(summary.transform, "SumFailed", new Vector3(0f, 0.18f, -0.55f), 0.25f, "0");
            var sumExp = MakeLabel(summary.transform, "SumExp", new Vector3(0f, 0.06f, -0.55f), 0.25f, "-$0");
            var sumEarn = MakeLabel(summary.transform, "SumEarn", new Vector3(0f, -0.08f, -0.55f), 0.3f, "+$0");
            _continueBtn = MakeButton(summary.transform, "ContBtn", new Vector3(0f, -0.25f, -0.55f), new Color(0.4f, 0.6f, 0.9f), "CONTINUE");

            // Wire clipboard
            var clip = board.AddComponent<NightClipboard>();
            SetPrivate(clip, "_config", _nightConfig);
            SetPrivate(clip, "_grab", grab);
            SetPrivate(clip, "_idleGroup", idle);
            SetPrivate(clip, "_runningGroup", running);
            SetPrivate(clip, "_summaryGroup", summary);
            SetPrivate(clip, "_startButton", _startBtn);
            SetPrivate(clip, "_abortButton", _abortBtn);
            SetPrivate(clip, "_continueButton", _continueBtn);
            SetPrivate(clip, "_summaryCash", sumCash);
            SetPrivate(clip, "_summarySales", sumSales);
            SetPrivate(clip, "_summaryFailed", sumFailed);
            SetPrivate(clip, "_summaryExpenses", sumExp);
            SetPrivate(clip, "_summaryNightlyEarnings", sumEarn);
            SetPrivate(clip, "_idleNightNumber", idleNightTxt);
            SetPrivate(clip, "_idleBestEarnings", idleBestTxt);
            SetPrivate(clip, "_idleCash", idleCashTxt);

            return clip;
        }

        private GameObject MakeGroup(Transform parent, string name)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            return g;
        }

        private TMP_Text MakeLabel(Transform parent, string name, Vector3 localPos, float scale, string text)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * (scale * 0.01f);
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 4f;
            tmp.color = Color.black;
            return tmp;
        }

        private PokeButton MakeButton(Transform parent, string name, Vector3 localPos, Color color, string label)
        {
            var btn = GameObject.CreatePrimitive(PrimitiveType.Cube);
            btn.name = name;
            btn.transform.SetParent(parent, false);
            btn.transform.localPosition = localPos;
            btn.transform.localScale = new Vector3(0.5f, 0.18f, 0.4f);
            btn.GetComponent<MeshRenderer>().sharedMaterial = MakeMat(color);
            var col = btn.GetComponent<BoxCollider>();
            col.isTrigger = true;

            MakeLabel(btn.transform, "Label", new Vector3(0f, 0f, -0.6f), 0.4f, label);

            return btn.AddComponent<PokeButton>();
        }

        // ------------------------------------------------------------------ helpers
        private static Material MakeMat(Color c)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var m = new Material(shader);
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            return m;
        }

        private static void SetPrivate(object target, string fieldName, object value)
        {
            var f = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (f == null) { Debug.LogWarning($"Field {fieldName} not found on {target.GetType().Name}"); return; }
            f.SetValue(target, value);
        }

        // ------------------------------------------------------------------ IMGUI overlay
        void OnGUI()
        {
            GUI.Box(new Rect(10, 10, 260, 300), "Pour Decisions — DEMO");
            int y = 35;
            GUI.Label(new Rect(20, y, 240, 20), $"State: {_state?.Current}"); y += 20;
            GUI.Label(new Rect(20, y, 240, 20), $"Cash: ${_economy?.Cash}"); y += 20;
            GUI.Label(new Rect(20, y, 240, 20), $"Sales: {_economy?.Sales}  Failed: {_economy?.FailedOrders}"); y += 20;
            GUI.Label(new Rect(20, y, 240, 20), $"Expenses: -${_economy?.Expenses}  Earn: {_economy?.NightlyEarnings}"); y += 25;

            if (GUI.Button(new Rect(20, y, 110, 25), "Begin Night")) _state?.BeginNight();
            if (GUI.Button(new Rect(140, y, 110, 25), "Abort Night")) _state?.AbortNight();
            y += 30;
            if (GUI.Button(new Rect(20, y, 230, 25), "Acknowledge Summary")) _state?.AcknowledgeSummary();
            y += 35;

            GUI.Label(new Rect(20, y, 240, 20), "Test hooks:"); y += 22;
            if (GUI.Button(new Rect(20, y, 110, 25), "+$25 Sale"))
                _economy?.RegisterSale(RecipeId.None, 1f, 0);
            if (GUI.Button(new Rect(140, y, 110, 25), "-$10 Expense"))
                _economy?.RegisterExpense(10, "demo");
            y += 30;
            if (GUI.Button(new Rect(20, y, 230, 25), "Reset Save")) _save?.ResetToDefaults();

            GUI.Label(new Rect(20, 280, 240, 20), "RMB + WASD = fly camera");
        }
    }

    // -------------------------------------------------------------- camera helper
    public sealed class FreeFlyCamera : MonoBehaviour
    {
        public float Speed = 2f;
        public float LookSpeed = 2f;
        private float _yaw, _pitch;

        void Start()
        {
            var e = transform.eulerAngles;
            _yaw = e.y; _pitch = e.x;
        }

        void Update()
        {
            if (Input.GetMouseButton(1))
            {
                _yaw += Input.GetAxis("Mouse X") * LookSpeed;
                _pitch -= Input.GetAxis("Mouse Y") * LookSpeed;
                _pitch = Mathf.Clamp(_pitch, -80f, 80f);
                transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            }

            var v = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) v += transform.forward;
            if (Input.GetKey(KeyCode.S)) v -= transform.forward;
            if (Input.GetKey(KeyCode.A)) v -= transform.right;
            if (Input.GetKey(KeyCode.D)) v += transform.right;
            if (Input.GetKey(KeyCode.E)) v += Vector3.up;
            if (Input.GetKey(KeyCode.Q)) v -= Vector3.up;
            transform.position += v * Speed * Time.deltaTime;
        }
    }

    // -------------------------------------------------------------- finger proxy
    public sealed class FingerMouseFollower : MonoBehaviour
    {
        public Camera Camera;
        public float Distance = 0.8f;

        void Update()
        {
            if (Camera == null) return;
            float d = Distance + Input.mouseScrollDelta.y * 0.05f;
            Distance = Mathf.Clamp(d, 0.2f, 2f);
            var ray = Camera.ScreenPointToRay(Input.mousePosition);
            transform.position = ray.origin + ray.direction * Distance;
        }
    }
}

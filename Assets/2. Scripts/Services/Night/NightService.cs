using System.Collections.Generic;
using Data.Enums;
using Data.SO;
using Gameplay;
using Gameplay.Customer;
using Gameplay.Systems;
using Services.Economy;
using Services.UpdateService;
using UnityEngine;
using Utilities;

namespace Services.Night
{
    public sealed class NightService : INightService, IUpdateListener
    {
        private readonly IUpdateService _updates;
        private readonly IEconomyService _economy;

        private NightConfigSO _config;
        private float _timeRemaining;
        private float _spawnTimer;
        private readonly List<CustomerEntity> _active = new(8);

        public bool IsRunning { get; private set; }
        public float TimeRemaining => _timeRemaining;
        public int ActiveCustomers => _active.Count;
        public Data.SO.DrunkennessConfigSO DrunkennessConfig => _config != null ? _config.DrunkennessConfig : null;

        public event System.Action NightStarted;
        public event System.Action NightEnded;

        public NightService(IUpdateService updates, IEconomyService economy)
        {
            _updates = updates;
            _economy = economy;
        }

        public void Initialize() { }

        public void StartNight(NightConfigSO config)
        {
            if (IsRunning) return;
            if (config == null) { MyLogger.LogError("[NightService] Null config."); return; }
            if (BarSceneRoot.Instance == null) { MyLogger.LogError("[NightService] BarSceneRoot missing."); return; }

            _config = config;
            _timeRemaining = config.DurationSeconds;
            _spawnTimer = 0f;
            IsRunning = true;
            _updates.AddUpdateListener(this);
            NightStarted?.Invoke();
            MyLogger.LogInfo("[NightService] Night started.");
        }

        public void EndNight()
        {
            if (!IsRunning) return;
            IsRunning = false;
            _updates.RemoveUpdateListener(this);
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var c = _active[i];
                if (c == null) continue;
                c.Served -= HandleCustomerServed;
                c.Left -= HandleCustomerLeft;
                if (c.Seat != null) c.Seat.Clear();
                c.DespawnNow();
            }
            _active.Clear();
            NightEnded?.Invoke();
            MyLogger.LogInfo("[NightService] Night ended.");
        }

        public void MyUpdate()
        {
            if (!IsRunning) return;

            _timeRemaining -= Time.deltaTime;
            CleanupDespawned();

            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer <= 0f)
            {
                TrySpawnCustomer();
                _spawnTimer = _config.SpawnIntervalSeconds;
            }

            if (_timeRemaining <= 0f && _active.Count == 0)
                EndNight();
        }

        private void CleanupDespawned()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
                if (_active[i] == null) _active.RemoveAt(i);
        }

        private void TrySpawnCustomer()
        {
            if (_timeRemaining <= 0f) return;
            if (_active.Count >= _config.MaxSimultaneous) return;

            var root = BarSceneRoot.Instance;
            var seat = root.GetFreeSeat();
            if (seat == null) return;

            var customerPool = _config.CustomerPool;
            var recipes = _config.RecipePool;
            if (customerPool == null || customerPool.Length == 0 || recipes == null || recipes.Length == 0) return;

            var so = customerPool[Random.Range(0, customerPool.Length)];
            var recipe = recipes[Random.Range(0, recipes.Length)];
            if (so == null || so.Prefab == null || recipe == RecipeId.None) return;

            // Serve mini-game: each customer wants one discrete fill level (30/50/70/100%).
            int targetLevel = Random.Range(0, Gameplay.Liquid.FillLevels.Count);

            if (!ServiceLocator.TryGet<ICustomerPoolService>(out var pool))
            {
                MyLogger.LogError("[NightService] ICustomerPoolService not found.");
                return;
            }

            var spawn = root.CustomerSpawnPoint != null ? root.CustomerSpawnPoint.position : Vector3.zero;
            var entity = pool.Spawn(so.Prefab, spawn, Quaternion.identity);
            if (entity == null)
            {
                MyLogger.LogError("[NightService] CustomerPoolService returned null entity.");
                return;
            }

            if (seat.ServeSocket != null)
            {
                seat.ServeSocket.TargetRecipe = recipe;
                seat.ServeSocket.TargetLevel = targetLevel;
            }
            // Subscribe AFTER pool.Spawn (which activates the GO) so OnEnable cannot
            // race with event wiring. Init sets up the FSM and registers the tick.
            entity.Served += HandleCustomerServed;
            entity.Left += HandleCustomerLeft;
            entity.Init(so, seat, recipe, root.CustomerExitPoint);
            entity.TargetLevel = targetLevel;
            seat.Bind(entity);
            _active.Add(entity);
        }

        private void HandleCustomerServed(CustomerEntity c, RecipeId recipe, float score, bool isExact)
        {
            // Tip scales with the serve score (full when perfect, partial when one axis is wrong).
            // RegisterSale also scales the base price by score, so a partial serve pays partially.
            float mult = _config != null && _config.DrunkennessConfig != null
                ? _config.DrunkennessConfig.TipMultiplier(c.Drunkenness)
                : 1f;
            int tip = Mathf.RoundToInt(c.So.BaseTip * Mathf.Clamp01(score) * mult);
            _economy.RegisterSale(recipe, score, tip);
        }

        private void HandleCustomerLeft(CustomerEntity c, bool happy)
        {
            // Unsubscribe before DespawnNow so the pooled entity carries no stale delegates.
            c.Served -= HandleCustomerServed;
            c.Left -= HandleCustomerLeft;
            // Seat.Clear() is also called inside DespawnNow; this is a belt-and-suspenders
            // guard in case the seat was already released by something else.
            if (c.Seat != null) c.Seat.Clear();
            _active.Remove(c);
            if (!happy) _economy.RegisterFailure(c.TargetRecipe);
        }
    }
}

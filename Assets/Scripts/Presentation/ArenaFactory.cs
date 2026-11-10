using System.Collections.Generic;
using Reborn.Core;
using UnityEngine;

namespace Reborn.Presentation
{
    /// <summary>Cenario procedural e placeholders; nao depende de modelos licenciados.</summary>
    public sealed class ArenaFactory
    {
        public Camera Camera { get; private set; }
        public OrbitCamera Orbit { get; private set; }
        public readonly List<ZoneView> Zones = new List<ZoneView>();
        public readonly List<CardView> HandViews = new List<CardView>();
        private readonly Transform root;
        private Transform dynamicRoot;
        private bool handExpanded;
        private int highlightedHandIndex = -1;
        private bool hasPendingPlace;
        private AttackAimLine attackAim;
        private FieldLane pendingLane;
        private DuelSide pendingSide = DuelSide.Player;
        private int pendingSlot;
        private Vector3 pendingOrigin;
        private bool pendingAnimate;
        private readonly Material stone = ArenaVisuals.Mat(ArenaVisuals.Stone);
        private readonly Material dark = ArenaVisuals.Mat(ArenaVisuals.Dark);
        private readonly Material midnight = ArenaVisuals.Mat(ArenaVisuals.Midnight);
        private readonly Material gold = ArenaVisuals.Mat(ArenaVisuals.Gold, true);
        private readonly Material cyan = ArenaVisuals.Mat(ArenaVisuals.Cyan, true);
        private readonly Material red = ArenaVisuals.Mat(ArenaVisuals.Red, true);
        private readonly Material validHover = ArenaVisuals.Mat(
            new Color(0.58f, 1f, 0.91f), true);
        private readonly Material attackHover = ArenaVisuals.Mat(
            new Color(1f, 0.67f, 0.41f), true);
        private readonly Material attackSelected = ArenaVisuals.Mat(
            new Color(1f, 0.93f, 0.36f), true);
        private readonly Material paper = ArenaVisuals.Mat(new Color(0.80f, 0.71f, 0.55f));
        private readonly Material orange = ArenaVisuals.Mat(new Color(0.55f, 0.31f, 0.16f));
        private readonly Material violet = ArenaVisuals.Mat(new Color(0.32f, 0.20f, 0.43f));
        private readonly Material green = ArenaVisuals.Mat(new Color(0.13f, 0.38f, 0.35f));
        private readonly Material back = ArenaVisuals.Mat(new Color(0.12f, 0.09f, 0.16f));

        public ArenaFactory(Transform parent)
        {
            var arena = new GameObject("Arena3D_Generated");
            arena.transform.SetParent(parent, false);
            root = arena.transform;
        }

        public void Build()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.39f, 0.38f, 0.44f);
            ArenaVisuals.Cube("Temple_Base", root, new Vector3(0,-0.9f,0), new Vector3(20, 1.6f, 18), stone);
            ArenaVisuals.Cube("Battle_Table", root, new Vector3(0,-0.05f,0), new Vector3(13.25f,0.34f,13.4f), midnight);
            ArenaVisuals.Cube("Golden_Side_Left", root, new Vector3(-6.65f,0.20f,0), new Vector3(0.15f,0.25f,13.5f), gold);
            ArenaVisuals.Cube("Golden_Side_Right", root, new Vector3(6.65f,0.20f,0), new Vector3(0.15f,0.25f,13.5f), gold);
            ArenaVisuals.Cube("Golden_Center", root, new Vector3(0,0.18f,0.12f), new Vector3(12.8f,0.05f,0.06f), gold);
            ArenaVisuals.Label("RE B O R N   |   A R E N A   0.6", root, new Vector3(0,0.22f,0), ArenaVisuals.Gold, 0.11f);
            BuildTemple(); BuildZones(); BuildDecks(); BuildLightsAndCamera();
            dynamicRoot = new GameObject("DynamicCards").transform;
            dynamicRoot.SetParent(root, false);
        }

        private void BuildTemple()
        {
            for (int side = -1; side <= 1; side += 2)
            {
                for (int z = -6; z <= 6; z += 4)
                {
                    var x = side * 9.0f;
                    ArenaVisuals.Cylinder("Sandstone_Column", root, new Vector3(x,2,z), new Vector3(0.85f,2.3f,0.85f), stone);
                    ArenaVisuals.Cube("Capital_Gold", root, new Vector3(x,4.4f,z), new Vector3(2.1f,0.30f,2.1f), gold);
                    ArenaVisuals.Cube("Column_Base", root, new Vector3(x,0.1f,z), new Vector3(2.1f,0.33f,2.1f), gold);
                }
                ArenaVisuals.Cylinder("Brazier", root, new Vector3(side * 8.35f,0.75f,-7.0f), new Vector3(0.68f,0.48f,0.68f), gold);
            }
            for (var level = 0; level < 4; level++)
                ArenaVisuals.Cube("Pyramid_" + level, root, new Vector3(0,0.4f + level * 0.72f,10f),
                    new Vector3(8f - level*1.8f,0.66f,4.6f - level*0.85f), stone);
        }

        private void BuildZones()
        {
            for (int side = 0; side < 2; side++)
            {
                bool opponent = side == 1;
                float sign = opponent ? 1f : -1f;
                for (int laneIndex = 0; laneIndex < 2; laneIndex++)
                {
                    var lane = laneIndex == 0 ? FieldLane.Monster : FieldLane.Support;
                    float z = sign * (lane == FieldLane.Monster ? 2.0f : 4.35f);
                    for (int i = 0; i < 5; i++)
                    {
                        float x = (i - 2) * 2.4f;
                        var slot = ArenaVisuals.Cube($"{(opponent ? "Opponent" : "Player")}_{lane}_{i+1}",
                            root,new Vector3(x,0.20f,z),new Vector3(1.88f,0.08f,2.1f), dark);
                        var edges = new Renderer[4];
                        edges[0] = ArenaVisuals.Cube("Trim_N",slot.transform,new Vector3(0,0.53f,0.49f),new Vector3(1.0f,0.14f,0.045f),gold).GetComponent<Renderer>();
                        edges[1] = ArenaVisuals.Cube("Trim_S",slot.transform,new Vector3(0,0.53f,-0.49f),new Vector3(1.0f,0.14f,0.045f),gold).GetComponent<Renderer>();
                        edges[2] = ArenaVisuals.Cube("Trim_L",slot.transform,new Vector3(-0.49f,0.53f,0),new Vector3(0.05f,0.14f,1.0f),gold).GetComponent<Renderer>();
                        edges[3] = ArenaVisuals.Cube("Trim_R",slot.transform,new Vector3(0.49f,0.53f,0),new Vector3(0.05f,0.14f,1.0f),gold).GetComponent<Renderer>();
                        var view = slot.AddComponent<ZoneView>();
                        view.Initialize(lane, i, opponent, edges, gold,
                            opponent ? red : cyan,
                            opponent ? attackHover : validHover, attackSelected);
                        Zones.Add(view);
                    }
                }
            }
        }

        private void BuildDecks()
        {
            for (int side = 0; side < 2; side++)
            {
                float sign = side == 0 ? -1f : 1f;
                for (int deck = 0; deck < 2; deck++)
                {
                    var x = deck == 0 ? -6.05f : 6.05f;
                    var z = sign * 4.4f;
                    ArenaVisuals.Cube(deck == 0 ? "Deck" : "Graveyard",root,
                        new Vector3(x,0.3f,z),new Vector3(0.84f,0.20f,1.15f),deck == 0 ? back : stone);
                    ArenaVisuals.Label(deck == 0 ? "DECK" : "GY",root,new Vector3(x,0.43f,z),
                        new Color(1,0.85f,0.54f),0.09f);
                }
            }
        }

        private void BuildLightsAndCamera()
        {
            var lightObj = new GameObject("Moonlight"); lightObj.transform.SetParent(root,false);
            var key = lightObj.AddComponent<Light>(); key.type = LightType.Directional;
            key.intensity = 0.95f; key.color = new Color(0.63f,0.76f,1f);
            lightObj.transform.rotation = Quaternion.Euler(43,-28,0);
            foreach (float x in new [] {-8.35f,8.35f})
            {
                var torch = new GameObject("Torch_Light"); torch.transform.SetParent(root,false);
                torch.transform.position = new Vector3(x,2.2f,-6.3f);
                var point = torch.AddComponent<Light>(); point.type = LightType.Point;
                point.range = 9f; point.intensity = 3.5f; point.color = new Color(1f,0.53f,0.22f);
            }
            var cameraObj = new GameObject("Main Camera"); cameraObj.tag = "MainCamera";
            cameraObj.transform.SetParent(root,false);
            Camera = cameraObj.AddComponent<Camera>();
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.backgroundColor = new Color(0.025f,0.04f,0.075f);
            Camera.fieldOfView = 48f; Camera.nearClipPlane = 0.1f; Camera.farClipPlane = 120f;
            Orbit = cameraObj.AddComponent<OrbitCamera>(); Orbit.ResetView();
        }

        private CardView MakeCard(string name, CardDefinition card, int handIndex, Vector3 pos,
            Vector3 size, bool hidden = false)
        {
            return CardFaceBuilder.Create(name, dynamicRoot, card, handIndex, pos, size, hidden);
        }

        /// <summary>Inverted fan with 0.90 table unit extra spacing when expanded.</summary>
        private CardView MakeHandCard(string name, CardDefinition card, int index, int count)
        {
            var pose = HandFanGeometry.WithHoveredCard(index, count,
                handExpanded, highlightedHandIndex);
            var position = new Vector3(pose.X, pose.Y, pose.Z);
            var view = MakeCard(name, card, index, position,
                new Vector3(HandFanGeometry.CardWidth, 0.12f, HandFanGeometry.CardLength));
            view.SetHandLayoutTarget(position, Quaternion.Euler(0f, pose.Yaw, 0f));
            return view;
        }

        /// <summary>Animate between arena and hand focus without rebuilding objects.</summary>
        public void SetHandFocus(bool expanded)
        {
            if (handExpanded == expanded) return;
            handExpanded = expanded;
            RefreshHandTargets();
        }

        /// <summary>
        /// Neighbor gaps update only when the hovered card changes.
        /// Dragging and numbered fusion selections are not cleared.
        /// </summary>
        public void SetHandHover(int handIndex)
        {
            if (handIndex < -1 || handIndex >= HandViews.Count)
                handIndex = -1;
            if (highlightedHandIndex == handIndex) return;
            highlightedHandIndex = handIndex;
            RefreshHandTargets();
        }

        private void RefreshHandTargets()
        {
            int count = HandViews.Count;
            for (int i = 0; i < count; i++)
            {
                var card = HandViews[i];
                if (card == null) continue;
                var pose = HandFanGeometry.WithHoveredCard(card.HandIndex,
                    count, handExpanded, highlightedHandIndex);
                card.SetHandLayoutTarget(new Vector3(pose.X, pose.Y, pose.Z),
                    Quaternion.Euler(0f, pose.Yaw, 0f));
            }
        }

        /// <summary>
        /// Focus area uses screen projections of BOTH poses, including lifted
        /// hover cards. Avoids flickering between compact/expanded when the
        /// card moves away from the pointer during its animation.
        /// </summary>
        public bool IsPointerNearHand(Vector2 pointer)
        {
            if (Camera == null || dynamicRoot == null || HandViews.Count == 0)
                return false;
            float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;
            int count = HandViews.Count;
            for (int mode = 0; mode < 2; mode++)
            {
                for (int i = 0; i < count; i++)
                {
                    var pose = HandFanGeometry.At(i, count, mode == 1);
                    var center = new Vector3(pose.X, pose.Y, pose.Z);
                    var rotation = Quaternion.Euler(0f, pose.Yaw, 0f);
                    for (int corner = 0; corner < 4; corner++)
                    {
                        float x = (corner & 1) == 0 ?
                            -HandFanGeometry.CardWidth * 0.5f :
                            HandFanGeometry.CardWidth * 0.5f;
                        float z = (corner & 2) == 0 ?
                            -HandFanGeometry.CardLength * 0.5f :
                            HandFanGeometry.CardLength * 0.5f;
                        var basePoint = center + rotation * new Vector3(x, 0f, z);
                        for (int raised = 0; raised < 2; raised++)
                        {
                            var point = basePoint +
                                Vector3.up * (raised == 0 ? 0f : 0.95f);
                            Vector3 screen = Camera.WorldToScreenPoint(
                                dynamicRoot.TransformPoint(point));
                            if (screen.z <= 0f) continue;
                            minX = Mathf.Min(minX, screen.x);
                            maxX = Mathf.Max(maxX, screen.x);
                            minY = Mathf.Min(minY, screen.y);
                            maxY = Mathf.Max(maxY, screen.y);
                        }
                    }
                }
            }
            if (float.IsInfinity(minX)) return false;
            // Allow for additional neighbor displacement during focused hover.
            const float marginX = 30f, marginY = 16f;
            return pointer.x >= minX - marginX && pointer.x <= maxX + marginX &&
                pointer.y >= minY - marginY && pointer.y <= maxY + marginY;
        }

        public void StagePlacement(FieldLane lane, int slot, Vector3 fromWorld, bool animate)
        {
            StageMatchPlacement(DuelSide.Player, lane, slot, fromWorld, animate);
        }

        public void StageMatchPlacement(DuelSide side, FieldLane lane, int slot,
            Vector3 fromWorld, bool animate)
        {
            hasPendingPlace = true;
            pendingSide = side;
            pendingLane = lane;
            pendingSlot = slot;
            pendingOrigin = fromWorld;
            pendingAnimate = animate;
        }

        /// <summary>Realce visual sem alterar as posicoes validas do campo.</summary>
        public void HighlightAttacker(DuelSide side, int slot)
        {
            foreach (var zone in Zones)
                if (zone.Lane == FieldLane.Monster &&
                    zone.Opponent == (side == DuelSide.Opponent))
                    zone.SetCombatTarget(zone.Index == slot);
        }

        /// <summary>
        /// Displays a subtle arc only for a rules-approved player attack target.
        /// A -1 target is the opposing player (direct attack).
        /// </summary>
        public void SetAttackAim(int attackerSlot, int targetSlot)
        {
            if (attackerSlot < 0 || attackerSlot >= DuelMatch.ZoneCount ||
                targetSlot < -1 || targetSlot >= DuelMatch.ZoneCount)
            {
                if (attackAim != null) attackAim.Hide();
                return;
            }
            if (attackAim == null)
            {
                var visual = new GameObject("Player_Legal_Attack_Aim");
                visual.transform.SetParent(root, false);
                attackAim = visual.AddComponent<AttackAimLine>();
            }
            var start = new Vector3((attackerSlot - 2) * 2.4f, 0.95f, -2f);
            var end = targetSlot >= 0
                ? new Vector3((targetSlot - 2) * 2.4f, 0.85f, 2f)
                : new Vector3(0f, 0.85f, 6.2f);
            attackAim.Show(root.TransformPoint(start), root.TransformPoint(end));
        }

        /// <summary>Trajetoria de ataque: somente VFX, nunca altera regras nem camera.</summary>
        public void PlayMatchAttack(DuelSide side, int attackerSlot, int targetSlot)
        {
            if (attackerSlot < 0 || attackerSlot >= DuelMatch.ZoneCount ||
                targetSlot < -1 || targetSlot >= DuelMatch.ZoneCount) return;
            float fromSign = side == DuelSide.Player ? -1f : 1f;
            var start = new Vector3((attackerSlot-2)*2.4f,0.95f,fromSign*2f);
            var end = targetSlot >= 0
                ? new Vector3((targetSlot-2)*2.4f,0.85f,-fromSign*2f)
                : new Vector3(0f,0.85f,-fromSign*6.2f);
            var visual = new GameObject("Attack_" + side + "_" + attackerSlot);
            visual.transform.SetParent(root, false);
            var projectile = visual.AddComponent<AttackProjectile>();
            projectile.Launch(root.TransformPoint(start),root.TransformPoint(end),
                side == DuelSide.Player ? ArenaVisuals.Cyan : ArenaVisuals.Red);
        }

        /// <summary>
        /// Completely procedural, original celebration of a resolved fusion.
        /// No duplicated cards, no camera movement and no gameplay mutation.
        /// </summary>
        public void PlayFieldCombinationEffect(DuelSide side, int monsterSlot,
            bool fused, bool animate)
        {
            if (!animate) return;
            SpawnCeremony(side, monsterSlot,
                fused ? FusionCeremonyVfx.Ceremony.Fusion :
                    FusionCeremonyVfx.Ceremony.Replacement);
        }

        public void PlayRitualCeremony(DuelSide side, int monsterSlot,
            bool animate)
        {
            if (!animate) return;
            SpawnCeremony(side, monsterSlot, FusionCeremonyVfx.Ceremony.Ritual);
        }

        private void SpawnCeremony(DuelSide side, int monsterSlot,
            FusionCeremonyVfx.Ceremony ceremony)
        {
            foreach (var zone in Zones)
            {
                if (zone.Lane != FieldLane.Monster ||
                    zone.Opponent != (side == DuelSide.Opponent) ||
                    zone.Index != monsterSlot) continue;
                string name = ceremony == FusionCeremonyVfx.Ceremony.Ritual
                    ? "Ritual_Invocation_Ceremony" :
                    ceremony == FusionCeremonyVfx.Ceremony.Fusion
                    ? "Field_Fusion_Spark" : "Field_Replacement_Spark";
                var visual = new GameObject(name);
                visual.transform.SetParent(root, false);
                visual.transform.position = zone.transform.position +
                    Vector3.up * 0.30f;
                visual.AddComponent<FusionCeremonyVfx>().Begin(ceremony);
                return;
            }
        }

        public void ClearStagedPlacement() { hasPendingPlace = false; }

        /// <summary>Efeito demonstrativo. Remocao real so ocorrera quando existir motor de combate.</summary>
        public void PlaySandboxRemoval(FieldLane lane, int index)
        {
            foreach (var zone in Zones)
            {
                if (zone.Opponent || zone.Lane != lane || zone.Index != index) continue;
                var fx = new GameObject("SandboxRemoval_VFX");
                fx.transform.SetParent(root, false);
                fx.transform.position = zone.transform.position + Vector3.up * 0.5f;
                var ps = fx.AddComponent<ParticleSystem>();
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = ps.main;
                main.loop = false;
                main.playOnAwake = false;
                main.duration = 0.65f;
                main.startLifetime = 0.5f;
                main.startSpeed = 1.8f;
                main.startSize = 0.13f;
                main.maxParticles = 50;
                main.startColor = ArenaVisuals.Cyan;
                var emission = ps.emission;
                emission.rateOverTime = 0f;
                emission.SetBursts(new [] { new ParticleSystem.Burst(0f, 26) });
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.2f;
                ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = cyan;
                ps.Play();
                Object.Destroy(fx, 2.0f);
                return;
            }
        }

        /// <summary>Render experimental duel without changing the approved camera.</summary>
        public void RefreshMatch(DuelMatch match)
        {
            SetAttackAim(-1, -1);
            if (dynamicRoot == null || match == null) return;
            int previousHandCount = HandViews.Count;
            HandViews.Clear();
            handExpanded = false;
            highlightedHandIndex = -1;
            foreach (Transform child in dynamicRoot)
            {
                child.gameObject.SetActive(false);
                Object.Destroy(child.gameObject);
            }
            var hand = match.Player.Hand;
            for (int i = 0; i < hand.Count; i++)
            {
                var view = MakeHandCard("Match_Hand_" + (i+1), hand[i], i, hand.Count);
                if (i >= previousHandCount)
                    view.AnimateDrawFrom(new Vector3(-6.05f, 0.66f, -4.4f),
                        0.065f * (i - previousHandCount));
                HandViews.Add(view);
            }
            foreach (var side in new [] { DuelSide.Player, DuelSide.Opponent })
            {
                var state = match.Side(side);
                float sign = side == DuelSide.Player ? -1f : 1f;
                for (int i=0; i<5; i++)
                {
                    var monster = state.Monster(i);
                    if (monster != null)
                    {
                        var placed = MakeCard(side+"_Monster_"+i,monster.Card,-1,
                            new Vector3((i-2)*2.4f,0.39f,sign*2.0f),
                            new Vector3(1.51f,0.12f,1.71f), monster.FaceDown);
                        if (monster.Position == BattlePosition.Defense)
                            placed.transform.rotation = Quaternion.Euler(0f,90f,0f);
                        if (side == pendingSide && hasPendingPlace && pendingAnimate &&
                            pendingLane == FieldLane.Monster && pendingSlot == i)
                            placed.gameObject.AddComponent<CardMotion>().Play(pendingOrigin,placed.transform.position);
                    }
                    var support = state.Support(i);
                    if (support != null)
                    {
                        var placed = MakeCard(side+"_Support_"+i,support,-1,
                            new Vector3((i-2)*2.4f,0.39f,sign*4.35f),
                            new Vector3(1.51f,0.12f,1.71f),
                            state.SupportIsFaceDown(i));
                        if (side == pendingSide && hasPendingPlace && pendingAnimate &&
                            pendingLane == FieldLane.Support && pendingSlot == i)
                            placed.gameObject.AddComponent<CardMotion>().Play(pendingOrigin,placed.transform.position);
                    }
                }
            }
            hasPendingPlace = false;
        }

        /// <summary>Pure presentation: impact does not change duel state.</summary>
        public void PlayMatchDestruction(DuelSide side, int slot)
        {
            foreach (var zone in Zones)
            {
                if (zone.Opponent != (side == DuelSide.Opponent) ||
                    zone.Lane != FieldLane.Monster || zone.Index != slot) continue;
                var fx = new GameObject("Combat_Destruction_Particles");
                fx.transform.SetParent(root,false);
                fx.transform.position = zone.transform.position + Vector3.up * 0.5f;
                var particles = fx.AddComponent<ParticleSystem>();
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = particles.main;
                main.loop = false;
                main.playOnAwake = false;
                main.startLifetime = 0.55f;
                main.startSpeed = 1.7f;
                main.startSize = 0.15f;
                main.maxParticles = 64;
                main.startColor = side == DuelSide.Player ? ArenaVisuals.Cyan : ArenaVisuals.Red;
                var emission = particles.emission;
                emission.rateOverTime = 0f;
                emission.SetBursts(new [] { new ParticleSystem.Burst(0f,30) });
                particles.Play();
                Object.Destroy(fx,2f);
                break;
            }
        }

        public void Refresh(DuelSession session)
        {
            SetAttackAim(-1, -1);
            if (dynamicRoot == null) return;
            int previousHandCount = HandViews.Count;
            HandViews.Clear();
            handExpanded = false;
            highlightedHandIndex = -1;
            foreach (Transform child in dynamicRoot)
            {
                // Inativa colliders imediatamente; Destroy() e postergado ate o fim do frame.
                child.gameObject.SetActive(false);
                Object.Destroy(child.gameObject);
            }
            for (int i = 0; i < session.Hand.Count; i++)
            {
                var view = MakeHandCard("Hand_" + (i+1), session.Hand[i], i, session.Hand.Count);
                if (i >= previousHandCount)
                    view.AnimateDrawFrom(new Vector3(-6.05f, 0.66f, -4.4f),
                        0.065f * (i - previousHandCount));
                HandViews.Add(view);
            }
            for (int i = 0; i < 5; i++)
            {
                foreach (var lane in new [] { FieldLane.Monster,FieldLane.Support })
                {
                    var card = session.At(lane,i);
                    if (card == null) continue;
                    float z = lane == FieldLane.Monster ? -2.0f : -4.35f;
                    var placed = MakeCard($"Placed_{lane}_{i}",card,-1,
                        new Vector3((i-2)*2.4f,0.39f,z),
                        new Vector3(1.51f,0.12f,1.71f));
                    if (hasPendingPlace && pendingAnimate && lane == pendingLane && i == pendingSlot)
                    {
                        Vector3 destination = placed.transform.position;
                        placed.gameObject.AddComponent<CardMotion>().Play(pendingOrigin,destination);
                    }
                }
            }
            hasPendingPlace = false;
            MakeCard("Opponent_Demo_1",null,-1,new Vector3(-2.4f,0.39f,2.0f),
                new Vector3(1.51f,0.12f,1.71f),true);
            MakeCard("Opponent_Demo_2",null,-1,new Vector3(2.4f,0.39f,2.0f),
                new Vector3(1.51f,0.12f,1.71f),true);
        }
    }
}

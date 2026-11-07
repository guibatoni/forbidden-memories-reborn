using System.Collections.Generic;
using Reborn.Core;
using UnityEngine;

namespace Reborn.Presentation
{
    /// <summary>
    /// Screen-space combat feedback. Animated LP values never write to the rules engine.
    /// All text and effects are generated; no licensed artwork is embedded.
    /// </summary>
    public sealed class CombatFeedbackOverlay : MonoBehaviour
    {
        private struct Feedback
        {
            public string Text;
            public DuelSide Side;
            public float Started;
            public Color Color;
        }
        private readonly List<Feedback> messages = new List<Feedback>();
        private DuelMatch observed;
        private ArenaHud hud;
        private float shownPlayerLife;
        private float shownOpponentLife;
        private GUIStyle numberStyle;
        private GUIStyle meterStyle;

        public void Initialize(ArenaHud interfaceHud) { hud = interfaceHud; }

        public void Bind(DuelMatch match)
        {
            if (observed != null) observed.EventEmitted -= OnEvent;
            observed = match;
            messages.Clear();
            shownPlayerLife = observed == null ? 0f : observed.Player.Life;
            shownOpponentLife = observed == null ? 0f : observed.Opponent.Life;
            if (observed != null) observed.EventEmitted += OnEvent;
        }

        private void OnEvent(MatchEvent evt)
        {
            if (hud == null || !hud.DuelModeActive || !hud.AnimationsEnabled) return;
            if (evt.Kind == MatchEventKind.LifeChanged && evt.Value != 0)
            {
                Add(evt.Side,"-" + Mathf.Abs(evt.Value) + " LP",new Color(1f,0.46f,0.31f));
            }
            else if (evt.Kind == MatchEventKind.Destroy)
            {
                Add(evt.Side,"DESTRUIDO",new Color(1f,0.8f,0.36f));
            }
            else if (evt.Kind == MatchEventKind.Attack && evt.Value >= 0)
            {
                var attacker = observed.Side(evt.Side).Monster(evt.Slot);
                var defender = observed.Side(DuelMatch.Other(evt.Side)).Monster(evt.Value);
                if (attacker == null || defender == null) return;
                int attackBonus = GuardianStarRules.Bonus(attacker.Star,defender.Star);
                int defenseBonus = GuardianStarRules.Bonus(defender.Star,attacker.Star);
                if (attackBonus > 0) Add(evt.Side,"GUARDIAN +"+attackBonus,new Color(0.37f,0.9f,1f));
                if (defenseBonus > 0) Add(DuelMatch.Other(evt.Side),
                    "GUARDIAN +"+defenseBonus,new Color(0.37f,0.9f,1f));
            }
        }

        private void Add(DuelSide side,string text,Color color)
        {
            messages.Add(new Feedback { Side = side, Text = text, Color = color,
                Started = Time.unscaledTime });
            while (messages.Count > 5) messages.RemoveAt(0);
        }

        private void Update()
        {
            if (observed == null) return;
            // Exponential interpolation makes small and large hits readable without
            // changing the authoritative integer LP values in DuelMatch.
            float fraction = 1f - Mathf.Exp(-5f * Time.unscaledDeltaTime);
            shownPlayerLife = Mathf.Lerp(shownPlayerLife,observed.Player.Life,fraction);
            shownOpponentLife = Mathf.Lerp(shownOpponentLife,observed.Opponent.Life,fraction);
            if (Mathf.Abs(shownPlayerLife-observed.Player.Life)<1f) shownPlayerLife=observed.Player.Life;
            if (Mathf.Abs(shownOpponentLife-observed.Opponent.Life)<1f) shownOpponentLife=observed.Opponent.Life;
            for (int i=messages.Count-1;i>=0;i--)
                if (Time.unscaledTime-messages[i].Started>1.65f) messages.RemoveAt(i);
        }

        private void OnGUI()
        {
            if (hud == null || observed == null || !hud.DuelModeActive || Screen.width<780)
                return;
            if (meterStyle == null)
            {
                meterStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment=TextAnchor.MiddleCenter,fontSize=13,
                    fontStyle=FontStyle.Bold,normal={textColor=new Color(0.93f,0.91f,0.77f)}
                };
                numberStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment=TextAnchor.MiddleCenter,fontSize=22,fontStyle=FontStyle.Bold
                };
            }
            // Two compact LP gauges outside the 3D card zones. No camera changes.
            float left = Screen.width * 0.5f-229f;
            DrawMeter(new Rect(left,16,214,19),"VOCE",shownPlayerLife);
            DrawMeter(new Rect(left+244f,16,214,19),"OPONENTE",shownOpponentLife);
            for (int i=0;i<messages.Count;i++)
            {
                var item=messages[i];
                float t=Mathf.Clamp01((Time.unscaledTime-item.Started)/1.65f);
                var previous=GUI.color;
                GUI.color = new Color(item.Color.r,item.Color.g,item.Color.b,1f-t);
                var x = item.Side == DuelSide.Player ? Screen.width*0.30f : Screen.width*0.70f;
                var y = Screen.height*0.39f - t*72f - i*19f;
                GUI.Label(new Rect(x-130f,y,260f,45f),item.Text,numberStyle);
                GUI.color=previous;
            }
        }

        private void DrawMeter(Rect bounds,string title,float points)
        {
            Color prev=GUI.color;
            GUI.color=new Color(0.025f,0.045f,0.09f,0.89f);
            GUI.DrawTexture(new Rect(bounds.x,bounds.y,bounds.width,34),Texture2D.whiteTexture);
            GUI.color=new Color(0.23f,0.72f,0.85f,0.9f);
            GUI.DrawTexture(new Rect(bounds.x+3,bounds.y+24,
                (bounds.width-6)*Mathf.Clamp01(points/8000f),5),Texture2D.whiteTexture);
            GUI.color=prev;
            GUI.Label(new Rect(bounds.x,bounds.y, bounds.width,23),
                title+"   "+Mathf.RoundToInt(points).ToString("N0")+" LP",meterStyle);
        }

        private void OnDestroy()
        {
            if (observed != null) observed.EventEmitted -= OnEvent;
        }
    }
}
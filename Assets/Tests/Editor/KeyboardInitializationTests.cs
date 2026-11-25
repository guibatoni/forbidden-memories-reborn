using System;
using System.Collections.Generic;
using NUnit.Framework;
using Reborn.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Reborn.Tests
{
    /// <summary>
    /// Regression for UnityException: PlayerPrefs cannot be accessed by
    /// a MonoBehaviour constructor or instance-field initializer.
    /// </summary>
    public sealed class KeyboardInitializationTests
    {
        [Test]
        public void AddingArenaHudComponentInitializesKeyboardWithoutConstructorException()
        {
            var gameObject = new GameObject("REBORN_Keyboard_Init_Test");
            try
            {
                ArenaHud hud = null;
                Assert.DoesNotThrow(() => hud = gameObject.AddComponent<ArenaHud>(),
                    "AddComponent must not read Unity PlayerPrefs in a field initializer");
                Assert.IsNotNull(hud);
                Assert.IsNotNull(hud.KeyBindings, "Awake must build and load keys");
                var seen = new HashSet<Key>();
                foreach (var action in RebornKeyBindings.Actions)
                {
                    var key = hud.KeyBindings.Get(action);
                    Assert.AreNotEqual(Key.None,key);
                    Assert.IsTrue(seen.Add(key),"Duplicate saved key mapping: " + action);
                }
                Assert.IsFalse(hud.KeyboardSettingsOpen);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void KeyboardBindingsConstructorHasDefaultsEvenBeforeUnityAwake()
        {
            RebornKeyBindings keys = null;
            Assert.DoesNotThrow(() => keys = new RebornKeyBindings());
            Assert.AreEqual(Key.Space, keys.Get(RebornKeyAction.PassTurn));
            Assert.AreEqual(Key.Q, keys.Get(RebornKeyAction.AttackPosition));
            Assert.AreEqual(Key.E, keys.Get(RebornKeyAction.DefensePosition));
            Assert.AreEqual(Key.Z, keys.Get(RebornKeyAction.Select));
        }
    }
}

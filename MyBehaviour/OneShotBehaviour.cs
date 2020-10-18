﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DecorationMaster.Util;
using UnityEngine;
using DecorationMaster.Attr;
using System.Collections;

namespace DecorationMaster.MyBehaviour
{
    public class OneShotBehaviour
    {
        [Decoration("IMG_recoverDash")]
        [Description("给予一次冲刺能力")]
        [Description("give player dash ability once \n yay just like Celeste", "en-us")]
        public class RecoverDash : CustomDecoration
        {
            public static AudioClip clip { get {
                    if (_c)
                        return _c;
                    _c = WavHelper.GetAudioClip("eat_crystal");
                    return _c; } }
            private static AudioClip _c;
            private AudioSource au;
            private bool used;
            private HeroTrigger ht;
            private void Awake()
            {
                
                au = gameObject.AddComponent<AudioSource>();
                //var sr = gameObject.AddComponent<SpriteRenderer>();
                //var col = gameObject.AddComponent<BoxCollider2D>();
                ht = gameObject.AddComponent<HeroTrigger>();
                ht.HeroEnter = RecoveOneshot;
                transform.localScale *= 2;
            }
            private void RecoveOneshot() 
            {
                if (used)
                    return;
                used = true;
                On.HeroController.CanDash += True;
                On.HeroController.HeroDash += RemoveHook;
                au.PlayOneShot(clip);
                StartCoroutine(Consume());
                
                IEnumerator Consume()
                {
                    gameObject.GetComponent<SpriteRenderer>().enabled = false;
                    ht.enabled = false;
                    yield return new WaitWhile(() => au.isPlaying);

                    yield return new WaitForSeconds(3);
                    gameObject.GetComponent<SpriteRenderer>().enabled = true;
                    ht.enabled = true;
                    used = false;
                }
                
                
                //Logger.LogDebug("Eat Recover Dash");
            }

            private bool True(On.HeroController.orig_CanDash orig, HeroController self)
            {
                return true;
            }

            private void RemoveHook(On.HeroController.orig_HeroDash orig, HeroController self)
            {
                orig(self);
                On.HeroController.CanDash -= True;
            }
        }
        [Decoration("IMG_recoverJump")]
        [Description("给予一次二段跳能力")]
        [Description("give player double ability once", "en-us")]
        public class RecoverWingJump : CustomDecoration
        {
            public static AudioClip clip => RecoverDash.clip;
            private AudioSource au;
            private HeroTrigger ht;
            private bool used;
            private void Awake()
            {
                au = gameObject.AddComponent<AudioSource>();
                //var sr = gameObject.AddComponent<SpriteRenderer>();
               // var col = gameObject.AddComponent<BoxCollider2D>();
                ht = gameObject.AddComponent<HeroTrigger>();
                ht.HeroEnter = RecoveOneshot;
                transform.localScale *= 2;
            }
            private void RecoveOneshot()
            {
                if (used)
                    return;
                used = true;
                On.HeroController.CanDoubleJump += True;
                On.HeroController.DoDoubleJump += RemoveHook;
                au.PlayOneShot(clip);
                StartCoroutine(Consume());

                IEnumerator Consume()
                {
                    gameObject.GetComponent<SpriteRenderer>().enabled = false;
                    ht.enabled = false;
                    yield return new WaitWhile(() => au.isPlaying);

                    yield return new WaitForSeconds(3);
                    gameObject.GetComponent<SpriteRenderer>().enabled = true;
                    ht.enabled = true;
                    used = false;

                }
            }

            private bool True(On.HeroController.orig_CanDoubleJump orig, HeroController self)
            {
                return true;
            }

            private void RemoveHook(On.HeroController.orig_DoDoubleJump orig, HeroController self)
            {
                orig(self);
                On.HeroController.CanDoubleJump -= True;
            }
        }
    }
    
}
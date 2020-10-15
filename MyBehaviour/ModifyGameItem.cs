﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DecorationMaster.UI;
using UnityEngine;
using DecorationMaster.Attr;
using DecorationMaster.Util;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using UnityEngine.SceneManagement;
using System.Collections;

namespace DecorationMaster.MyBehaviour
{
    public class ModifyGameItem
    {
        [Decoration("disable_col")]
        [Description("禁用游戏原本的某些物体(区域、平台)\n注意：请谨慎使用该物品，特别不要对任何墙体使用。建议用途为禁用某些小平台\n注意：请不要用鼠标选中此物品")]
        public class DisableCollider : CustomDecoration
        {
            private BoxCollider2D col;
            private void Awake()
            {
                col = gameObject.AddComponent<BoxCollider2D>();
                col.size = Vector2.one * 0.5f;
                var rb = gameObject.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 0;
                if(SetupMode)
                    gameObject.AddComponent<ShowColliders>();
                gameObject.name = "Disable Collider";
                gameObject.layer = (int)GlobalEnums.PhysLayers.WATER;
                UnVisableBehaviour.AttackReact.Create(gameObject);


            }
            private void OnCollisionEnter2D(Collision2D collision)
            {
                if (collision.gameObject.name.Contains("Slash") || collision.gameObject.name.Contains("Knight") || collision.gameObject.name.Contains("Hero"))
                    return;
                if (collision.gameObject.GetComponent<CustomDecoration>() != null)
                    return;

                collision.gameObject.SetActive(false);
                Logger.LogDebug($"Disable {collision.gameObject.name}");
                col.enabled = false;
                Destroy(gameObject.GetComponent<Rigidbody2D>());
            }

        }
        [Decoration("disable_hazard_spawn")]
        [Description("禁用游戏原本的刷新存档点的地方")]
        public class DisableRespawnTrigger : CustomDecoration
        {
            private BoxCollider2D col;
            private void Awake()
            {
                col = gameObject.AddComponent<BoxCollider2D>();
                col.size = Vector2.one;
                if (SetupMode)
                    gameObject.AddComponent<ShowColliders>();
                gameObject.name = "Disable Hazard Respawn Trigger";
                gameObject.layer = (int)GlobalEnums.PhysLayers.PLAYER;
                UnVisableBehaviour.AttackReact.Create(gameObject);
                col.isTrigger = true;
                var rb = gameObject.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
            private void OnTriggerEnter2D(Collider2D collider)
            {
                if(collider.gameObject.GetComponent<HazardRespawnTrigger>() == null)
                {
                    Logger.LogDebug("not respawn box");
                    return;
                }
                collider.gameObject.SetActive(false);
                Logger.LogDebug($"Disable respawn Trigger {collider.gameObject.name}");
                col.enabled = false;
            }
        }
    
        //[Decoration("gg_destroy")]
        public class DisableGGPrefab:CustomDecoration
        {

        }

        [Decoration("remove_scene")]
        [Description("移除场景内所有物体（除了进出的门）\n未测试物品，谨慎使用")]
        public class DisableRootObjs:CustomDecoration
        {
            private void Awake()
            {
                UnVisableBehaviour.AttackReact.Create(gameObject);
            }
            private IEnumerator Start()
            {
                yield return new WaitForSceneLoadFinish();
                Scene s = USceneManager.GetActiveScene();
                foreach (var g in s.GetRootGameObjects())
                {
                    if (g.name.Contains("_Transition"))
                    {
                        if(SetupMode)
                            g.gameObject.AddComponent<ShowColliders>();
                        continue;
                    }
                    if (g.GetComponent<CustomDecoration>() != null)
                        continue;
                    Destroy(g.gameObject);
                }
            }
        }
    }
}

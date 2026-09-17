using System.Collections.Generic;
using UnityEngine;
using LostIsland.Logic.Wave;

namespace LostIsland.Logic.Tower
{
    #region 机枪塔 - 快速单体射击

    /// <summary>
    /// 机枪塔：快速单体攻击
    /// 特点：高攻速、低伤害、单体
    /// </summary>
    public class DefenseTower_MachineGun : DefenseTowerBase
    {
        protected override void FireAttack(ZombieController target)
        {
            if (target == null || target.IsDead) return;

            // 直接造成伤害（即时命中）
            DealDamage(target, Damage);

            // 触发攻击事件
            // TODO: 播放射击特效
        }
    }

    #endregion

    #region 炮塔 - 范围爆炸

    /// <summary>
    /// 炮塔：范围爆炸伤害
    /// 特点：低攻速、高伤害、AOE
    /// </summary>
    public class DefenseTower_Cannon : DefenseTowerBase
    {
        protected override void FireAttack(ZombieController target)
        {
            if (target == null || target.IsDead) return;

            Vector3 explosionPos = target.MoveComp.Position;
            float radius = Config != null ? Config.AoeRadius : 2f;

            // 对范围内所有丧尸造成伤害
            var spawner = ZombieSpawner.Instance;
            if (spawner == null) return;

            var zombies = spawner.ActiveZombies;
            int hitCount = 0;

            foreach (var zombie in zombies)
            {
                if (zombie == null || zombie.IsDead) continue;

                float dist = Vector3.Distance(explosionPos, zombie.MoveComp.Position);
                if (dist <= radius)
                {
                    // 距离衰减：越靠近中心伤害越高
                    float falloff = 1f - (dist / radius) * 0.5f; // 边缘伤害=50%
                    float dmg = Damage * falloff;
                    DealDamage(zombie, dmg);
                    hitCount++;
                }
            }

            // Debug.Log($"[炮塔] 爆炸命中 {hitCount} 个目标，伤害={Damage:F0}");
        }
    }

    #endregion

    #region 箭塔 - 穿透射击

    /// <summary>
    /// 箭塔：穿透直线射击
    /// 特点：中攻速、中伤害、穿透多个目标
    /// </summary>
    public class DefenseTower_Arrow : DefenseTowerBase
    {
        protected override void FireAttack(ZombieController target)
        {
            if (target == null || target.IsDead) return;

            Vector3 direction = (target.MoveComp.Position - TowerPosition).normalized;
            int pierceCount = Config != null ? Config.PierceCount : 3;
            float range = Range;

            // 找射线路径上的所有丧尸
            var spawner = ZombieSpawner.Instance;
            if (spawner == null) return;

            var zombies = spawner.ActiveZombies;

            // 按距离排序，优先命中最近的
            List<(ZombieController zombie, float dist)> hits = new List<(ZombieController, float)>();

            foreach (var zombie in zombies)
            {
                if (zombie == null || zombie.IsDead) continue;

                Vector3 toZombie = zombie.MoveComp.Position - TowerPosition;
                float dist = toZombie.magnitude;

                if (dist > range) continue;

                // 检查是否在射击线上（点积判定）
                float dot = Vector3.Dot(direction, toZombie.normalized);
                if (dot < 0.8f) continue; // 约37度角内算命中

                // 检查横向偏移
                Vector3 proj = direction * Vector3.Dot(toZombie, direction);
                float lateralDist = (toZombie - proj).magnitude;
                if (lateralDist > 0.8f) continue; // 0.8米内算命中

                hits.Add((zombie, dist));
            }

            // 按距离排序，取前pierceCount个
            hits.Sort((a, b) => a.dist.CompareTo(b.dist));

            int count = Mathf.Min(pierceCount, hits.Count);
            for (int i = 0; i < count; i++)
            {
                // 穿透伤害衰减：每穿过一个目标-15%
                float dmgMult = Mathf.Pow(0.85f, i);
                DealDamage(hits[i].zombie, Damage * dmgMult);
            }
        }
    }

    #endregion

    #region 电击塔 - 链式闪电

    /// <summary>
    /// 电击塔：链式闪电
    /// 特点：中攻速、中伤害、链式跳跃
    /// </summary>
    public class DefenseTower_Tesla : DefenseTowerBase
    {
        protected override void FireAttack(ZombieController target)
        {
            if (target == null || target.IsDead) return;

            int chainCount = Config != null ? Config.ChainCount : 3;
            float chainRange = Config != null ? Config.ChainRange : 3f;

            var spawner = ZombieSpawner.Instance;
            if (spawner == null) return;

            var zombies = spawner.ActiveZombies;
            HashSet<ZombieController> hitSet = new HashSet<ZombieController>();
            List<ZombieController> chainOrder = new List<ZombieController>();

            ZombieController current = target;
            hitSet.Add(current);
            chainOrder.Add(current);

            // 链式跳跃
            for (int i = 1; i < chainCount; i++)
            {
                ZombieController next = FindNextChainTarget(current, zombies, hitSet, chainRange);
                if (next == null) break;

                hitSet.Add(next);
                chainOrder.Add(next);
                current = next;
            }

            // 造成伤害（链式衰减）
            for (int i = 0; i < chainOrder.Count; i++)
            {
                float dmgMult = Mathf.Pow(0.8f, i); // 每跳-20%
                DealDamage(chainOrder[i], Damage * dmgMult);
            }
        }

        /// <summary>
        /// 寻找下一个链式目标
        /// </summary>
        private ZombieController FindNextChainTarget(ZombieController current,
            List<ZombieController> zombies, HashSet<ZombieController> hitSet, float chainRange)
        {
            ZombieController best = null;
            float bestDist = float.MaxValue;

            foreach (var zombie in zombies)
            {
                if (zombie == null || zombie.IsDead) continue;
                if (hitSet.Contains(zombie)) continue;

                float dist = Vector3.Distance(current.MoveComp.Position, zombie.MoveComp.Position);
                if (dist <= chainRange && dist < bestDist)
                {
                    bestDist = dist;
                    best = zombie;
                }
            }

            return best;
        }
    }

    #endregion
}

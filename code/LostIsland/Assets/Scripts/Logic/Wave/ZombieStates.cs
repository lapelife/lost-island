using UnityEngine;
using LostIsland.Logic.Wave;

namespace LostIsland.Logic.Wave
{
    /// <summary>
    /// 丧尸AI状态枚举
    /// </summary>
    public enum ZombieState
    {
        None = 0,
        Spawn = 1,      // 出生/生成动画
        Chase = 2,      // 追击目标
        Attack = 3,     // 攻击中
        Stun = 4,       // 被眩晕/减速
        Dead = 5,       // 死亡
    }

    /// <summary>
    /// 丧尸AI状态基类
    /// 轻量级状态机，每个状态有 Enter/Update/Exit
    /// </summary>
    public abstract class ZombieStateBase
    {
        protected ZombieController _zombie;

        public ZombieStateBase(ZombieController zombie)
        {
            _zombie = zombie;
        }

        public abstract ZombieState StateType { get; }

        public virtual void Enter() { }
        public virtual void Update(float dt) { }
        public virtual void Exit() { }
    }

    /// <summary>
    /// 出生状态
    /// </summary>
    public class ZombieState_Spawn : ZombieStateBase
    {
        public override ZombieState StateType => ZombieState.Spawn;

        private float _spawnDuration = 0.5f;
        private float _timer = 0f;

        public ZombieState_Spawn(ZombieController zombie) : base(zombie) { }

        public override void Enter()
        {
            _timer = 0f;
            // 出生时不可移动、不可攻击
            _zombie.MoveComp?.StopMove();
        }

        public override void Update(float dt)
        {
            _timer += dt;
            if (_timer >= _spawnDuration)
            {
                _zombie.ChangeState(ZombieState.Chase);
            }
        }
    }

    /// <summary>
    /// 追击状态：朝目标移动
    /// </summary>
    public class ZombieState_Chase : ZombieStateBase
    {
        public override ZombieState StateType => ZombieState.Chase;

        public ZombieState_Chase(ZombieController zombie) : base(zombie) { }

        public override void Enter()
        {
            UpdateMoveDirection();
        }

        public override void Update(float dt)
        {
            // 更新目标方向
            UpdateMoveDirection();

            // 检查是否进入攻击范围
            if (_zombie.IsTargetInAttackRange())
            {
                _zombie.ChangeState(ZombieState.Attack);
            }
        }

        private void UpdateMoveDirection()
        {
            Vector3 targetPos = _zombie.GetTargetPosition();
            Vector3 myPos = _zombie.MoveComp.Position;
            Vector3 dir = (targetPos - myPos).normalized;

            if (dir.sqrMagnitude > 0.001f)
            {
                _zombie.MoveComp.SetMoveDirection(dir);
            }
        }
    }

    /// <summary>
    /// 攻击状态
    /// </summary>
    public class ZombieState_Attack : ZombieStateBase
    {
        public override ZombieState StateType => ZombieState.Attack;

        private float _attackTimer = 0f;
        private bool _hasDealtDamage = false;

        public ZombieState_Attack(ZombieController zombie) : base(zombie) { }

        public override void Enter()
        {
            _attackTimer = 0f;
            _hasDealtDamage = false;
            _zombie.MoveComp?.StopMove();
        }

        public override void Update(float dt)
        {
            _attackTimer += dt;

            // 攻击前摇结束时造成伤害
            if (!_hasDealtDamage && _attackTimer >= _zombie.Config.AttackWindup)
            {
                _zombie.DealDamageToTarget();
                _hasDealtDamage = true;
            }

            // 攻击动作完成
            float totalAttackTime = _zombie.Config.AttackWindup + _zombie.Config.AttackCooldown;
            if (_attackTimer >= totalAttackTime)
            {
                // 检查目标是否还在范围内
                if (_zombie.IsTargetInAttackRange())
                {
                    // 继续攻击
                    _attackTimer = 0f;
                    _hasDealtDamage = false;
                }
                else
                {
                    // 目标离开范围，回到追击
                    _zombie.ChangeState(ZombieState.Chase);
                }
            }
        }

        public override void Exit()
        {
            _hasDealtDamage = false;
        }
    }

    /// <summary>
    /// 眩晕状态
    /// </summary>
    public class ZombieState_Stun : ZombieStateBase
    {
        public override ZombieState StateType => ZombieState.Stun;

        private float _stunDuration = 0f;
        private float _timer = 0f;

        public ZombieState_Stun(ZombieController zombie) : base(zombie) { }

        public void SetDuration(float duration)
        {
            _stunDuration = duration;
        }

        public override void Enter()
        {
            _timer = 0f;
            _zombie.MoveComp?.StopMove();
        }

        public override void Update(float dt)
        {
            _timer += dt;
            if (_timer >= _stunDuration)
            {
                _zombie.ChangeState(ZombieState.Chase);
            }
        }
    }

    /// <summary>
    /// 死亡状态
    /// </summary>
    public class ZombieState_Dead : ZombieStateBase
    {
        public override ZombieState StateType => ZombieState.Dead;

        private float _deathDuration = 1f;
        private float _timer = 0f;

        public ZombieState_Dead(ZombieController zombie) : base(zombie) { }

        public override void Enter()
        {
            _timer = 0f;
            _zombie.MoveComp?.StopMove();
            // 停止攻击组件
            var attackComp = _zombie.Entity.GetComponent<AttackComponent>();
            attackComp?.SetAttackDisabled(true);
        }

        public override void Update(float dt)
        {
            _timer += dt;
            if (_timer >= _deathDuration)
            {
                // 通知可以回收了
                _zombie.OnDeathComplete();
            }
        }

        public override void Exit()
        {
            // 死亡状态不应该被退出
        }
    }
}

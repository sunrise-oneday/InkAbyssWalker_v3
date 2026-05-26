using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 状态抽象基类
/// </summary>
public abstract class BaseState<T> where T : class
{
    protected T owner { get; private set; }

    protected StateMachine<T> stateMachine { get; private set; }

    protected float stateTimer;

    /// <summary>
    /// 初始化方法，在状态注册时被自动调用
    /// </summary>
    /// <param name="owner"></param>
    /// <param name="stateMachine"></param>
    public void Init(T owner, StateMachine<T> stateMachine)
    {
        this.owner = owner;
        this.stateMachine = stateMachine;
        OnInit();
    }

    protected virtual void OnInit() { }
    public virtual void Enter()
    {
        stateTimer = 0f;
    }
    public virtual void Update()
    {
        stateTimer += Time.deltaTime;
    }
    public virtual void FixedUpdate() { }
    public virtual void Exit() { }
}

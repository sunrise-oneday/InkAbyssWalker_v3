using System;
using System.Collections.Generic;
using UnityEngine;

public class StateMachine<T> where T : class
{
    public BaseState<T> currentState { get; private set; }

    // 新增：记录上一个状态，方便在受击、暂停、打断后快速恢复
    public BaseState<T> previousState { get; private set; }

    private readonly T owner;
    private readonly Dictionary<Type, BaseState<T>> stateDic = new Dictionary<Type, BaseState<T>>();

    public StateMachine(T owner)
    {
        this.owner = owner;
    }

    public void RegisterState(BaseState<T> state)
    {
        Type type = state.GetType();
        if (!stateDic.ContainsKey(type))
        {
            state.Init(owner, this);
            stateDic.Add(type, state);
        }
    }

    public void ChangeState<TState>() where TState : BaseState<T>
    {
        Type type = typeof(TState);
        if (stateDic.TryGetValue(type, out BaseState<T> newState))
        {
            currentState?.Exit();

            // 切换前，将当前状态记录为上一个状态
            previousState = currentState;

            currentState = newState;
            currentState.Enter();
        }
        else
        {
            Debug.LogWarning($"未能在状态机中找到状态: {type.Name}。请确保该状态已注册。");
        }
    }

    // 新增：快速返回上一个状态的便利方法
    public void RevertToPreviousState()
    {
        if (previousState != null)
        {
            currentState?.Exit();

            BaseState<T> temp = currentState;
            currentState = previousState;
            previousState = temp;

            currentState.Enter();
        }
    }

    /// <summary>
    /// 核心新增：从状态机中获取已注册的特定状态实例（高可复用性）
    /// </summary>
    public TState GetState<TState>() where TState : BaseState<T>
    {
        Type type = typeof(TState);
        if (stateDic.TryGetValue(type, out BaseState<T> state))
        {
            return state as TState;
        }
        return null;
    }

    public void Update() => currentState?.Update();
    public void FixedUpdate() => currentState?.FixedUpdate();
}
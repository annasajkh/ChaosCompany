using System;
using UnityEngine;

namespace ChaosCompany.Scripts.Components;

public sealed class Timer
{
    /// <summary>
    /// if the timer finished
    /// </summary>
    public bool Finished { get; private set; }

    /// <summary>
    /// The time the timer have to wait before it time out
    /// </summary>
    public float WaitTime { get; set; }

    /// <summary>
    /// Whether it's only timeout once
    /// </summary>
    public bool Oneshot { get; set; }

    /// <summary>
    /// The time it have left before it time out
    /// </summary>
    public float TimeLeft { get; private set; }

    /// <summary>
    /// Whether it's paused
    /// </summary>
    public bool Paused { get; private set; } = true;

    /// <summary>
    /// Trigged when TimeLeft reached 0
    /// </summary>
    public event Action? OnTimeout;

    /// <summary>
    /// The constructor
    /// </summary>
    /// <param name="waitTime">The time before it time out (in seconds)</param>
    /// <param name="oneshot">Whether it's only timeout once</param>
    public Timer(float waitTime, bool oneshot)
    {
        WaitTime = waitTime;
        Oneshot = oneshot;
    }

    /// <summary>
    /// Start the timer
    /// </summary>
    public void Start()
    {
        Paused = false;
    }

    /// <summary>
    /// Stop the timer
    /// </summary>
    public void Stop()
    {
        Paused = true;
    }

    /// <summary>
    /// Update the timer so it run duh
    /// </summary>
    public void Update()
    {
        if (!Paused)
        {
            TimeLeft += Time.deltaTime;

            if (TimeLeft >= WaitTime)
            {
                TimeLeft = 0;

                if (OnTimeout != null)
                {
                    OnTimeout();

                    if (Oneshot)
                    {
                        Finished = true;
                    }
                }

                if (Oneshot)
                {
                    Paused = true;
                }
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
public struct TimedCheerInput
{
    public CheerDirection direction;
    public double timestamp;

    public TimedCheerInput(CheerDirection dir, double time)
    {
        direction = dir;
        timestamp = time;
    }
}

public class CheerInputBridge : MonoBehaviour
{
    public static CheerInputBridge Instance { get; private set; }
    private Queue<TimedCheerInput> inputQueue = new();
    

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    private double GetAccurateTimestamp()
    {
        return AudioSettings.dspTime > 0 ? AudioSettings.dspTime : Time.time;
    }

    public void OnPressUp()    => EnqueueDirection(CheerDirection.Up);
    public void OnPressDown()  => EnqueueDirection(CheerDirection.Down);
    public void OnPressLeft()  => EnqueueDirection(CheerDirection.Left);
    public void OnPressRight() => EnqueueDirection(CheerDirection.Right);

    private void EnqueueDirection(CheerDirection dir)
    {
        double timestamp = GetAccurateTimestamp();
        inputQueue.Enqueue(new TimedCheerInput(dir, timestamp));
        Debug.Log($"[INPUT] {dir} at {timestamp:F3}");
    }


    public void DiscardBefore(double minTimestamp)
    {
        while (inputQueue.Count > 0 && inputQueue.Peek().timestamp < minTimestamp)
            inputQueue.Dequeue();
    }

    public bool TryGetNextDirection(out CheerDirection dir, out double timestamp)
    {
        if (inputQueue.Count > 0)
        {
            var timedInput = inputQueue.Dequeue();
            dir = timedInput.direction;
            timestamp = timedInput.timestamp;
            return true;
        }

        dir = default;
        timestamp = default;
        return false;
    }

    public void Clear()
    {
        inputQueue.Clear();
    }
}
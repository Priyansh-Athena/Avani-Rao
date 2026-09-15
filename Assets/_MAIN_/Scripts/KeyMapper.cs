using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class KeyCodeLogger : MonoBehaviour
{
    [SerializeField] private TMP_Text keysTxt;

    private KeyCode[] keyCodes;

    private readonly string[] axisNames =
    {
        "Horizontal", "Vertical",
        "Fire1", "Fire2", "Fire3",
        "Jump", "Mouse X", "Mouse Y",
        "Axis 1", "Axis 2", "Axis 3",
        "Axis 4", "Axis 5", "Axis 6"
    };

    private readonly HashSet<string> missingAxes = new HashSet<string>();
    private readonly Queue<LogEntry> recentKeys = new Queue<LogEntry>();
    private readonly StringBuilder display = new StringBuilder();

    private struct LogEntry
    {
        public float time;
        public string message;
    }

    private void Awake()
    {
        var uniqueKeys = new HashSet<KeyCode>(
            (KeyCode[])Enum.GetValues(typeof(KeyCode))
        );

        uniqueKeys.Remove(KeyCode.None);
        keyCodes = new KeyCode[uniqueKeys.Count];
        uniqueKeys.CopyTo(keyCodes);
    }

    private void Update()
    {
        float now = Time.unscaledTime;

        foreach (KeyCode key in keyCodes)
        {
            if (Input.GetKeyDown(key))
            {
                recentKeys.Enqueue(new LogEntry
                {
                    time = now,
                    message = $"KeyCode.{key} pressed — value: {(int)key}"
                });
            }
        }

        // Keep every key press from the previous one second.
        while (recentKeys.Count > 0 &&
               now - recentKeys.Peek().time >= 1f)
        {
            recentKeys.Dequeue();
        }

        display.Clear();

        foreach (LogEntry entry in recentKeys)
        {
            display.AppendLine(entry.message);
        }

        // Show current axis values without adding a log every frame.
        foreach (string axis in axisNames)
        {
            if (missingAxes.Contains(axis))
                continue;

            float value;

            try
            {
                value = Input.GetAxis(axis);
            }
            catch (ArgumentException)
            {
                missingAxes.Add(axis);
                continue;
            }

            if (Mathf.Abs(value) > 0.1f)
            {
                display.AppendLine($"{axis}: {value:F3}");
            }
        }

        if (keysTxt != null)
        {
            keysTxt.text = display.ToString();
        }
    }
}
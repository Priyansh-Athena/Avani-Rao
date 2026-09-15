using System;
using System.Collections.Generic;
using UnityEngine;

public class KeyCodeLogger : MonoBehaviour
{
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

    private void Awake()
    {
        // Remove duplicate enum values (some KeyCodes have aliases).
        var uniqueKeys = new HashSet<KeyCode>(
            (KeyCode[])Enum.GetValues(typeof(KeyCode))
        );

        uniqueKeys.Remove(KeyCode.None);
        keyCodes = new KeyCode[uniqueKeys.Count];
        uniqueKeys.CopyTo(keyCodes);
    }

    private void Update()
    {
        // Includes keyboard keys, mouse buttons, and joystick buttons.
        foreach (KeyCode key in keyCodes)
        {
            if (Input.GetKeyDown(key))
            {
                Debug.Log($"KeyCode.{key} pressed — value: {(int)key}");
            }
        }

        // Try common Unity axes.
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
                Debug.Log($"{axis}: {value}");
            }
        }
    }
}
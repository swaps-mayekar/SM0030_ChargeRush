using UnityEditor;
using UnityEngine;
using ChargeRush.Data;

namespace ChargeRush.Editor
{
    [CustomEditor(typeof(LevelData))]
    public sealed class LevelDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var level = (LevelData)target;
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                $"Stars: 1@{level.TargetEarnings}  2@{level.TwoStarThreshold}  3@{level.ThreeStarThreshold}\nPorts: {level.ChargingPortCount}  Queue: {level.MaximumVisibleQueue}",
                MessageType.Info);

            if (level.TwoStarThreshold < level.TargetEarnings || level.ThreeStarThreshold < level.TwoStarThreshold)
            {
                EditorGUILayout.HelpBox("Star thresholds are out of order.", MessageType.Error);
            }
        }
    }

    [CustomEditor(typeof(DeviceData))]
    public sealed class DeviceDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var device = (DeviceData)target;
            EditorGUILayout.HelpBox($"{device.DisplayName} uses {device.RequiredConnector} and earns {device.ServicePrice} credits.", MessageType.Info);
        }
    }

    [CustomEditor(typeof(CustomerData))]
    public sealed class CustomerDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var customer = (CustomerData)target;
            EditorGUILayout.HelpBox($"{customer.DisplayName}: patience {customer.BasePatienceSeconds:0}s, pay x{customer.PaymentMultiplier:0.00}.", MessageType.Info);
        }
    }
}

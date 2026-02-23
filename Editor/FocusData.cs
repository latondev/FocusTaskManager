using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace AshDev.Focus
{
    [Serializable]
    public class ChecklistItem
    {
        public string title;
        public bool isChecked;
    }

    [Serializable]
    public class TaskData
    {
        public string id = Guid.NewGuid().ToString();
        public string title;
        public string description;
        public string date;
        public List<Object> attachments = new List<Object>();
        public List<ChecklistItem> checklist = new List<ChecklistItem>();

        public string GetProgressString() => $"{checklist.FindAll(x => x.isChecked).Count}/{checklist.Count}";
    }

    [Serializable]
    public class TaskColumn
    {
        public string id = Guid.NewGuid().ToString();
        public string title;
        public List<TaskData> tasks = new List<TaskData>();
    }

    [CreateAssetMenu(fileName = "FocusBoardData", menuName = "Focus/BoardData")]
    public class FocusBoardData : ScriptableObject
    {
        public List<TaskColumn> columns = new List<TaskColumn>();
        public List<string> availableTags = new List<string> { "Bug", "Feature", "Refactor", "UI", "Performance", "High Priority", "Low Priority" };
    }
}
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

        // --- NEW FIELDS FOR PREMIUM UI ---
        public TaskPriority priority = TaskPriority.None;
        public List<string> tags = new List<string>(); // ID hoặc tên tags
        public List<string> assignees = new List<string>(); // ID hoặc tên assignees
        public string sprintId = "";
        public string startDate = "";
        public string dueDate = "";
        public float timeTracked = 0f; // Bấm giờ

        public string GetProgressString() => $"{checklist.FindAll(x => x.isChecked).Count}/{checklist.Count}";
    }

    [Serializable]
    public class TaskColumn
    {
        public string id = Guid.NewGuid().ToString();
        public string title;
        public List<TaskData> tasks = new List<TaskData>();
    }

    [Serializable]
    public class SprintData
    {
        public string id = Guid.NewGuid().ToString();
        public string title;
        public string startDate;
        public string endDate;
        public bool isActive;
    }

    [Serializable]
    public class AssigneeData
    {
        public string id = Guid.NewGuid().ToString();
        public string name;
        public Color color;
    }

    [CreateAssetMenu(fileName = "FocusBoardData", menuName = "Focus/BoardData")]
    public class FocusBoardData : ScriptableObject
    {
        public List<TaskColumn> columns = new List<TaskColumn>();
        public List<string> availableTags = new List<string> { "Bug", "Feature", "Refactor", "UI", "Performance", "High Priority", "Low Priority" };
        public List<SprintData> sprints = new List<SprintData>();
        public List<AssigneeData> availableAssignees = new List<AssigneeData>
        {
            new AssigneeData { name = "Marco", color = new Color(0.2f, 0.6f, 1f) },
            new AssigneeData { name = "Alex", color = new Color(0.8f, 0.4f, 0.4f) },
            new AssigneeData { name = "Miguel", color = new Color(0.2f, 0.8f, 0.4f) }
        };
    }
}
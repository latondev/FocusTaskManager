using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AshDev.Focus
{
    [System.Serializable]
    public class TaskItem
    {
        public string id = Guid.NewGuid().ToString();
        public string title;
        public string description;
        public string date;
        public List<Object> attachments = new List<Object>();

        // Link task với cột bằng ID cột (quan trọng cho logic Move)
        public string columnId;

        // --- NEW FIELDS FOR PREMIUM UI ---
        public TaskPriority priority = TaskPriority.None;
        public List<string> tags = new List<string>();
        public List<string> assignees = new List<string>(); // Danh sách tên/ID người phụ trách
        public string sprintId; // ID của Sprint mà task này thuộc về
        public string dueDate;  // Hạn chót
    }

    public enum TaskStatus
    {
        Backlog = 0,
        InProgress = 1,
        CodeReview = 2,
        Done = 3
    }

    public enum TaskPriority
    {
        None = 0,
        Low = 1,
        Medium = 2,
        High = 3
    }
}

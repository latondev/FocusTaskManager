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
    }
    
    public enum TaskStatus
    {
        Backlog = 0,
        InProgress = 1,
        CodeReview = 2,
        Done = 3
    }
}

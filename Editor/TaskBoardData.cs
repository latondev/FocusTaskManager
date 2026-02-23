using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace AshDev.Focus
{
    public interface ITaskManager
    {
        void MoveTask(TaskData task, TaskColumn targetColumn);
    }
    [System.Serializable]
    public class BoardColumn
    {
        public string id = Guid.NewGuid().ToString();
        public string title;
    }


    [CreateAssetMenu(fileName = "TaskBoardData", menuName = "Focus/TaskBoardData")]
    public class TaskBoardData : ScriptableObject
    {
        [Header("Dynamic Columns")]
        public List<BoardColumn> columns = new List<BoardColumn>();

        [Header("All Tasks")]
        public List<TaskItem> tasks = new List<TaskItem>();

        public void InitializeDefault()
        {
            if (columns == null || columns.Count == 0)
            {
                columns = new List<BoardColumn>
                {
                    new BoardColumn { id = "col_backlog", title = "Backlog" },
                    new BoardColumn { id = "col_progress", title = "In Progress" },
                    new BoardColumn { id = "col_review", title = "Code Review" },
                    new BoardColumn { id = "col_done", title = "Done" }
                };
            }
        }

        public void AddColumn(string title)
        {
            columns.Add(new BoardColumn 
            { 
                id = System.Guid.NewGuid().ToString(), 
                title = title 
            });
        }

        public void RemoveColumn(string colId)
        {
            columns.RemoveAll(c => c.id == colId);
            // Tùy chọn: Xóa luôn task hoặc chuyển về backlog (ở đây xóa luôn để đơn giản)
            tasks.RemoveAll(t => t.columnId == colId);
        }

        public void AddTask(TaskItem task)
        {
            if (string.IsNullOrEmpty(task.id)) task.id = System.Guid.NewGuid().ToString();
            tasks.Add(task);
        }

        public void RemoveTask(string taskId)
        {
            tasks.RemoveAll(t => t.id == taskId);
        }

        public void UpdateTaskStatus(string taskId, string newColId)
        {
            var task = tasks.Find(t => t.id == taskId);
            if (task != null)
            {
                task.columnId = newColId;
            }
        }

        public List<TaskItem> GetTasksByColumn(string colId)
        {
            if (tasks == null) return new List<TaskItem>();
            return tasks.Where(t => t.columnId == colId).ToList();
        }
    }
}

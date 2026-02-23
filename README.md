# Focus Task - Unity Task Management System

A Kanban-style task management tool built for Unity Editor using UI Toolkit.

## Features

- **Kanban Board Interface**: Visual task board with customizable columns
- **Drag & Drop Tasks**: Move tasks between columns by dragging cards
- **Tag System**: Organize tasks with custom tags and color coding
- **Asset Attachments**: Attach Unity assets directly to tasks
- **Task Details**: Title, description, date, tags, and file attachments
- **Tag Manager**: Dedicated tab to create and manage tags
- **Data Persistence**: Tasks saved in ScriptableObject asset
- **Dark Theme UI**: Modern, dark-themed interface
- **Responsive Layout**: Grid or list view for attachments

## Requirements

- Unity 6000.0 or higher
- UI Toolkit (built-in)

## Structure

```
Focus Task/
├── Editor/               # Editor-only code
│   ├── TaskManagerWindow.cs      # Main window implementation
│   ├── TaskCardDragManipulator.cs # Drag & drop logic
│   ├── TaskBoardData.cs           # Data container
│   ├── FocusData.cs               # Data models
│   └── TaskItem.cs                # Task item definition
├── GUI/                  # UI Toolkit assets
│   ├── TaskBoard.uss              # Styles
│   ├── TaskBoardWindow.uxml       # Main window layout
│   ├── TaskCard.uxml              # Task card template
│   └── TaskBoardData.asset        # Saved data
└── Runtime/              # Runtime assembly (currently empty)
```

## Usage

### Opening the Task Manager

Navigate to **Tools > Focus Task Manager** in Unity Editor menu.

### Creating Tasks

1. Click **+ Add Task** button in the header
2. Fill in task details:
   - **Title**: Task name
   - **Description**: Detailed description
   - **Tags**: Select from available tags
   - **Attachments**: Drag & drop Unity assets
3. Click **Save Changes** or press `Ctrl+Enter`

### Managing Columns

1. Click **+ Add Column** to create new columns
2. Click **✎** icon on column header to rename
3. Click **×** to delete a column

### Managing Tags

1. Switch to **Tag Manager** tab
2. Enter tag name in input field
3. Click **Create Tag** or press `Enter`
4. View tag usage statistics
5. Delete tags with **×** button

### Drag & Drop Tasks

- Click and hold a task card
- Drag to target column
- Release to move task

### Attaching Files

- Drag Unity assets (prefabs, textures, scripts, etc.) to the drop area
- Toggle between grid and list view
- Double-click to ping asset in Project view
- Right-click to remove attachment

## Data Structure

### TaskData
```csharp
- id (string)
- title (string)
- description (string)
- date (string)
- attachments (List<Object>)
- checklist (List<ChecklistItem>)
```

### TaskColumn
```csharp
- id (string)
- title (string)
- tasks (List<TaskData>)
```

### FocusBoardData
```csharp
- columns (List<TaskColumn>)
- availableTags (List<string>)
```

## Default Tags

Bug, Feature, Refactor, UI, Performance, High Priority, Low Priority

## Key Classes

- **TaskManagerWindow**: Main Editor window (`Editor/TaskManagerWindow.cs:11`)
- **TaskDragManipulator**: Handles drag & drop interactions (`Editor/TaskCardDragManipulator.cs:6`)
- **FocusBoardData**: ScriptableObject data container (`Editor/FocusData.cs:38`)
- **ITaskManager**: Interface for task operations (`Editor/TaskBoardData.cs:8`)

## Keyboard Shortcuts

- `Esc`: Close dialog
- `Ctrl+Enter`: Save task changes
- `Enter`: Confirm action in dialogs

## Customization

### Adding Custom Styles

Modify `GUI/TaskBoard.uss` to customize:
- Colors and themes
- Layout spacing
- Typography
- Card styling
- Drop zone appearance

### Extending Data Models

Edit `Editor/FocusData.cs` to add:
- Additional task properties
- Custom checklist types
- New data structures

## Data Storage

Tasks are stored in `Assets/Editor/Focus/UI/TaskBoardData.asset` as a ScriptableObject.

## Changelog

### [1.0.0] - 2026-02-23
- Initial release
- Kanban board with drag & drop
- Tag management system
- File attachments
- Dark themed UI

## Author

latondev

## License

See package.json
